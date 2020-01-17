using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;

using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using UnityEngine;

public class Utils
{
    public static Texture2D ConvertMatToTex2D(Mat matImage, Texture2D texture, int display_WIDTH, int display_HEIGHT)
    {
        // texture size must also be equal to display_WIDTH,display_HEIGHT !!
        CvInvoke.Resize(matImage, matImage, new Size(display_WIDTH, display_HEIGHT));

        CvInvoke.CvtColor(matImage, matImage, ColorConversion.Bgra2Rgba);
        CvInvoke.Flip(matImage, matImage, FlipType.Vertical);

        texture.LoadRawTextureData(matImage.ToImage<Rgba, Byte>().Bytes);
        texture.Apply();

        return texture;
    }

    /// Clamp a value between 0 and 255
    public static int Clamp(int i)
    {
        if (i < 0) return 0;
        if (i > 255) return 255;
        return i;
    }
}
