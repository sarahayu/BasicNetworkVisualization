using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureUtil
{
    public static Texture2D GenerateNormalFromHeight(Texture2D heightMap, float scaleHeight, float meshRealWidth)
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
                    col = new Vector3(0, 1, 0);
                else
                {
                    float vx = 0f, vy = 0f;
                    if (x == 0 || x == width - 1)
                    {
                        vy = heightMap.GetPixelBilinear((float)x / (width - 1), ((float)y - 0.5f) / (height - 1)).r - heightMap.GetPixelBilinear((float)x / (width - 1), ((float)y + 0.5f) / (height - 1)).r;
                    }
                    else if (y == 0 || y == height - 1)
                    {
                        vx = heightMap.GetPixelBilinear(((float)x - 0.5f) / (width - 1), (float)y / (height - 1)).r - heightMap.GetPixelBilinear(((float)x + 0.5f) / (width - 1), (float)y / (height - 1)).r;
                    }
                    else
                    {
                        vx = heightMap.GetPixelBilinear(((float)x - 0.5f) / (width - 1), (float)y / (height - 1)).r - heightMap.GetPixelBilinear(((float)x + 0.5f) / (width - 1), (float)y / (height - 1)).r;
                        vy = heightMap.GetPixelBilinear((float)x / (width - 1), ((float)y - 0.5f) / (height - 1)).r - heightMap.GetPixelBilinear((float)x / (width - 1), ((float)y + 0.5f) / (height - 1)).r;
                    }

                    float vz = meshRealWidth / width / scaleHeight;

                    col = new Vector3(vx, vy, vz);

                }
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
        MathUtil.gaussBlur_4(r, res, width, height, 1);
        Array.Copy(res, r, res.Length);
        MathUtil.gaussBlur_4(g, res, width, height, 1);
        Array.Copy(res, g, res.Length);
        MathUtil.gaussBlur_4(b, res, width, height, 1);
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
    
    // need heightmap for converting node weight to height
    public static Texture2D GenerateNodeColsFromGraph(TerrainGraphData graph, HeightMap heightMap, int resX, int resY)
    {
        var colors = new Color[resX * resY];
        float pixToGraphCoord = graph.width / resX;

        for (int y = 0; y < resY; y++)
            for (int x = 0; x < resX; x++)
            {
                float gx = x * pixToGraphCoord, gy = y * pixToGraphCoord;

                Color finalColor = Color.clear;

                int count = 0;
                foreach (var node in graph.nodes)
                {
                    float dx = gx - node.x, dy = gy - node.y;
                    float distSq = dx * dx + dy * dy;
                    float rad = heightMap.GetRadiusFromNodeSize(node.size) * 0.8f, radSq = rad * rad;

                    if (distSq <= radSq)
                    {
                        // only take sqrt if we're close enough
                        float dist = Mathf.Sqrt(distSq);
                        float alpha = 1 - dist / rad;
                        var color = node.color;
                        color.a = alpha;
                        float finalAlph = color.a + finalColor.a * (1 - color.a);
                        finalColor.r = (color.r * color.a + finalColor.r * finalColor.a * (1 - color.a)) / finalAlph;
                        finalColor.g = (color.g * color.a + finalColor.g * finalColor.a * (1 - color.a)) / finalAlph;
                        finalColor.b = (color.b * color.a + finalColor.b * finalColor.a * (1 - color.a)) / finalAlph;
                        finalColor.a = finalAlph;
                    }

                    count++;
                }

                colors[y * resX + x] = finalColor;
            }

        var nodeColsTex = new Texture2D(resX, resY);
        nodeColsTex.wrapMode = TextureWrapMode.Clamp;
        nodeColsTex.SetPixels(colors);
        nodeColsTex.Apply();
        return nodeColsTex;
    }
}