/**
 * File Description: Utility functions to help with color operations
 **/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ColorUtil
{
    public static Color TryGetColor(string colorStr)
    {
        Color color;
        if (!ColorUtility.TryParseHtmlString(colorStr, out color))
            color = Color.black;
        return color;
    }
    
    public static HSV TryGetHSV(string colorStr)
    {
        Color color;
        if (!ColorUtility.TryParseHtmlString(colorStr, out color))
            color = Color.black;
        return new HSV(color);
    }

    public static Color DuplicateWithGamma(Color color, float gamma)
    {
        return new Color(color.r, color.g, color.b, gamma);
    }

    // generate color that is high contrast
    // https://www.researchgate.net/figure/Kellys-22-colours-of-maximum-contrast_fig2_237005166
    // once kelly colors are exhausted, generate random hsv
    static int index = 0;
    static string[] kellys = { "#fdfdfd", "#1d1d1d", "#ebce2b", "#702c8c", "#db6917", "#96cde6", "#ba1c30", "#c0bd7f", "#7f7e80", "#5fa641", "#d485b2", "#4277b6", "#df8461", "#463397", "#e1a11a", "#91218c", "#e8e948", "#7e1510", "#92ae31", "#6f340d", "#d32b1e", "#2b3514" };

    public static HSV GenerateRandomHSV()
    {
        if (index < kellys.Length)
            return TryGetHSV(kellys[index++]);
        return new HSV(UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
    }

    public static void ResetRandomHSV()
    {
        index = 0;
    }
}
