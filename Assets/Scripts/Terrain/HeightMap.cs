using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMap
{
    float _scaleHeight;
    float _falloff;
    int _width;
    int _height;

    float _graphMaxWeight = -1f;
    float _graphMaxSize = -1f;

    public HeightMap(float scaleHeight, float falloff, int width, int height)
    {
        _scaleHeight = scaleHeight;
        _falloff = falloff;
        _width = width;
        _height = height;
    }
    
    public float[,] GenerateFromGraph(TerrainGraphData graph)
    {
        var heightMap = new float[_width, _height];
        calculateMaxes(graph);
        for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
            {
                heightMap[x, y] = maxWeightAt(graph, x, y) * _scaleHeight;
            }

        var blurred = MathUtil.gaussBlur_4(heightMap, 1);
        return blurred;
    }

    void calculateMaxes(TerrainGraphData graph)
    {
        foreach (var link in graph.links)
            if (link.weight > _graphMaxWeight) _graphMaxWeight = link.weight;
            
        foreach (var node in graph.nodes)
            if (node.size > _graphMaxSize) _graphMaxSize = node.size;
    }

    float ridgeFunc(float height1, float height2, float weight, float offset)
    {
        return Mathf.Max(0.01f, Mathf.Lerp(0.25f, 1, (MathUtil.clerp((height1 - 1) / (_graphMaxSize - 1), (height2 - 1) / (_graphMaxSize - 1), Mathf.Lerp(0, 0.5f, 1 - (weight - 1) / (_graphMaxWeight - 1)), offset))));
    }

    float maxWeightAt(TerrainGraphData graph, float x, float y)
    {
        var maxWeight = 0f;
        foreach (var link in graph.links)
        {
            var source = link.source;
            var target = link.target;
            var weight = link.weight;
            var first = graph.nodes[source];
            var second = graph.nodes[target];
            int x1 = first.x, y1 = first.y, size1 = first.size,
                x2 = second.x, y2 = second.y, size2 = second.size;
            int len_x = Math.Abs(x1 - x2), len_y = Math.Abs(y1 - y2);
            var len_sq = len_x * len_x + len_y * len_y;

            var w = MathUtil.proj_factor(x, y, x1, y1, x2, y2);
            var minSize = Math.Min(size1, size2);

            var heightAtPoint = Math.Max(0, ridgeFunc(minSize, minSize, weight, Math.Min(1, Math.Max(0, w))));
            var heightAt1 = ridgeFunc(minSize, minSize, weight, 0);
            var heightAt2 = ridgeFunc(minSize, minSize, weight, 1);

            var line_dist = MathUtil.LineDistSq(x, y, x1, y1, x2, y2);
            var param = line_dist.param;
            var distline_sq = line_dist.dist_sq;

            var dist1 = Mathf.Pow(MathUtil.distSq(x, y, x1, y1), 0.5f / Mathf.Lerp(1, 3, heightAt1));
            var dist2 = Mathf.Pow(MathUtil.distSq(x, y, x2, y2), 0.5f / Mathf.Lerp(1, 3, heightAt2));
            var dist3 = Mathf.Pow(distline_sq, 0.5f / Mathf.Lerp(1, 3, heightAtPoint));

            var relWeight1 = heightAt1 - Mathf.Min(heightAt1, dist1 / (_falloff / Mathf.Lerp(5, 210, Mathf.Pow(Mathf.Max(0, heightAt1 - 0.1f), 2))));
            var relWeight2 = heightAt2 - Mathf.Min(heightAt2, dist2 / (_falloff / Mathf.Lerp(5, 210, Mathf.Pow(Mathf.Max(0, heightAt2 - 0.1f), 2))));
            var relWeight3 = heightAtPoint - Mathf.Min(heightAtPoint, dist3 / (_falloff / Mathf.Lerp(5, 210, Mathf.Pow(Mathf.Max(0, heightAtPoint - 0.1f), 2))));
            float relWeight;
            if (param < 0) relWeight = relWeight1;
            else if (param > 1) relWeight = relWeight2;
            else
            {
                relWeight = Math.Max(relWeight1, Math.Max(relWeight2, relWeight3));
            }

            if (relWeight > maxWeight) maxWeight = relWeight;
        }

        return maxWeight;
    }
}
