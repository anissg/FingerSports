using UnityEngine;

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
    [SerializeField] private UnityEngine.UI.Slider player1SliderH, player1SliderS, player1SliderV;
    [SerializeField] private UnityEngine.UI.Slider player2SliderH, player2SliderS, player2SliderV;
    [SerializeField] private UnityEngine.UI.Text player1TextH, player1TextS, player1TextV;
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

        player1SliderH.onValueChanged.AddListener(delegate { Player1HvalueChange(); });
        player1SliderS.onValueChanged.AddListener(delegate { Player1SvalueChange(); });
        player1SliderV.onValueChanged.AddListener(delegate { Player1VvalueChange(); });
        player2SliderH.onValueChanged.AddListener(delegate { Player2HvalueChange(); });
        player2SliderS.onValueChanged.AddListener(delegate { Player2SvalueChange(); });
        player2SliderV.onValueChanged.AddListener(delegate { Player2VvalueChange(); });

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
        player1TextH.text = player1SliderH.value.ToString();
        player1TextS.text = player1SliderS.value.ToString();
        player1TextV.text = player1SliderV.value.ToString();
        player2TextH.text = player2SliderH.value.ToString();
        player2TextS.text = player2SliderS.value.ToString();
        player2TextV.text = player2SliderV.value.ToString();

        // Get video flux
        Mat image = new Mat();
        videoCapture.Retrieve(image);

        // HSV image
        Mat imgHSV = image.Clone();
        CvInvoke.CvtColor(image, imgHSV, ColorConversion.Bgr2Hsv);

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
        menuManager.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    // Sliders callback

    void Player1HvalueChange()
    {
        player1HMin = (int)player1SliderH.value - 10;
        player1HMax = (int)player1SliderH.value + 10;
    }

    void Player1SvalueChange()
    {
        player1SMin = (int)player1SliderS.value - 25;
        player1SMax = (int)player1SliderS.value + 25;
    }

    void Player1VvalueChange()
    {
        player1VMin = (int)player1SliderV.value - 40;
        player1VMax = (int)player1SliderV.value + 40;
    }

    void Player2HvalueChange()
    {
        player2HMin = (int)player2SliderH.value - 10;
        player2HMax = (int)player2SliderH.value + 10;
    }

    void Player2SvalueChange()
    {
        player2SMin = (int)player2SliderS.value - 25;
        player2SMax = (int)player2SliderS.value + 25;
    }

    void Player2VvalueChange()
    {
        player2VMin = (int)player2SliderV.value - 40;
        player2VMax = (int)player2SliderV.value + 40;
    }
}
