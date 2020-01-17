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
    [SerializeField] [Range(0, 179)] private float minValueH, maxValueH;
    [SerializeField] [Range(0, 255)] private float minValueS, minValueV, maxValueS, maxValueV;
    [SerializeField] private PhysicMaterial bridgePhysicMaterial;
    [SerializeField] private Material bridgeMaterial;

    public static int windowWidth, windowHeight;
    public static float bridgeMinimalArea; // RANGE(0.001f, 0.05f) 0.001f <=> 10‰ of screen space ; 0.05f <=> 5%
    public static int captureCyclesBuffer; // RANGE(1, 20) Keep the last 10 values and average them to get proper / less jerky movements instead of using last frame results only

    private static VideoCapture webcam;
    private static GameObject bridge;
    private static float bridgeYSize = 1.0f;
    private static float bridgeZSize = 1.0f;
    private static float bridgeZposition = 1.0f;
    private static Vector3 bottomLeftSreenInWorldSpace;

    // OpenCV inputs variables
    private static OpenCVInputBuffer<float> bridgeAngles;
    private static OpenCVInputBuffer<Vector3> normalizedBridgeCenters; // (0,0) <=> Bottom Left, (1,1) <=> Top Right corner
    private static OpenCVInputBuffer<float> bridgeXSizes;
    private static OpenCVInputBuffer<float> bridgeScreenSpacePorcentages;

    // fixedUpdate variables for bridge's movements
    private Rigidbody bridgeRigidBody;
    private Vector3 bridgePosition;
    private Vector3 bridgeLocalScale;
    private Vector3 bridgeEulerAngles;
    private bool bridgeState;

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
    private float curBridgeXSize;
    private Vector3 normalizedBridgeCenterAverage;

    void Awake()
    {
        // Capture from webcam
        webcam = new VideoCapture(0);

        bridgeMinimalArea = PlayerPrefs.GetFloat("bridgeMinimalArea", 0.001f);
        captureCyclesBuffer = PlayerPrefs.GetInt("captureCyclesBuffer", 5);
        windowWidth = PlayerPrefs.GetInt("windowWidth", 640); // !!! CAMERA WINDOW !!! NOT ACTUAL SCREEN DIMENSIONS
        windowHeight = PlayerPrefs.GetInt("windowHeight", 480);

        // Set output
        if (Debug.isDebugBuild)
            CvInvoke.NamedWindow("BGR Output");

        // Get bridge renderer
        bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bridge.transform.name = "Bridge";
        bridge.transform.parent = transform;
        bridge.AddComponent<BoxCollider>();
        bridgeRigidBody = bridge.AddComponent<Rigidbody>();
        bridgeRigidBody.isKinematic = true;
        bridgeRigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        bridgeRigidBody.interpolation = RigidbodyInterpolation.Interpolate;
        bridgeRigidBody.GetComponent<BoxCollider>().material = bridgePhysicMaterial;
        bridgeRigidBody.isKinematic = true;
        bridge.tag = "Bridge";
        bridge.layer = LayerMask.NameToLayer("Bridge");
        bridge.GetComponent<MeshRenderer>().material = bridgeMaterial;
        bridge.GetComponent<BoxCollider>().material = bridgePhysicMaterial;

        // Get bottom left screen in world space coordinates
        bottomLeftSreenInWorldSpace = new Vector3(0, 0, bridgeZposition);

        minValueH = PlayerPrefs.GetFloat("minValueH", 70);
        minValueS = PlayerPrefs.GetFloat("minValueS", 130);
        minValueV = PlayerPrefs.GetFloat("minValueV", 55);
        maxValueH = PlayerPrefs.GetFloat("maxValueH", 110);
        maxValueS = PlayerPrefs.GetFloat("maxValueS", 255);
        maxValueV = PlayerPrefs.GetFloat("maxValueV", 255);

        // Set OpenCV inputs buffers
        normalizedBridgeCenters = new OpenCVInputBuffer<Vector3>(captureCyclesBuffer);
        if (captureCyclesBuffer % 2 == 0) // MUST be an even number otherwise there'll be sign problem as we're averaging positive and negative values
            bridgeAngles = new OpenCVInputBuffer<float>(captureCyclesBuffer);
        else
            bridgeAngles = new OpenCVInputBuffer<float>(captureCyclesBuffer + 1);
        bridgeXSizes = new OpenCVInputBuffer<float>(captureCyclesBuffer);
        bridgeScreenSpacePorcentages = new OpenCVInputBuffer<float>(captureCyclesBuffer);

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
        if (webcamThreadRunning && bridgeState)
        {
            bridge.SetActive(true);
            bridgeRigidBody.MovePosition(bridgePosition);
            bridge.transform.localScale = bridgeLocalScale;
            bridge.transform.eulerAngles = bridgeEulerAngles;
        }
        else
            bridge.SetActive(false);
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
            thresoldOUTFilter = imgOUTBin.InRange(new Hsv(minValueH, minValueS, minValueV), new Hsv(maxValueH, maxValueS, maxValueV)).Mat;

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
                    bridgeState = false;
                    continue;
                }
                boundRecPoints = boundRec.GetVertices();
                curNormalizedBridgeCenter.x = (1 - boundRec.Center.X / windowWidth) * 35 - 35 / 2; //TODO : hardcoded values, change them
                curNormalizedBridgeCenter.y = (1 - boundRec.Center.Y / windowWidth) * 25 - 15; //TODO : hardcoded values, change them
                //curNormalizedBridgeCenter.x = (1 - boundRec.Center.X / windowWidth) * Screen.width;
                //curNormalizedBridgeCenter.y = (1 - boundRec.Center.Y / windowHeight) * Screen.height;
                curNormalizedBridgeCenter.z = bridgeZposition;
                getCurNormalizedBridgeCenter();
                // Insert position value
                normalizedBridgeCenters.PushBack(curNormalizedBridgeCenter);

                // Draw Bounding Rectangle 
                DrawPointsFRectangle(boundRecPoints, imgBGRMat);

                // Draw Unity's rectangle (only if it is superior to bridgeMinimalArea)
                curBridgeScreenSpacePorcentage = (boundRec.Size.Height / windowHeight) * (boundRec.Size.Width / windowWidth); // (0,1) porcentage of screen taken by the scanned object
                bridgeScreenSpacePorcentages.PushBack(curBridgeScreenSpacePorcentage);
                // Get birdge screen space average
                bridgeScreenSpacePorcentageAverage = 0.0f;
                foreach (float f in bridgeScreenSpacePorcentages.data)
                    bridgeScreenSpacePorcentageAverage += f;
                bridgeScreenSpacePorcentageAverage /= bridgeScreenSpacePorcentages.curLength;
                if (bridgeScreenSpacePorcentageAverage > bridgeMinimalArea)
                {
                    // Get useful values
                    curBridgeAngle = boundRec.Angle;
                    if (boundRec.Size.Width < boundRec.Size.Height)
                    {
                        curBridgeAngle = 90 + curBridgeAngle;
                        curBridgeXSize = (boundRec.Size.Height / windowHeight) * (cameraMainOrthographicSize * 2.35f) * (Screen.width / Screen.height); // TODO (low priority) : totally hacked value for 16/9 ratio screenss, fix it 
                    }
                    else
                        curBridgeXSize = (boundRec.Size.Width / windowHeight) * (cameraMainOrthographicSize * 2.35f) * (Screen.width / Screen.height);

                    // Insert angle value
                    bridgeAngles.PushBack(curBridgeAngle);
                    // Insert bridge size value
                    bridgeXSizes.PushBack(curBridgeXSize);

                    // !!! Get averages !!! 
                    // average position
                    normalizedBridgeCenterAverage = new Vector3();
                    foreach (Vector3 v in normalizedBridgeCenters.data)
                        normalizedBridgeCenterAverage += v;
                    normalizedBridgeCenterAverage /= normalizedBridgeCenters.curLength;
                    // average angle
                    float angleAverage = 0.0f;
                    foreach (float f in bridgeAngles.data)
                        angleAverage += f;
                    angleAverage /= bridgeAngles.curLength;
                    // average bridge size
                    float bridgeXSizeAverage = 0.0f;
                    foreach (float f in bridgeXSizes.data)
                        bridgeXSizeAverage += f;
                    bridgeXSizeAverage /= bridgeXSizes.curLength;

                    // !!! Setting bridge values !!!
                    bridgeState = true;
                    bridgePosition = new Vector3(bottomLeftSreenInWorldSpace.x + normalizedBridgeCenterAverage.x, bottomLeftSreenInWorldSpace.y + normalizedBridgeCenterAverage.y, bridgeZposition);
                    bridgeLocalScale = new Vector3(bridgeXSizeAverage, bridgeYSize, bridgeZSize);
                    bridgeEulerAngles = new Vector3(0, 0, curBridgeAngle);
                }
                else
                    bridgeState = false;
            }
            else
                bridgeState = false;
            /************************************************/

            /*** Debug Display ***/
            if (debugFlag)
            {
                CvInvoke.Flip(imgBGRMat, imgBGRMat, FlipType.Horizontal); // Flip picture
                CvInvoke.CvtColor(imgBGRMat, imgBGRMat, ColorConversion.Hsv2Bgr);
                CvInvoke.Imshow("BGR Output", imgBGRMat); // !!! BY DEFAULT USE BGR COLORSPACE !!!
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
