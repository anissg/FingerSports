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

public class PlayerManager : MonoBehaviour
{
    public DetectionConfig player1Config;
    public DetectionConfig player2Config;

    public int windowWidth, windowHeight;
    // RANGE(0.001f, 0.05f) 0.001f <=> 10‰ of screen space ; 0.05f <=> 5%
    public float playersMinimalArea;
    // RANGE(1, 20) Keep the last 10 values and average them to get proper
    // less jerky movements instead of using last frame results only
    public int captureCyclesBuffer; 
    public GameObject player1;
    public GameObject player2;
    private bool playersState;

    private VideoCapture webcam;
    
    private Vector3 bottomLeftSreenInWorldSpace;

    // OpenCV inputs variables
    private InputBuffer<float> player1Angles;
    private InputBuffer<float> player2Angles;
    // (0,0) <=> Bottom Left, (1,1) <=> Top Right corner
    private InputBuffer<Vector3> normalizedPlayer1Centers;
    // (0,0) <=> Bottom Left, (1,1) <=> Top Right corner
    private InputBuffer<Vector3> normalizedPlayer2Centers;

    private InputBuffer<float> player1ScreenSpacePorcentages;


    // fixedUpdate variables for players movement
    private Rigidbody2D player1RigidBody;
    private Rigidbody2D player2RigidBody;
    private Vector3 player1Position;
    private Vector3 player2Position;
    private Vector3 player1EulerAngles;
    private Vector3 player2EulerAngles;

    // Camera and CameraHandler's thread variables 
    private Thread webcamThread;
    private bool webcamThreadRunning;
    private Camera mainCamera;
    private bool debugFlag;
    private Vector3 curNormalizedBridgeCenter;

    private float cameraMainOrthographicSize;

    // WebcamHandler variables
    private Mat imgBGRMat, imgINMat;
    private Image<Hsv, byte> imgOUTBin;
    private Mat thresoldOUTFilter;
    private Mat structuringElement;
    private VectorOfVectorOfPoint contours;
    private VectorOfPoint biggestContour;
    private Mat hierarchy;
    private RotatedRect boundRec;
    private PointF[] boundRecPoints;
    private float curBridgeScreenSpacePorcentage;
    private float bridgeScreenSpacePorcentageAverage;
    private float curBridgeAngle;
    private Vector3 normalizedBridgeCenterAverage;

    void Awake()
    {
        // Capture from webcam
        webcam = new VideoCapture(0);

        playersMinimalArea = 0.01f; //todo add to config
        captureCyclesBuffer = 3;
        windowWidth = 640;
        windowHeight = 480;

        // Set output
        if (Debug.isDebugBuild)
            CvInvoke.NamedWindow("BGR Output");


        player1RigidBody = player1.GetComponent<Rigidbody2D>();
        player2RigidBody = player2.GetComponent<Rigidbody2D>();

        // Get bottom left screen in world space coordinates
        //bottomLeftSreenInWorldSpace = new Vector3(0, 0, bridgeZposition);

        if (PlayerPrefs.HasKey("player1conf"))
        {
            player1Config = JsonUtility.FromJson<DetectionConfig>(PlayerPrefs.GetString("player1conf"));
        }
        else
        {
            // load default values
            player1Config = new DetectionConfig()
            {
                minValueH = 9-10,//to do
                minValueS = 182-25,
                minValueV = 189-40,
                maxValueH = 9+10,
                maxValueS = 182+25,
                maxValueV = 189+40
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
        normalizedPlayer1Centers = new InputBuffer<Vector3>(captureCyclesBuffer);
        normalizedPlayer2Centers = new InputBuffer<Vector3>(captureCyclesBuffer);
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
        player1ScreenSpacePorcentages = new InputBuffer<float>(captureCyclesBuffer);

        // Starting WebcamHandler thread and its variables
        webcamThread = new System.Threading.Thread(WebcamHandler);
        webcamThreadRunning = true;
        webcamThread.Start();
        mainCamera = Camera.main;
        debugFlag = Debug.isDebugBuild;
        curNormalizedBridgeCenter = new Vector3();
        cameraMainOrthographicSize = Camera.main.orthographicSize;
    }

    void OnDestroy()
    {
        webcamThreadRunning = false;
        webcam.Stop();
        webcam.Dispose();
        webcamThread.Join();
        if (debugFlag)
            CvInvoke.DestroyAllWindows();
        imgBGRMat = null;
        imgINMat = null;
    }

    void FixedUpdate()
    {
        Vector2 dir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        player1RigidBody.velocity = dir * 10;

        if (webcamThreadRunning && playersState)
        {
            player1RigidBody.MovePosition(player1Position);
            player1.transform.eulerAngles = player1EulerAngles;
            player2RigidBody.MovePosition(player2Position);
            player2.transform.eulerAngles = player2EulerAngles;
        }
        else
        {
            // reset positions and rotations
        }
    }

    void WebcamHandler()
    {
        while (webcamThreadRunning)
        {
            /*** Capture webcam stream ***/
            imgBGRMat = webcam.QueryFrame();
            //CvInvoke.Resize(imgBGRMat, imgBGRMat, new Size(windowWidth, windowHeight)); // TODO : correct the usage of webcamWidth/Heights
            if (imgBGRMat == null) // if frame is not ready 
                continue;
            imgINMat = imgBGRMat;

            /*** COMPUTE INPUT ***/ // Isolate unity defined Color range 
            CvInvoke.Flip(imgINMat, imgINMat, FlipType.Horizontal); // Flip picture
            CvInvoke.CvtColor(imgINMat, imgINMat, ColorConversion.Bgr2Hsv); // Convert input to hsv
            CvInvoke.Flip(imgINMat, imgINMat, FlipType.Horizontal); // Flip picture

            // Applying thresold => getting binary filter => multiply it by input to get back color values
            imgOUTBin = imgINMat.ToImage<Hsv, byte>(); // Binary output
            thresoldOUTFilter = new Mat(); // Binary Filter
            thresoldOUTFilter = imgOUTBin.InRange(
                new Hsv( player1Config.minValueH, player1Config.minValueS, player1Config.minValueV), 
                new Hsv(player1Config.maxValueH, player1Config.maxValueS, player1Config.maxValueV)).Mat;

            // Clearing Filter <=> Applying opening 
            int operationSize = 1;
            structuringElement = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(2 * operationSize + 1, 2 * operationSize + 1), new Point(operationSize, operationSize));
            CvInvoke.Erode(thresoldOUTFilter, thresoldOUTFilter, structuringElement, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar(0)); // Erode -> Dilate <=> Opening
            CvInvoke.Dilate(thresoldOUTFilter, thresoldOUTFilter, structuringElement, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar(0));

            /*** Detecting Edges (with built-in method) ***/
            contours = new VectorOfVectorOfPoint();
            biggestContour = new VectorOfPoint();
            double biggestContourArea = 0;

            hierarchy = new Mat();
            CvInvoke.FindContours(thresoldOUTFilter, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone); // Find Contour using Binary filter
            for (int i = 0; i < contours.Size; i++)
            {
                double a = CvInvoke.ContourArea(contours[i], false);
                if (a > biggestContourArea)
                {
                    biggestContourArea = a;
                    biggestContour = contours[i];
                }
            }

            /*** Extract / Create Unity "Cube" / Bridge ***/
            if (biggestContour.Size > 0 /*&& SplashScreenBehavior.getSplashScreenState() == SplashScreenBehavior.SplashScreenState.Invisible*/) // If we detected enough of the calibrated color and the game is in progress
            {
                // Determine Bounding Rectangle and setting its related values
                boundRec = CvInvoke.MinAreaRect(biggestContour);
                if (boundRec.Size.IsEmpty) // Just in case MinAreaRect fails ... It happens sometime ... Because why not
                {
                    playersState = false;
                    continue;
                }
                boundRecPoints = boundRec.GetVertices();
                curNormalizedBridgeCenter.x = (1 - boundRec.Center.X / windowWidth) * 35 - 35 / 2; //TODO : hardcoded values, change them
                curNormalizedBridgeCenter.y = (1 - boundRec.Center.Y / windowWidth) * 25 - 15; //TODO : hardcoded values, change them
                //curNormalizedBridgeCenter.x = (1 - boundRec.Center.X / windowWidth) * Screen.width;
                //curNormalizedBridgeCenter.y = (1 - boundRec.Center.Y / windowHeight) * Screen.height;
                curNormalizedBridgeCenter.z = 0;
                getCurNormalizedBridgeCenter();
                // Insert position value
                normalizedPlayer1Centers.PushBack(curNormalizedBridgeCenter);

                // Draw Bounding Rectangle 
                DrawPointsFRectangle(boundRecPoints, imgBGRMat);

                // Draw Unity's rectangle (only if it is superior to bridgeMinimalArea)
                curBridgeScreenSpacePorcentage = (boundRec.Size.Height / windowHeight) * (boundRec.Size.Width / windowWidth); // (0,1) porcentage of screen taken by the scanned object
                player1ScreenSpacePorcentages.PushBack(curBridgeScreenSpacePorcentage);
                // Get birdge screen space average
                bridgeScreenSpacePorcentageAverage = 0.0f;
                foreach (float f in player1ScreenSpacePorcentages.data)
                    bridgeScreenSpacePorcentageAverage += f;
                bridgeScreenSpacePorcentageAverage /= player1ScreenSpacePorcentages.curLength;
                if (bridgeScreenSpacePorcentageAverage > playersMinimalArea)
                {
                    // Get useful values
                    curBridgeAngle = boundRec.Angle;
                    if (boundRec.Size.Width < boundRec.Size.Height)
                    {
                        curBridgeAngle = 90 + curBridgeAngle;
                    }

                    // Insert angle value
                    player1Angles.PushBack(curBridgeAngle);

                    // !!! Get averages !!! 
                    // average position
                    normalizedBridgeCenterAverage = new Vector3();
                    foreach (Vector3 v in normalizedPlayer1Centers.data)
                        normalizedBridgeCenterAverage += v;
                    normalizedBridgeCenterAverage /= normalizedPlayer1Centers.curLength;
                    // average angle
                    float angleAverage = 0.0f;
                    foreach (float f in player1Angles.data)
                        angleAverage += f;
                    angleAverage /= player1Angles.curLength;

                    // !!! Setting bridge values !!!
                    playersState = true;
                    player1Position = new Vector3(bottomLeftSreenInWorldSpace.x + normalizedBridgeCenterAverage.x, bottomLeftSreenInWorldSpace.y + normalizedBridgeCenterAverage.y, 0);
                    player1EulerAngles = new Vector3(0, 0, curBridgeAngle);
                }
                else
                    playersState = false;
            }
            else
                playersState = false;
            /************************************************/

            /*** Debug Display ***/
            if (debugFlag)
            {
                CvInvoke.Flip(imgBGRMat, imgBGRMat, FlipType.Horizontal); // Flip picture
                CvInvoke.CvtColor(imgBGRMat, imgBGRMat, ColorConversion.Hsv2Bgr);
                // CvInvoke.Imshow("BGR Output", imgBGRMat); // !!! BY DEFAULT USE BGR COLORSPACE !!!
                /*** CvInvoke.Resize(imgBGRMat, imgBGRMat, new Size(windowWidth, windowHeight)); ***/ // DO NOT USE UNTIL windowWidth and windowHeight is fixed and used
            }
        }
    }

    private IEnumerator getCurNormalizedBridgeCenter()
    {
        curNormalizedBridgeCenter = mainCamera.ScreenToWorldPoint(curNormalizedBridgeCenter);
        yield return null;
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
