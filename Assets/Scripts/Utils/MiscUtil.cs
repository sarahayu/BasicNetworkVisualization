using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MiscUtil
{
    public static Color TryGetColor(string colorStr)
    {
        Color color;
        if (!ColorUtility.TryParseHtmlString(colorStr, out color))
            color = Color.black;
        return color;
    }
}
