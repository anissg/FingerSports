﻿using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

public class Settings : MonoBehaviour
{
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private UnityEngine.UI.Slider player1SliderHmin, player1SliderSmin, player1SliderVmin, player1SliderHmax, player1SliderSmax, player1SliderVmax;
    [SerializeField] private UnityEngine.UI.Slider player2SliderH, player2SliderS, player2SliderV;
    [SerializeField] private UnityEngine.UI.Text player1TextHmin, player1TextSmin, player1TextVmin, player1TextHmax, player1TextSmax, player1TextVmax;
    [SerializeField] private UnityEngine.UI.Text player2TextH, player2TextS, player2TextV;
    [SerializeField] private UnityEngine.UI.Image player1Camera;
    [SerializeField] private UnityEngine.UI.Image player2Camera;
    [SerializeField] private UnityEngine.UI.Button buttonSubmit;

    private VideoCapture videoCapture;
    private Texture2D player1Texture, player2Texture;
    private int player1HMin = 0, player1HMax = 179, player1SMin = 0, player1SMax = 255, player1VMin = 0, player1VMax = 255;
    private int player2HMin = 0, player2HMax = 179, player2SMin = 0, player2SMax = 255, player2VMin = 0, player2VMax = 255;

    // Start is called before the first frame update
    void Start()
    {
        videoCapture = new VideoCapture(0);
        videoCapture.FlipHorizontal = true;
        videoCapture.FlipVertical = true;

        videoCapture.ImageGrabbed += imageGrabbed;

        player1Texture = new Texture2D(videoCapture.Width, videoCapture.Height, TextureFormat.BGRA32, false);
        player2Texture = new Texture2D(videoCapture.Width, videoCapture.Height, TextureFormat.BGRA32, false);

        player1SliderHmin.onValueChanged.AddListener(delegate { Player1HminValueChange(); });
        player1SliderSmin.onValueChanged.AddListener(delegate { Player1SminValueChange(); });
        player1SliderVmin.onValueChanged.AddListener(delegate { Player1VminValueChange(); });
        player1SliderHmax.onValueChanged.AddListener(delegate { Player1HmaxValueChange(); });
        player1SliderSmax.onValueChanged.AddListener(delegate { Player1SmaxValueChange(); });
        player1SliderVmax.onValueChanged.AddListener(delegate { Player1VmaxValueChange(); });

        player2SliderH.onValueChanged.AddListener(delegate { Player2HvalueChange(); });
        player2SliderS.onValueChanged.AddListener(delegate { Player2SvalueChange(); });
        player2SliderV.onValueChanged.AddListener(delegate { Player2VvalueChange(); });

        if (PlayerPrefs.HasKey("player1conf"))
        {
            DetectionConfig player1conf = JsonUtility.FromJson<DetectionConfig>(PlayerPrefs.GetString("player1conf"));
            player1SliderHmax.value = player1conf.maxValueH;
            player1SliderSmax.value = player1conf.maxValueS;
            player1SliderVmax.value = player1conf.maxValueV;
            player1SliderHmin.value = player1conf.minValueH;
            player1SliderSmin.value = player1conf.minValueS;
            player1SliderVmin.value = player1conf.minValueV;
        }

        if (PlayerPrefs.HasKey("player2conf"))
        {
            DetectionConfig player2conf = JsonUtility.FromJson<DetectionConfig>(PlayerPrefs.GetString("player2conf"));
            player2SliderH.value = (player2conf.maxValueH + player2conf.minValueH) / 2;
            player2SliderS.value = (player2conf.maxValueS + player2conf.minValueS) / 2;
            player2SliderV.value = (player2conf.maxValueV + player2conf.minValueV) / 2;
        }

        buttonSubmit.onClick.AddListener(ButtonSubmit);
    }

    // Update is called once per frame
    void Update()
    {
        if (videoCapture.IsOpened) videoCapture.Grab();
    }

    void imageGrabbed(object sender, EventArgs e)
    {
        // Sliders
        player1TextHmin.text = player1SliderHmin.value.ToString();
        player1TextSmin.text = player1SliderSmin.value.ToString();
        player1TextVmin.text = player1SliderVmin.value.ToString();
        player1TextHmax.text = player1SliderHmax.value.ToString();
        player1TextSmax.text = player1SliderSmax.value.ToString();
        player1TextVmax.text = player1SliderVmax.value.ToString();
        
        player2TextH.text = player2SliderH.value.ToString();
        player2TextS.text = player2SliderS.value.ToString();
        player2TextV.text = player2SliderV.value.ToString();

        // Get video flux
        Mat image = new Mat();
        videoCapture.Retrieve(image);

        // HSV image
        Mat imgHSV = image.Clone();
        CvInvoke.CvtColor(image, imgHSV, ColorConversion.Bgr2Hsv);
        CvInvoke.GaussianBlur(imgHSV, imgHSV, new Size(25, 25), 0);

        // Blur image
        Mat blurHSV = imgHSV.Clone();
        CvInvoke.GaussianBlur(imgHSV, blurHSV, new Size(25, 25), 0);

        // To image
        Image<Hsv, Byte> img = blurHSV.ToImage<Hsv, Byte>();

        // Threshold
        Hsv player1LowerRange = new Hsv(player1HMin, player1SMin, player1VMin);
        Hsv player1UpperRange = new Hsv(player1HMax, player1SMax, player1VMax);
        Mat player1Threshold = img.InRange(player1LowerRange, player1UpperRange).Mat;

        Hsv player2LowerRange = new Hsv(player2HMin, player2SMin, player2VMin);
        Hsv player2UpperRange = new Hsv(player2HMax, player2SMax, player2VMax);
        Mat player2Threshold = img.InRange(player2LowerRange, player2UpperRange).Mat;

        // Erode
        Mat element = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(5, 5), new Point(2, 2));

        Mat player1Erode = player1Threshold.Clone();
        CvInvoke.Erode(player1Threshold, player1Erode, element, new Point(-1, -1), 3, BorderType.Constant, new MCvScalar(0));

        Mat player2Erode = player2Threshold.Clone();
        CvInvoke.Erode(player2Threshold, player2Erode, element, new Point(-1, -1), 3, BorderType.Constant, new MCvScalar(0));

        // Dilate
        element = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(5, 5), new Point(2, 2));

        Mat player1Dilate = player1Erode.Clone();
        CvInvoke.Dilate(player1Erode, player1Dilate, element, new Point(-1, -1), 3, BorderType.Constant, new MCvScalar(0));

        Mat player2Dilate = player2Erode.Clone();
        CvInvoke.Dilate(player2Erode, player2Dilate, element, new Point(-1, -1), 3, BorderType.Constant, new MCvScalar(0));

        // // Contours player 1
        // VectorOfVectorOfPoint player1Contours = new VectorOfVectorOfPoint();
        // Mat player1Hierarchy = new Mat();
        // CvInvoke.FindContours(player1Dilate, player1Contours, player1Hierarchy, RetrType.List, ChainApproxMethod.ChainApproxSimple);

        // Mat player1ImageContours = player1Dilate.Clone();

        // // Contours player 2
        // VectorOfVectorOfPoint player2Contours = new VectorOfVectorOfPoint();
        // Mat player2Hierarchy = new Mat();
        // CvInvoke.FindContours(player2Dilate, player2Contours, player2Hierarchy, RetrType.List, ChainApproxMethod.ChainApproxSimple);

        // Mat player2ImageContours = player2Dilate.Clone();

        // // Find biggest contour player 1
        // if (player1Contours.Size > 0)
        // {
        //     int player1BiggestContourIndex = 0;
        //     VectorOfPoint player1BiggestContour = player1Contours[player1BiggestContourIndex];
        //     double player1BiggestContourArea = CvInvoke.ContourArea(player1BiggestContour);

        //     for (int i = 1; i < player1Contours.Size; i++)
        //     {
        //         if (CvInvoke.ContourArea(player1Contours[i]) > player1BiggestContourArea)
        //         {
        //             player1BiggestContourIndex = i;
        //             player1BiggestContour = player1Contours[player1BiggestContourIndex];
        //             player1BiggestContourArea = CvInvoke.ContourArea(player1BiggestContour);
        //         }
        //     }
        //     CvInvoke.DrawContours(player1ImageContours, player1Contours, player1BiggestContourIndex, new MCvScalar(255, 0, 0), 5);
        // }

        // // Find biggest contour player 2
        // if (player2Contours.Size > 0)
        // {
        //     int player2BiggestContourIndex = 0;
        //     VectorOfPoint player2BiggestContour = player1Contours[player2BiggestContourIndex];
        //     double player2BiggestContourArea = CvInvoke.ContourArea(player2BiggestContour);

        //     for (int i = 1; i < player2Contours.Size; i++)
        //     {
        //         if (CvInvoke.ContourArea(player2Contours[i]) > player2BiggestContourArea)
        //         {
        //             player2BiggestContourIndex = i;
        //             player2BiggestContour = player2Contours[player2BiggestContourIndex];
        //             player2BiggestContourArea = CvInvoke.ContourArea(player2BiggestContour);
        //         }
        //     }
        //     CvInvoke.DrawContours(player2ImageContours, player2Contours, player2BiggestContourIndex, new MCvScalar(0, 0, 255), 5);
        // }

        if (player1Camera != null) applyTextureToImage(player1Camera, player1Texture, player1Dilate);

        if (player2Camera != null) applyTextureToImage(player2Camera, player2Texture, player2Dilate);
    }

    void applyTextureToImage(UnityEngine.UI.Image image, Texture2D texture, Mat mat)
    {
        texture.LoadRawTextureData(mat.ToImage<Bgra, Byte>().Bytes);
        texture.Apply();
        image.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1.0f);
    }

    void OnDestroy()
    {
        videoCapture.Stop();
    }

    void ButtonSubmit()
    {
        // Recuperer les valeurs Hmin, Hmax, Smin, Smax, Vmin, Vmax pour les deux joueurs

        DetectionConfig player1conf = new DetectionConfig()
        {
            minValueH = player1HMin,
            minValueS = player1SMin,
            minValueV = player1VMin,
            maxValueH = player1HMax,
            maxValueS = player1SMax,
            maxValueV = player1VMax
        };
        PlayerPrefs.SetString("player1conf", JsonUtility.ToJson(player1conf));

        DetectionConfig player2conf = new DetectionConfig()
        {
            minValueH = player2HMin,
            minValueS = player2SMin,
            minValueV = player2VMin,
            maxValueH = player2HMax,
            maxValueS = player2SMax,
            maxValueV = player2VMax
        };
        PlayerPrefs.SetString("player2conf", JsonUtility.ToJson(player2conf));

        menuManager.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    // Sliders callback

    void Player1HminValueChange()
    {
        player1HMin = (int)player1SliderHmin.value;
    }

    void Player1SminValueChange()
    {
        player1SMin = (int)player1SliderSmin.value;
    }

    void Player1VminValueChange()
    {
        player1VMin = (int)player1SliderVmin.value;
    }
    void Player1HmaxValueChange()
    {
        player1HMax = (int)player1SliderHmax.value;
    }

    void Player1SmaxValueChange()
    {
        player1SMax = (int)player1SliderSmax.value;
    }

    void Player1VmaxValueChange()
    {
        player1VMax = (int)player1SliderVmax.value;
    }




    void Player2HvalueChange()
    {
        player2HMin = (int)player2SliderH.value - 10;
        player2HMax = (int)player2SliderH.value + 10;
    }

    void Player2SvalueChange()
    {
        player2SMin = (int)player2SliderS.value - 50;
        player2SMax = (int)player2SliderS.value + 50;
    }

    void Player2VvalueChange()
    {
        player2VMin = (int)player2SliderV.value - 50;
        player2VMax = (int)player2SliderV.value + 50;
    }
}
