using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{    
    public GameObject player1;
    public GameObject player2;

    private bool playersState;
    private DetectionConfig player1Config;
    private DetectionConfig player2Config;

    // fixedUpdate variables for players movement
    private Rigidbody2D player1RigidBody;
    private Rigidbody2D player2RigidBody;
    private Vector3 player1Velocity;
    private Vector3 player2Velocity;
    private Vector3 player1EulerAngles;
    private Vector3 player2EulerAngles;
    
    // RANGE(1, 20) Keep the last 10 values and average them to get proper
    // less jerky movements instead of using last frame results only
    public int captureCyclesBuffer; 
    public float playersMinimalArea;
    private VideoCapture webcam;
    private int windowWidth, windowHeight;
    private InputBuffer<float> player1Angles;
    private InputBuffer<float> player2Angles;
    private InputBuffer<Vector3> player1Centers;
    private InputBuffer<Vector3> player2Centers;

    private Camera mainCamera;
    private bool debugFlag;
    private float cameraMainOrthographicSize;

    // Detection variables
    private Mat imgBGR;
    private Mat imgIN;
    private Image<Hsv, byte> imgBIN;
    private Mat player1ThresholdOUT;
    private Mat player2ThresholdOUT;
    private Mat structuringElement;
    private VectorOfVectorOfPoint player1Contours;
    private VectorOfVectorOfPoint player2Contours;
    private VectorOfPoint player1Contour;
    private VectorOfPoint player2Contour;
    private Mat hierarchy;
    private RotatedRect boundRec;

    void Awake()
    {
        // Capture from webcam
        webcam = new VideoCapture(0);
        webcam.FlipHorizontal = true;
        //webcam.FlipVertical = true;
        CvInvoke.CheckLibraryLoaded();

        playersMinimalArea = 0.001f;
        captureCyclesBuffer = 5;
        windowWidth = 640;
        windowHeight = 480;

        player1RigidBody = player1.GetComponent<Rigidbody2D>();
        player2RigidBody = player2.GetComponent<Rigidbody2D>();

        if (PlayerPrefs.HasKey("player1conf"))
        {
            player1Config = JsonUtility.FromJson<DetectionConfig>(PlayerPrefs.GetString("player1conf"));
        }
        else
        {
            // load default values
            player1Config = new DetectionConfig()
            {
                minValueH = 9 - 10,//to do
                minValueS = 182 - 25,
                minValueV = 189 - 40,
                maxValueH = 9 + 10,
                maxValueS = 182 + 25,
                maxValueV = 189 + 40
            };
        }

        if (PlayerPrefs.HasKey("player2conf"))
        {
            player2Config = JsonUtility.FromJson<DetectionConfig>(PlayerPrefs.GetString("player2conf"));
        }
        else
        {
            // load default values
            player2Config = new DetectionConfig()
            {
                minValueH = 96 - 10,
                minValueS = 164 - 25,
                minValueV = 133 - 40,
                maxValueH = 96 + 10,
                maxValueS = 164 + 25,
                maxValueV = 133 + 40
            };
        }

        // Set OpenCV inputs buffers
        player1Centers = new InputBuffer<Vector3>(captureCyclesBuffer);
        player2Centers = new InputBuffer<Vector3>(captureCyclesBuffer);
        // MUST be an even number otherwise there'll be sign problem as we're averaging positive and negative values
        if (captureCyclesBuffer % 2 == 0)
        {
            player1Angles = new InputBuffer<float>(captureCyclesBuffer);
            player2Angles = new InputBuffer<float>(captureCyclesBuffer);
        }
        else
        {
            player1Angles = new InputBuffer<float>(captureCyclesBuffer + 1);
            player2Angles = new InputBuffer<float>(captureCyclesBuffer + 1);
        }
        
        //player1ScreenSpacePorcentages = new InputBuffer<float>(captureCyclesBuffer);

        // Starting WebcamHandler thread and its variables

        webcam.ImageGrabbed += Webcam_ImageGrabbed;

        mainCamera = Camera.main;
        debugFlag = Debug.isDebugBuild;
        cameraMainOrthographicSize = Camera.main.orthographicSize;
    }

    private void Webcam_ImageGrabbed(object sender, EventArgs e)
    {
        imgBGR = new Mat();

        if (webcam.IsOpened)
        {
            /*** Capture webcam stream ***/
            webcam.Retrieve(imgBGR);

            if (imgBGR.IsEmpty) return;

            imgIN = imgBGR.Clone();

            // Isolate unity defined Color range 
            CvInvoke.CvtColor(imgIN, imgIN, ColorConversion.Bgr2Hsv); // Convert input to hsv
            //CvInvoke.GaussianBlur(imgIN, imgIN, new Size(25, 25), 0);

            // Applying thresold => getting binary filter => multiply it by input to get back color values
            imgBIN = imgIN.ToImage<Hsv, byte>(); // Binary output
            player1ThresholdOUT = new Mat(); // Player1 Binary Filter
            player2ThresholdOUT = new Mat(); // Player2 Binary Filter

            player1ThresholdOUT = imgBIN.InRange(
                new Hsv(player1Config.minValueH, player1Config.minValueS, player1Config.minValueV),
                new Hsv(player1Config.maxValueH, player1Config.maxValueS, player1Config.maxValueV)).Mat;
            player2ThresholdOUT = imgBIN.InRange(
                            new Hsv(player2Config.minValueH, player2Config.minValueS, player2Config.minValueV),
                            new Hsv(player2Config.maxValueH, player2Config.maxValueS, player2Config.maxValueV)).Mat;

            // Clearing Filter and Applying opening 
            int operationSize = 1;
            structuringElement = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(2 * operationSize + 1, 2 * operationSize + 1), new Point(operationSize, operationSize));
            
            CvInvoke.Erode(player1ThresholdOUT, player1ThresholdOUT, structuringElement, new Point(-1, -1), 5, BorderType.Constant, new MCvScalar(0));
            CvInvoke.Dilate(player1ThresholdOUT, player1ThresholdOUT, structuringElement, new Point(-1, -1), 5, BorderType.Constant, new MCvScalar(0));

            CvInvoke.Erode(player2ThresholdOUT, player2ThresholdOUT, structuringElement, new Point(-1, -1), 5, BorderType.Constant, new MCvScalar(0));
            CvInvoke.Dilate(player2ThresholdOUT, player2ThresholdOUT, structuringElement, new Point(-1, -1), 5, BorderType.Constant, new MCvScalar(0));

            // Detecting Edges 
            player1Contours = new VectorOfVectorOfPoint();
            player1Contour = new VectorOfPoint();
            double biggestContourArea = 0;
            hierarchy = new Mat();
            CvInvoke.FindContours(player1ThresholdOUT, player1Contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone); // Find player1 Contour using Binary filter
            for (int i = 0; i < player1Contours.Size; i++) 
            {
                double a = CvInvoke.ContourArea(player1Contours[i], false);
                if (a > biggestContourArea)
                {
                    biggestContourArea = a;
                    player1Contour = player1Contours[i];
                }
            }

            player2Contours = new VectorOfVectorOfPoint();
            player2Contour = new VectorOfPoint();
            biggestContourArea = 0;
            hierarchy = new Mat();
            CvInvoke.FindContours(player2ThresholdOUT, player2Contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone); // Find player2 Contour using Binary filter
            for (int i = 0; i < player2Contours.Size; i++)
            {
                double a = CvInvoke.ContourArea(player2Contours[i], false);
                if (a > biggestContourArea)
                {
                    biggestContourArea = a;
                    player2Contour = player2Contours[i];
                }
            }

            // extract player1 rect pos and rotation
            if (player1Contour.Size > 0)
            {
                // Determine Bounding Rectangle and setting its related values
                boundRec = CvInvoke.MinAreaRect(player1Contour);

                if (boundRec.Size.IsEmpty)
                {
                    playersState = false;
                    return;
                }

                Vector3 currentCenter = new Vector2(boundRec.Center.X,boundRec.Center.Y);

                player1Centers.PushBack(currentCenter);

                // Draw Bounding Rectangle 
                if (debugFlag)
                    DrawPointsFRectangle(boundRec.GetVertices(), imgBGR);

                float currentScreenSpacePorcentage = (boundRec.Size.Height / windowHeight) * (boundRec.Size.Width / windowWidth);

                if (currentScreenSpacePorcentage > playersMinimalArea)
                {
                    float currentAngle = boundRec.Angle;

                    if (boundRec.Size.Height < boundRec.Size.Width)
                    {
                        currentAngle = 90 + currentAngle;
                    }

                    // Insert angle value
                    player1Angles.PushBack(currentAngle);

                    // Get averages
                    Vector3 centerDiffAverage = Vector3.zero;
                    for (int i = 0; i < player1Centers.curLength - 1; i++)
                    {
                        centerDiffAverage += player1Centers.data[i + 1] - player1Centers.data[i];
                    }
                    centerDiffAverage /= (player1Centers.curLength - 1);

                    // average angle
                    float angleAverage = 0.0f;
                    foreach (float f in player1Angles.data)
                        angleAverage += f;
                    angleAverage /= player1Angles.curLength;

                    // !!! Setting bridge values !!!
                    playersState = true;
                    player1Velocity = centerDiffAverage * 1f;
                    player1Velocity.y = -player1Velocity.y;
                    player1EulerAngles = new Vector3(0, 0, -angleAverage);
                }
            }

            // extract player2 rect pos and rotation
            if (player2Contour.Size > 0)
            {
                // Determine Bounding Rectangle and setting its related values
                boundRec = CvInvoke.MinAreaRect(player2Contour);

                if (boundRec.Size.IsEmpty)
                {
                    playersState = false;
                    return;
                }

                Vector3 currentCenter = new Vector2(boundRec.Center.X, boundRec.Center.Y);

                player2Centers.PushBack(currentCenter);

                // Draw Bounding Rectangle 
                if (debugFlag)
                    DrawPointsFRectangle(boundRec.GetVertices(), imgBGR);

                float currentScreenSpacePorcentage = (boundRec.Size.Height / windowHeight) * (boundRec.Size.Width / windowWidth);

                if (currentScreenSpacePorcentage > playersMinimalArea)
                {
                    float currentAngle = boundRec.Angle;

                    if (boundRec.Size.Height < boundRec.Size.Width)
                    {
                        currentAngle = 90 + currentAngle;
                    }

                    // Insert angle value
                    player2Angles.PushBack(currentAngle);

                    // Get averages
                    Vector3 centerDiffAverage = Vector3.zero;
                    for (int i = 0; i < player2Centers.curLength - 1; i++)
                    {
                        centerDiffAverage += player2Centers.data[i + 1] - player2Centers.data[i];
                    }
                    centerDiffAverage /= (player2Centers.curLength - 1);

                    // average angle
                    float angleAverage = 0.0f;
                    foreach (float f in player2Angles.data)
                        angleAverage += f;
                    angleAverage /= player2Angles.curLength;

                    // !!! Setting bridge values !!!
                    playersState = true;
                    player2Velocity = centerDiffAverage * 1f;
                    player2Velocity.y = -player2Velocity.y;
                    player2EulerAngles = new Vector3(0, 0, -angleAverage);
                }
            }

            //Debug Display
            if (debugFlag)
            {
                //CvInvoke.Imshow("cam", imgBGR);
                //CvInvoke.Imshow("p1", player1ThresholdOUT);
                //CvInvoke.Imshow("p2", player2ThresholdOUT);
            }
        }
    }

    void OnDestroy()
    {
        webcam.Stop();
        webcam.Dispose();
        if (debugFlag)
            CvInvoke.DestroyAllWindows();
        imgBGR = null;
        imgIN = null;
    }

    void Update()
    {
        if (webcam.IsOpened)
        {
            // update the image from the webcam
            webcam.Grab();
        }
    }

    void FixedUpdate()
    {
        if (webcam.IsOpened && playersState)
        {
            if (!float.IsNaN(player1Velocity.x))
            {
                player1RigidBody.velocity = player1Velocity;
                player1.transform.eulerAngles = player1EulerAngles;
            }

            if (!float.IsNaN(player2Velocity.x))
            {
                player2RigidBody.velocity = player2Velocity;
                player2.transform.eulerAngles = player2EulerAngles;
            }
        }
        else
        {
            // reset positions and rotations
            player1RigidBody.velocity = Vector3.zero;
            player1.transform.eulerAngles = Vector3.Lerp(player1.transform.eulerAngles, Vector3.zero, .8f);
            player2RigidBody.velocity = Vector3.zero;
            player2.transform.eulerAngles = Vector3.Lerp(player2.transform.eulerAngles, Vector3.zero, .8f);
        }
    }

    void DrawPointsFRectangle(PointF[] boundRecPoints, Mat output)
    {
        // Draw Bounding Rectangle from the first 4 points of "boundRecPoints" onto "output"
        CvInvoke.Line(output, new Point((int)boundRecPoints[0].X, (int)boundRecPoints[0].Y), new Point((int)boundRecPoints[1].X, (int)boundRecPoints[1].Y), new MCvScalar(100, 0, 0), 3, (LineType)8, 0);
        CvInvoke.Line(output, new Point((int)boundRecPoints[1].X, (int)boundRecPoints[1].Y), new Point((int)boundRecPoints[2].X, (int)boundRecPoints[2].Y), new MCvScalar(100, 0, 0), 3, (LineType)8, 0);
        CvInvoke.Line(output, new Point((int)boundRecPoints[2].X, (int)boundRecPoints[2].Y), new Point((int)boundRecPoints[3].X, (int)boundRecPoints[3].Y), new MCvScalar(100, 0, 0), 3, (LineType)8, 0);
        CvInvoke.Line(output, new Point((int)boundRecPoints[3].X, (int)boundRecPoints[3].Y), new Point((int)boundRecPoints[0].X, (int)boundRecPoints[0].Y), new MCvScalar(100, 0, 0), 3, (LineType)8, 0);
    }
}
