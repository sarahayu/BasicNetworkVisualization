using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMap
{
    TerrainGraphData _graph;
    float _falloffDistance;
    AnimationCurve _falloffShapeFunc;
    AnimationCurve _peakHeightFunc;
    AnimationCurve _slackFunc;
    int _graphWidth;
    int _graphHeight;
    bool _slackIsLevel;

    float _graphMaxWeight = -1f;
    float _graphMaxSize = -1f;

    public HeightMap(TerrainGraphData graph, int graphWidth, int graphHeight, float falloffDistance, 
        AnimationCurve falloffShapeFunc, AnimationCurve peakHeightFunc, AnimationCurve slackFunc, bool slackIsLevel)
    {
        _graph = graph;
        _graphWidth = graphWidth;
        _graphHeight = graphHeight;
        _falloffDistance = falloffDistance;
        _falloffShapeFunc = falloffShapeFunc;
        _peakHeightFunc = peakHeightFunc;
        _slackFunc = slackFunc;
        _slackIsLevel = slackIsLevel;

        calculateMaxes();
    }

    // public float[,] GenerateFromGraph(TerrainGraphData graph)
    // {
    //     var heightMap = new float[_width, _height];
    //     calculateMaxes(graph);
    //     for (int y = 0; y < _height; y++)
    //         for (int x = 0; x < _width; x++)
    //         {
    //             heightMap[x, y] = maxWeightAt(graph, x, y) * _scaleHeight;
    //         }

    //     // var blurred = MathUtil.gaussBlur_4(heightMap, 1);
    //     return heightMap;
    // }

    public Texture2D GenerateTextureHeight(int resX, int resY)
    {
        var heightVal = new float[resX * resY];

        for (int y = 0; y < resY; y++)
            for (int x = 0; x < resX; x++)
            {
                heightVal[y * resX + x] = maxWeightAt((float)x / (resX - 1), (float)y / (resY - 1));
            }
        
        float[] res = new float[resX * resX];
            
        // MathUtil.gaussBlur_4(heightVal, res, resX, resY, 1);
        
        var colors = new Color[resX * resY];

        for (int y = 0; y < resY; y++)
            for (int x = 0; x < resX; x++)
            {
                colors[y * resX + x] = Color.Lerp(Color.black, Color.white, heightVal[y * resY + x]);
            }

        var texture = new Texture2D(resX, resY);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    public Texture2D GenerateTextureAlbedo(int resX, int resY, float intensity)
    {
        var colorBit = new float[resX * resY];

        for (int y = 0; y < resY; y++)
            for (int x = 0; x < resX; x++)
            {
                colorBit[y * resX + x] = 0f;
            }


        foreach (var link in _graph.links)
        {
            var source = link.source;
            var target = link.target;
            var weight = link.weight;
            var first = _graph.nodes[source];
            var second = _graph.nodes[target];
            int x1 = (int)Mathf.Floor((float)(first.x - 0) / _graphWidth * (resX - 1)), y1 = (int)Mathf.Floor((float)(first.y - 0) / _graphHeight * (resY - 1)), size1 = first.size,
                x2 = (int)Mathf.Floor((float)(second.x - 0) / _graphWidth * (resX - 1)), y2 = (int)Mathf.Floor((float)(second.y - 0) / _graphHeight * (resY - 1)), size2 = second.size;
            int dist_x = Math.Abs(x1 - x2), dist_y = Math.Abs(y1 - y2);
            var dist = Mathf.Sqrt(dist_x * dist_x + dist_y * dist_y);
            float vx = (x2 - x1) / dist, vy = (y2 - y1) / dist;
            var minheight = Math.Min(size1, size2);
            var minridge = ridgeFunc(minheight, minheight, weight, 0);
            if (dist_x > dist_y)
            {
                for (var i = 0; i < dist_x; i++)
                {
                    int dx = vx > 0 ? i : -i, dy = (int)Mathf.Floor((float)(y2 - y1) / dist_x * i);
                    colorBit[(y1 + dy) * resX + x1 + dx] = intensity;
                }
            }
            else
            {
                for (var i = 0; i < dist_y; i++)
                {
                    int dx = (int)Mathf.Floor((float)(x2 - x1) / dist_y * i), dy = vy > 0 ? i : -i;
                    colorBit[(y1 + dy) * resX + x1 + dx] = intensity;
                }
            }
        }
        
        float[] res = new float[resX * resX];
            
        MathUtil.gaussBlur_4(colorBit, res, resX, resY, 1);
        
        var colors = new Color[resX * resY];

        for (int y = 0; y < resY; y++)
            for (int x = 0; x < resX; x++)
            {
                colors[y * resX + x] = Color.Lerp(Color.black, Color.white, res[y * resY + x]);
            }

        var texture = new Texture2D(resX, resY);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    void calculateMaxes()
    {
        foreach (var link in _graph.links)
            if (link.weight > _graphMaxWeight) _graphMaxWeight = link.weight;

        foreach (var node in _graph.nodes)
            if (node.size > _graphMaxSize) _graphMaxSize = node.size;
    }

    float ridgeFunc(float height1, float height2, float weight, float offset)
    {
        float lerpedHeight1 = _peakHeightFunc.Evaluate((height1 - 1) / (_graphMaxSize - 1));
        float lerpedHeight2 = _peakHeightFunc.Evaluate((height2 - 1) / (_graphMaxSize - 1));
        return Mathf.Max(0.01f, Mathf.Lerp(0, 1, (MathUtil.clerp(lerpedHeight1, lerpedHeight2, 1 - _slackFunc.Evaluate((weight - 1) / (_graphMaxWeight - 1)), offset))));
    }

    public float maxWeightAt(float ratioX, float ratioY)
    {
        float x = ratioX * (_graphWidth), y = ratioY * (_graphHeight);
        var maxWeight = 0f;
        foreach (var link in _graph.links)
        {
            var source = link.source;
            var target = link.target;
            var weight = link.weight;
            var first = _graph.nodes[source];
            var second = _graph.nodes[target];
            int x1 = first.x, y1 = first.y, size1 = first.size,
                x2 = second.x, y2 = second.y, size2 = second.size;
            int len_x = Math.Abs(x1 - x2), len_y = Math.Abs(y1 - y2);
            var len_sq = len_x * len_x + len_y * len_y;

            var w = MathUtil.proj_factor(x, y, x1, y1, x2, y2);

            if (_slackIsLevel)
            {
                size1 = size2 = Math.Min(size1, size2);
            }

            var heightAtPoint = Math.Max(0, ridgeFunc(size1, size2, weight, Math.Min(1, Math.Max(0, w))));
            var heightAt1 = ridgeFunc(size1, size2, weight, 0);
            var heightAt2 = ridgeFunc(size1, size2, weight, 1);

            var line_dist = MathUtil.LineDistSq(x, y, x1, y1, x2, y2);
            var param = line_dist.param;
            var distline_sq = line_dist.dist_sq;

            float maxHeightPoint = heightAt1;
            float relWeight;
            if (param < 0)
            {
                var dist = Mathf.Pow(MathUtil.distSq(x, y, x1, y1), 0.5f);
                relWeight = _falloffShapeFunc.Evaluate(Math.Max(0, 1 - dist / _falloffDistance / maxHeightPoint)) * heightAt1;
            }
            else if (param > 1)
            {
                var dist = Mathf.Pow(MathUtil.distSq(x, y, x2, y2), 0.5f);
                relWeight = _falloffShapeFunc.Evaluate(Math.Max(0, 1 - dist / _falloffDistance / maxHeightPoint)) * heightAt2;
            }
            else
            {
                var dist = Mathf.Pow(distline_sq, 0.5f);
                relWeight = _falloffShapeFunc.Evaluate(Math.Max(0, 1 - dist / _falloffDistance / maxHeightPoint)) * heightAtPoint;
            }

            if (relWeight > maxWeight) maxWeight = relWeight;
        }
        
        // TODO optimize, only check isolated nodes (nodes without links since they would have been looped above)
        foreach (var node in _graph.nodes)
        {
            int x2 = node.x, y2 = node.y, size2 = node.size;

            var dist = Mathf.Pow(Mathf.Pow(x2 - x, 2) + Mathf.Pow(y2 - y, 2), 0.5f);
            float maxHeightPoint = ridgeFunc(size2, size2, _graphMaxWeight, 0);
            float relWeight = _falloffShapeFunc.Evaluate(Math.Max(0, 1 - dist / _falloffDistance / maxHeightPoint)) * maxHeightPoint;

            if (relWeight > maxWeight) maxWeight = relWeight;
        }

        return maxWeight;
    }
}
