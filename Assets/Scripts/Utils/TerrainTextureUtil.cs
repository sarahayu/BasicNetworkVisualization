/**
 * File Description: General utility functions for generating textures
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TerrainTextureUtil
{
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
                        var color = node.color.ToRGB();
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