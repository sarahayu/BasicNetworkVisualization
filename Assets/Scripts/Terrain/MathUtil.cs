using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtil
{
    public static float TWO_SQRT_SIX = 2 * Mathf.Sqrt(6);
    public static LineDistItem LineDistSq(float x, float y, float x1, float y1, float x2, float y2) {
        //https://stackoverflow.com/a/6853926

        var A = x - x1;
        var B = y - y1;
        var C = x2 - x1;
        var D = y2 - y1;

        var dot = A * C + B * D;
        var len_sq = C * C + D * D;
        var param = -1f;
        if (len_sq != 0) //in case of 0 length line
            param = dot / len_sq;

        float xx, yy;

        if (param < 0) {
            xx = x1;
            yy = y1;
        }
        else if (param > 1) {
            xx = x2;
            yy = y2;
        }
        else {
            xx = x1 + param * C;
            yy = y1 + param * D;
        }

        var dx = x - xx;
        var dy = y - yy;

        return new LineDistItem{ param = param, dist_sq = dx * dx + dy * dy };
    }

    public static float clerp(float first, float second, float sag, float w) {
        if (sag < 0.0001) return Mathf.Lerp(first, second, w);
        var s = 1 + sag;
        var v = second - first;
        var a = 1 / (TWO_SQRT_SIX * Math.Sqrt(Math.Sqrt(s * s + v * v) - 1));
        var j = -a * Math.Log((first - second - Math.Sqrt(Math.Pow(second - first, 2) + a * a * Math.Pow(1 - Math.Exp(1 / a), 2) / Math.Exp(1 / a))) / (a * (1 - Math.Exp(1 / a))));
        var k = first - a * Math.Cosh(-j / a);
        var final = a * Math.Cosh((w - j) / a) + k;
        // print(j, k);
        return (float)final;
    }
    // consider v=(x,y), returns how much along <v2-v1> the projection of v on it is
    public static float proj_factor(float x, float y, float x1, float y1, float x2, float y2) {
        float ux = x - x1, uy = y - y1, vx = x2 - x1, vy = y2 - y1;
        return (ux * vx + uy * vy) / (vx * vx + vy * vy);
    }

    public static float distSq(float x1, float y1, float x2, float y2) {
        return Mathf.Pow(x1 - x2, 2) + Mathf.Pow(y1 - y2, 2);
    }
}

public class LineDistItem
{
    public float param;
    public float dist_sq;
}