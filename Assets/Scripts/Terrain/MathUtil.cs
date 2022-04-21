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

    // http://blog.ivank.net/fastest-gaussian-blur.html
    // Photopea creator is the MVP
    public static float[,] gaussBlur_4 (float [,] scl, int r) {
        var w = scl.GetLength(0);
        var h = scl.GetLength(1);
        var tcl = new float[w, h];
        var bxs = boxesForGauss(r, 3);
        boxBlur_4 (scl, ref tcl, (bxs[0]-1)/2);
        boxBlur_4 (tcl, ref scl, (bxs[1]-1)/2);
        boxBlur_4 (scl, ref tcl, (bxs[2]-1)/2);
        return tcl;
    }

    static int[] boxesForGauss(float sigma, int n)  // standard deviation, number of boxes
    {
        var wIdeal = Mathf.Sqrt((12*sigma*sigma/n)+1);  // Ideal averaging filter width 
        var wl = Mathf.Floor(wIdeal);  if(wl%2==0) wl--;
        var wu = wl+2;
                    
        var mIdeal = (12*sigma*sigma - n*wl*wl - 4*n*wl - 3*n)/(-4*wl - 4);
        var m = Mathf.Round(mIdeal);
        // var sigmaActual = Math.sqrt( (m*wl*wl + (n-m)*wu*wu - n)/12 );
                    
        var sizes = new int[n];  for(var i=0; i<n; i++) sizes[i] = (int)(i<m?wl:wu);
        return sizes;
    }
    static void boxBlur_4 (float [,] scl, ref float [,] tcl, int r) {
        var w = scl.GetLength(0);
        var h = scl.GetLength(1);
        Array.Copy(scl, tcl, scl.Length);
        // for(var i=0; i< w * h; i++) tcl[i] = scl[i];
        boxBlurH_4(tcl, ref scl, r);
        boxBlurT_4(scl, ref tcl, r);
    }
    static void boxBlurH_4 (float [,] scl, ref float [,] tcl, int r) {
        var w = scl.GetLength(0);
        var h = scl.GetLength(1);
        var iarr = 1f / (r+r+1);
        for(var i=0; i<h; i++) {
            int ti = i*w, li = ti, ri = ti+r;
            float fv = scl[getX(ti, w), getY(ti, w)], lv = scl[getX(ti+w-1, w), getY(ti+w-1, w)], val = (r+1)*fv;
            for(var j=0; j<r; j++) val += scl[getX(ti+j, w), getY(ti+j, w)];
            for(var j=0  ; j<=r ; j++) { val += scl[getX(ri, w), getY(ri++, w)] - fv       ;   tcl[getX(ti, w), getY(ti++, w)] = Mathf.Round(val*iarr); }
            for(var j=r+1; j<w-r; j++) { val += scl[getX(ri, w), getY(ri++, w)] - scl[getX(li++, w), getY(li, w)];   tcl[getX(ti, w), getY(ti++, w)] = Mathf.Round(val*iarr); }
            for(var j=w-r; j<w  ; j++) { val += lv        - scl[getX(li, w), getY(li++, w)];   tcl[getX(ti, w), getY(ti++, w)] = Mathf.Round(val*iarr); }
        }
    }
    static void boxBlurT_4 (float [,] scl, ref float [,] tcl, int r) {
        var w = scl.GetLength(0);
        var h = scl.GetLength(1);
        var iarr = 1f / (r+r+1);
        for(var i=0; i<w; i++) {
            int ti = i, li = ti, ri = ti+r*w;
            float fv = scl[getX(ti,w), getY(ti, w)], lv = scl[getX(ti+w*(h-1), w), getY(ti+w*(h-1), w)], val = (r+1)*fv;
            for(var j=0; j<r; j++) val += scl[getX(ti+j*w, w), getY(ti+j*w, w)];
            for(var j=0  ; j<=r ; j++) { val += scl[getX(ri, w), getY(ri, w)] - fv     ;  tcl[getX(ti, w), getY(ti, w)] = Mathf.Round(val*iarr);  ri+=w; ti+=w; }
            for(var j=r+1; j<h-r; j++) { val += scl[getX(ri, w), getY(ri, w)] - scl[getX(li, w), getY(li, w)];  tcl[getX(ti, w), getY(ti, w)] = Mathf.Round(val*iarr);  li+=w; ri+=w; ti+=w; }
            for(var j=h-r; j<h  ; j++) { val += lv      - scl[getX(li, w), getY(li, w)];  tcl[getX(ti, w), getY(ti, w)] = Mathf.Round(val*iarr);  li+=w; ti+=w; }
        }
    }

    static int getX(int i, int w)
    {
        return i % w;
    }

    static int getY(int i, int w)
    {
        return i / w;
    }
}

public class LineDistItem
{
    public float param;
    public float dist_sq;
}