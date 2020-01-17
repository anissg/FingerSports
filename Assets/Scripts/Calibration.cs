using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using UnityEngine.SceneManagement;

public class Calibration : MonoBehaviour {

    [SerializeField] [Range(0, 180)] float minValueH;[SerializeField] [Range(0, 255)] float minValueS, minValueV;
    [SerializeField] [Range(0, 180)] float maxValueH;[SerializeField] [Range(0, 255)] float maxValueS, maxValueV;
    [SerializeField] private Texture textureHue;
    [SerializeField] private Texture textureSaturation;
    [SerializeField] private Texture textureValue;


    private VideoCapture webcam;
    private int webcam_Capture_WIDTH, webcam_Capture_HEIGHT;
    private Texture2D texture;
    private Hsv pickerHSV;

    public float bridgeMinimalArea; // 10‰ of screen space
    public int captureCyclesBuffer;
    public int windowWidth, windowHeight;

    public UnityEngine.Color outputTest;

    // Use this for initialization
    void Start () {

        bridgeMinimalArea = PlayerPrefs.GetFloat("bridgeMinimalArea", 0.001f) * 100;
        captureCyclesBuffer = PlayerPrefs.GetInt("captureCyclesBuffer", 5);
        windowWidth = PlayerPrefs.GetInt("windowWidth", 640);
        windowHeight = PlayerPrefs.GetInt("windowHeight", 480);
		minValueH = PlayerPrefs.GetFloat("minValueH", 65);
        minValueS = PlayerPrefs.GetFloat("minValueS", 155);
        minValueV = PlayerPrefs.GetFloat("minValueV", 150);
        maxValueH = PlayerPrefs.GetFloat("maxValueH", 100);
        maxValueS = PlayerPrefs.GetFloat("maxValueS", 255);
        maxValueV = PlayerPrefs.GetFloat("maxValueV", 255);

        webcam_Capture_WIDTH = Screen.width * 2 / 3;
        webcam_Capture_HEIGHT = Screen.height * 2 / 3;

        webcam = new VideoCapture(0);
        

		texture = new Texture2D(webcam_Capture_WIDTH, webcam_Capture_HEIGHT, TextureFormat.RGBA32, false);
    }

    // Update is called once per frame
    void Update()
    {
        Mat imgBGRMat;
        imgBGRMat = webcam.QueryFrame();

        if (imgBGRMat == null) // if frame is not ready 
            return;

        CvInvoke.Flip(imgBGRMat, imgBGRMat, FlipType.Horizontal);
        CvInvoke.Resize(imgBGRMat, imgBGRMat, new Size(webcam_Capture_WIDTH, webcam_Capture_HEIGHT));
        
		  
        Mat thresoldOUTFilter = new Mat();
        Mat imgOUTMat = new Mat();
        
        CvInvoke.CvtColor(imgBGRMat, imgOUTMat, ColorConversion.Bgr2Hsv);
        Image<Hsv, byte> imgOUTBin = imgOUTMat.ToImage<Hsv, byte>();

        thresoldOUTFilter = imgOUTBin.InRange(new Hsv(minValueH, minValueS, minValueV), new Hsv(maxValueH, maxValueS, maxValueV)).Mat;
        
        int operationSize = 1;
        
        Mat structuringElement = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(2 * operationSize + 1, 2 * operationSize + 1), new Point(operationSize, operationSize));
        CvInvoke.Erode(thresoldOUTFilter, thresoldOUTFilter, structuringElement, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar(0)); // Erode -> Dilate <=> Opening
        CvInvoke.Dilate(thresoldOUTFilter, thresoldOUTFilter, structuringElement, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar(0));

        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        VectorOfPoint biggestContour = new VectorOfPoint();
        int biggestContourIndex = -1;
        double biggestContourArea = 0;

        Mat hierarchy = new Mat();
        CvInvoke.FindContours(thresoldOUTFilter, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone); // Find Contour using Binary filter
        for (int i = 0; i < contours.Size; i++)
        {
            double a = CvInvoke.ContourArea(contours[i], false);
            if (a > biggestContourArea)
            {
                biggestContourArea = a;
                biggestContourIndex = i;
                biggestContour = contours[i];
            }
        }

        if (biggestContourIndex != -1 && biggestContour.Size > 0)
        {
            // Determine Bounding Rectangle and setting its related values
            RotatedRect boundRec = CvInvoke.MinAreaRect(biggestContour);
            PointF[] boundRecPoints = boundRec.GetVertices();

            // Draw Bounding Rectangle 
            DrawPointsFRectangle(boundRecPoints, imgBGRMat);
        }

        texture = Utils.ConvertFromMatToTex2D(imgBGRMat, texture, webcam_Capture_WIDTH, webcam_Capture_HEIGHT);
    }

    

    void DrawPointsFRectangle(PointF[] boundRecPoints, Mat output)
    {
        // Draw Bounding Rectangle
        CvInvoke.Line(output, new Point((int)boundRecPoints[0].X, (int)boundRecPoints[0].Y), new Point((int)boundRecPoints[1].X, (int)boundRecPoints[1].Y), new MCvScalar(100, 0, 0), 3, (LineType)8, 0);
        CvInvoke.Line(output, new Point((int)boundRecPoints[1].X, (int)boundRecPoints[1].Y), new Point((int)boundRecPoints[2].X, (int)boundRecPoints[2].Y), new MCvScalar(100, 0, 0), 3, (LineType)8, 0);
        CvInvoke.Line(output, new Point((int)boundRecPoints[2].X, (int)boundRecPoints[2].Y), new Point((int)boundRecPoints[3].X, (int)boundRecPoints[3].Y), new MCvScalar(100, 0, 0), 3, (LineType)8, 0);
        CvInvoke.Line(output, new Point((int)boundRecPoints[3].X, (int)boundRecPoints[3].Y), new Point((int)boundRecPoints[0].X, (int)boundRecPoints[0].Y), new MCvScalar(100, 0, 0), 3, (LineType)8, 0);
    }

    

    private void OnGUI()
    {
        Rect rect = new Rect(0, 0, Screen.width * 2 / 3, Screen.height * 2 / 3);
        GUI.DrawTexture(rect, texture);

        GUIStyle style = new GUIStyle();
        style.fontSize = 15;
        style.normal.textColor = UnityEngine.Color.black;
        style.alignment = TextAnchor.UpperCenter;
		
		
		// Minimal HSV 
 
        GUI.Label(new Rect(Screen.width * 0.5f / 10, Screen.height * 7.0f / 10, Screen.width / 10f, Screen.height / 10f), "Minimal Hue", style); 
        minValueH = GUI.HorizontalSlider(new Rect(Screen.width * 0.5f / 10, Screen.height * 7.5f / 10, Screen.width / 10f, Screen.height / 30f), minValueH, 0.0F, 180.0F); 
        GUI.Label(new Rect(Screen.width * 1.3f / 10, Screen.height * 7.4f / 10, Screen.width / 10f, Screen.height / 10f), minValueH.ToString("F2"), style);
        GUI.DrawTexture(new Rect(Screen.width * 0.5f / 10, Screen.height * 7.7f / 10, Screen.width / 10f, Screen.height / 35f), textureHue);

        GUI.Label(new Rect(Screen.width * 3.0f / 10, Screen.height * 7.0f / 10, Screen.width / 10f, Screen.height / 10f), "Minimal Saturation", style); 
        minValueS = GUI.HorizontalSlider(new Rect(Screen.width * 3.0f / 10, Screen.height * 7.5f / 10, Screen.width / 10f, Screen.height / 30f), minValueS, 0.0F, 255.0F); 
        GUI.Label(new Rect(Screen.width * 3.8f / 10, Screen.height * 7.4f / 10, Screen.width / 10f, Screen.height / 10f), minValueS.ToString("F2"), style);
        GUI.DrawTexture(new Rect(Screen.width * 3.0f / 10, Screen.height * 7.7f / 10, Screen.width / 10f, Screen.height / 30f), textureSaturation);

        GUI.Label(new Rect(Screen.width * 5.5f / 10, Screen.height * 7.0f / 10, Screen.width / 10f, Screen.height / 10f), "Minimal Value", style); 
        minValueV = GUI.HorizontalSlider(new Rect(Screen.width * 5.5f / 10, Screen.height * 7.5f / 10, Screen.width / 10f, Screen.height / 30f), minValueV, 0.0F, 255.0F); 
        GUI.Label(new Rect(Screen.width * 6.3f / 10, Screen.height * 7.4f / 10, Screen.width / 10f, Screen.height / 10f), minValueV.ToString("F2"), style);
        GUI.DrawTexture(new Rect(Screen.width * 5.5f / 10, Screen.height * 7.7f / 10, Screen.width / 10f, Screen.height / 30f), textureValue);


        // Maximal HSV 

        GUI.Label(new Rect(Screen.width * 0.5f / 10, Screen.height * 8.5f / 10, Screen.width / 10f, Screen.height / 10f), "Maximal Hue", style); 
        maxValueH = GUI.HorizontalSlider(new Rect(Screen.width * 0.5f / 10, Screen.height * 9.0f / 10, Screen.width / 10f, Screen.height / 30f), maxValueH, 0.0F, 180.0F); 
        GUI.Label(new Rect(Screen.width * 1.3f / 10, Screen.height * 8.9f / 10, Screen.width / 10f, Screen.height / 10f), maxValueH.ToString("F2"), style);
        GUI.DrawTexture(new Rect(Screen.width * 0.5f / 10, Screen.height * 9.2f / 10, Screen.width / 10f, Screen.height / 35f), textureHue);

        GUI.Label(new Rect(Screen.width * 3.0f / 10, Screen.height * 8.5f / 10, Screen.width / 10f, Screen.height / 10f), "Maximal Saturation", style); 
        maxValueS = GUI.HorizontalSlider(new Rect(Screen.width * 3.0f / 10, Screen.height * 9.0f / 10, Screen.width / 10f, Screen.height / 30f), maxValueS, 0.0F, 255.0F); 
        GUI.Label(new Rect(Screen.width * 3.8f / 10, Screen.height * 8.9f / 10, Screen.width / 10f, Screen.height / 10f), maxValueS.ToString("F2"), style);
        GUI.DrawTexture(new Rect(Screen.width * 3.0f / 10, Screen.height * 9.2f / 10, Screen.width / 10f, Screen.height / 30f), textureSaturation);

        GUI.Label(new Rect(Screen.width * 5.5f / 10, Screen.height * 8.5f / 10, Screen.width / 10f, Screen.height / 10f), "Maximal Value", style); 
        maxValueV = GUI.HorizontalSlider(new Rect(Screen.width * 5.5f / 10, Screen.height * 9.0f / 10, Screen.width / 10f, Screen.height / 30f), maxValueV, 0.0F, 255.0F); 
        GUI.Label(new Rect(Screen.width * 6.3f / 10, Screen.height * 8.9f / 10, Screen.width / 10f, Screen.height / 10f), maxValueV.ToString("F2"), style);
        GUI.DrawTexture(new Rect(Screen.width * 5.5f / 10, Screen.height * 9.2f / 10, Screen.width / 10f, Screen.height / 30f), textureValue);


        // Variables on the left of the screen 

        GUI.Label(new Rect(Screen.width * 4 / 5, Screen.height * 2.5f / 10, Screen.width / 10f, Screen.height / 10f), "Minimal size of the bridge", style);
        bridgeMinimalArea = GUI.HorizontalSlider(new Rect(Screen.width* 3.9f/5, Screen.height*3/10, Screen.width/10f, Screen.height/30f), bridgeMinimalArea, 0.1F, 5.0F); 
        GUI.Label(new Rect(Screen.width * 7 / 8, Screen.height * 2.95f / 10, Screen.width / 10f, Screen.height / 10f), bridgeMinimalArea.ToString("F2")+" %", style);
 
        GUI.Label(new Rect(Screen.width * 4 / 5, Screen.height * 3.5f / 10, Screen.width / 10f, Screen.height / 10f), "Capture size buffer", style);
        captureCyclesBuffer = (int) GUI.HorizontalSlider(new Rect(Screen.width* 3.9f / 5, Screen.height*4/10, Screen.width / 10f, Screen.height/30f), captureCyclesBuffer, 1.0F, 20.0F); 
        GUI.Label(new Rect(Screen.width * 7 / 8, Screen.height * 3.95f / 10, Screen.width / 10f, Screen.height / 10f), captureCyclesBuffer.ToString(), style);

		
        // TODO : Fix the usage of windowWidth and windowHeight in BridgeBehavior before decommenting those labels
		//GUI.Label(new Rect(Screen.width * 4 / 5, Screen.height * 4.5f / 10, Screen.width / 10f, Screen.height / 10f), "Camera Width", style); 
  //      windowWidth = (int)GUI.HorizontalSlider(new Rect(Screen.width * 3.9f / 5, Screen.height * 5 / 10, Screen.width / 10f, Screen.height / 30f), windowWidth, 320.0F, 1920.0F); 
  //      GUI.Label(new Rect(Screen.width * 7 / 8, Screen.height * 4.95f / 10, Screen.width / 10f, Screen.height / 10f), windowWidth.ToString(), style);
		
		//GUI.Label(new Rect(Screen.width * 4 / 5, Screen.height * 5.5f / 10, Screen.width / 10f, Screen.height / 10f), "Camera Height", style);
  //      windowHeight = (int)GUI.HorizontalSlider(new Rect(Screen.width * 3.9f / 5, Screen.height * 6 / 10, Screen.width / 10f, Screen.height / 30f), windowHeight, 240.0F, 1080.0F); 
  //      GUI.Label(new Rect(Screen.width * 7 / 8, Screen.height * 5.95f / 10, Screen.width / 10f, Screen.height / 10f), windowHeight.ToString(), style);



        if (GUI.Button(new Rect(Screen.width * 4 / 5, Screen.height * 8.0f / 10, Screen.width / 10f, Screen.height / 25f), "Validate")) 
        {
            PlayerPrefs.SetFloat("minValueH", minValueH);
            PlayerPrefs.SetFloat("minValueS", minValueS);
            PlayerPrefs.SetFloat("minValueV", minValueV);
            PlayerPrefs.SetFloat("maxValueH", maxValueH);
            PlayerPrefs.SetFloat("maxValueS", maxValueS);
            PlayerPrefs.SetFloat("maxValueV", maxValueV);
            PlayerPrefs.SetFloat("bridgeMinimalArea", bridgeMinimalArea / 100);
            PlayerPrefs.SetInt("captureCyclesBuffer", captureCyclesBuffer);
            PlayerPrefs.SetInt("windowWidth", windowWidth);
            PlayerPrefs.SetInt("windowHeight", windowHeight);

            PlayerPrefs.Save();

            SceneManager.LoadScene("MainMenu");
        }
    }

    void OnDestroy()
    {
		webcam.Dispose();
    }
}
