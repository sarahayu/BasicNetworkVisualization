using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureUtil
{
    public static Texture2D GenerateNormalFromHeight(Texture2D heightMap, float scaleHeight, float meshResWidth)
    {
        int width = heightMap.width, height = heightMap.height;
        var colors = new Vector3[width * height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int ind = y * width + x;
                Vector3 col;
                if ((x == 0 && y == 0)
                 || (x == 0 && y == height - 1)
                 || (x == width - 1 && y == height - 1)
                 || (x == width - 1 && y == 0))
                    col = new Vector3(0, 0, 1);
                else if (x == 0 || x == width - 1)
                    col = new Vector3(
                        0,
                        meshResWidth / width / scaleHeight,
                        heightMap.GetPixelBilinear((float)x / (width - 1), ((float)y - 0.5f) / (height - 1)).r - heightMap.GetPixelBilinear((float)x / (width - 1), ((float)y + 0.5f) / (height - 1)).r
                    );
                else if (y == 0 || y == height - 1)
                    col = new Vector3(
                        heightMap.GetPixelBilinear(((float)x - 0.5f) / (width - 1), (float)y / (height - 1)).r - heightMap.GetPixelBilinear(((float)x + 0.5f) / (width - 1), (float)y / (height - 1)).r,
                        meshResWidth / width / scaleHeight,
                        0
                    );
                else
                    col = new Vector3(
                        heightMap.GetPixelBilinear(((float)x - 0.5f) / (width - 1), (float)y / (height - 1)).r - heightMap.GetPixelBilinear(((float)x + 0.5f) / (width - 1), (float)y / (height - 1)).r,
                        meshResWidth / width / scaleHeight,
                        heightMap.GetPixelBilinear((float)x / (width - 1), ((float)y - 0.5f) / (height - 1)).r - heightMap.GetPixelBilinear((float)x / (width - 1), ((float)y + 0.5f) / (height - 1)).r
                    );
                col.Normalize();
                colors[ind] = (col + Vector3.one) / 2;
            }

        float[] r = new float[width * height];
        float[] g = new float[width * height];
        float[] b = new float[width * height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                r[y * width + x] = colors[y * width + x].x;
                g[y * width + x] = colors[y * width + x].y;
                b[y * width + x] = colors[y * width + x].z;
            }

        float[] res = new float[width * height];
        MathUtil.gaussBlur_4(r, res, width, height, 2);
        Array.Copy(res, r, res.Length);
        MathUtil.gaussBlur_4(g, res, width, height, 2);
        Array.Copy(res, g, res.Length);
        MathUtil.gaussBlur_4(b, res, width, height, 2);
        Array.Copy(res, b, res.Length);

        var colColors = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                colors[y * width + x].x = r[y * width + x];
                colors[y * width + x].y = g[y * width + x];
                colors[y * width + x].z = b[y * width + x];
                colors[y * width + x].Normalize();
                colColors[y * width + x] = new Color(colors[y * width + x].x, colors[y * width + x].y, colors[y * width + x].z, 1);
            }


        var normalMap = new Texture2D(width, height);
        normalMap.wrapMode = TextureWrapMode.Clamp;
        normalMap.SetPixels(colColors);
        normalMap.Apply();
        return normalMap;
    }
}