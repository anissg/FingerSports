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

public class Utils {

    public static Hsv ConvertRgbToHsv(float r, float b, float g)
    {
        float delta, min;
        float h = 0, s = 0, v = 0;

        min = Math.Min(Math.Min(r, g), b);
        v = Math.Max(Math.Max(r, g), b);
        delta = v - min;

        if (v == 0.0)
            s = 0;
        else
            s = delta / v;

        if (s == 0)
            h = 360;
        else
        {
            if (r == v)
                h = (g - b) / delta;
            else if (g == v)
                h = 2 + (b - r) / delta;
            else if (b == v)
                h = 4 + (r - g) / delta;

            h *= 60;
            if (h <= 0.0)
                h += 360;
        }
        


        Hsv hsvColor = new Hsv();
        hsvColor.Hue = 360 - h;
        hsvColor.Satuation = s;
        hsvColor.Value = v / 255;

       

        return hsvColor;

    }

    public static Texture2D ConvertFromMatToTex2D(Mat matImage, Texture2D texture, int display_WIDTH, int display_HEIGHT)
    {

        // texture size must also be equal to display_WIDTH,display_HEIGHT !!
        CvInvoke.Resize(matImage, matImage, new Size(display_WIDTH, display_HEIGHT));

        CvInvoke.CvtColor(matImage, matImage, ColorConversion.Bgra2Rgba);
        CvInvoke.Flip(matImage, matImage, FlipType.Vertical);

        texture.LoadRawTextureData(matImage.ToImage<Rgba, Byte>().Bytes);
        texture.Apply();

        return texture;
    }

    /// <summary>
    /// Convert HSV to RGB
    /// h is from 0-360
    /// s,v values are 0-1
    /// r,g,b values are 0-255
    /// Based upon http://ilab.usc.edu/wiki/index.php/HSV_And_H2SV_Color_Space#HSV_Transformation_C_.2F_C.2B.2B_Code_2
    /// </summary>
    public static void HsvToRgb(float h, float S, float V, out float r, out float g, out float b)
    {
        h *= 2;
        S /= 255;
        V /= 255;
        float H = h;
        while (H < 0) { H += 360; };
        while (H >= 360) { H -= 360; };
        float R, G, B;
        if (V <= 0)
        { R = G = B = 0; }
        else if (S <= 0)
        {
            R = G = B = V;
        }
        else
        {
            float hf = H / 60.0f;
            int i = (int)Math.Floor(hf);
            float f = hf - i;
            float pv = V * (1 - S);
            float qv = V * (1 - S * f);
            float tv = V * (1 - S * (1 - f));
            switch (i)
            {

                // Red is the dominant color

                case 0:
                    R = V;
                    G = tv;
                    B = pv;
                    break;

                // Green is the dominant color

                case 1:
                    R = qv;
                    G = V;
                    B = pv;
                    break;
                case 2:
                    R = pv;
                    G = V;
                    B = tv;
                    break;

                // Blue is the dominant color

                case 3:
                    R = pv;
                    G = qv;
                    B = V;
                    break;
                case 4:
                    R = tv;
                    G = pv;
                    B = V;
                    break;

                // Red is the dominant color

                case 5:
                    R = V;
                    G = pv;
                    B = qv;
                    break;

                // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                case 6:
                    R = V;
                    G = tv;
                    B = pv;
                    break;
                case -1:
                    R = V;
                    G = pv;
                    B = qv;
                    break;

                // The color is not defined, we should throw an error.

                default:
                    //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                    R = G = B = V; // Just pretend its black/white
                    break;
            }
        }
        r = Clamp((int)(R * 255.0));
        g = Clamp((int)(G * 255.0));
        b = Clamp((int)(B * 255.0));
    }

    /// <summary>
    /// Clamp a value to 0-255
    /// </summary>
    public static int Clamp(int i)
    {
        if (i < 0) return 0;
        if (i > 255) return 255;
        return i;
    }
}
