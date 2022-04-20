using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMap
{
    public static float[,] GenerateFromGraph(TerrainGraphData graph, int width, int height)
    {
        var heightMap = new float[width, height];
        float max_weight = 3f, max_size = 5f;
        int ind = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                heightMap[x,y] = maxWeightAt(graph, x, y, width, max_size, max_weight) * 100;
            }
        return heightMap;
    }

    // TODO normalize height with max size, weight with max weight to prevent addt. params
    static float ridgeFunc(float height1, float height2, float max_size, float weight, float max_weight, float offset)
    {
        return Mathf.Max(0.01f, Mathf.Lerp(0.25f, 1, (MathUtil.clerp((height1 - 1) / (max_size - 1), (height2 - 1) / (max_size - 1), Mathf.Lerp(0, 0.5f, 1 - (weight - 1) / (max_weight - 1)), offset))));
    }

    // TODO use something other than WIDTH to determine falloff rate?
    // TODO calculate max_weight in TerrainGraphData?
    static float maxWeightAt(TerrainGraphData graph, float x, float y, float WIDTH, float max_size, float max_weight)
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

            var heightAtPoint = Math.Max(0, ridgeFunc(minSize, minSize, max_size, weight, max_weight, Math.Min(1, Math.Max(0, w))));
            var heightAt1 = ridgeFunc(minSize, minSize, max_size, weight, max_weight, 0);
            var heightAt2 = ridgeFunc(minSize, minSize, max_size, weight, max_weight, 1);

            var line_dist = MathUtil.LineDistSq(x, y, x1, y1, x2, y2);
            var param = line_dist.param;
            var distline_sq = line_dist.dist_sq;

            var dist1 = Mathf.Pow(MathUtil.distSq(x, y, x1, y1), 0.5f / Mathf.Lerp(1, 3, heightAt1));
            var dist2 = Mathf.Pow(MathUtil.distSq(x, y, x2, y2), 0.5f / Mathf.Lerp(1, 3, heightAt2));
            var dist3 = Mathf.Pow(distline_sq, 0.5f / Mathf.Lerp(1, 3, heightAtPoint));

            var relWeight1 = heightAt1 - Mathf.Min(heightAt1, dist1 / (WIDTH / Mathf.Lerp(5, 210, Mathf.Pow(Mathf.Max(0, heightAt1 - 0.1f), 2))));
            var relWeight2 = heightAt2 - Mathf.Min(heightAt2, dist2 / (WIDTH / Mathf.Lerp(5, 210, Mathf.Pow(Mathf.Max(0, heightAt2 - 0.1f), 2))));
            var relWeight3 = heightAtPoint - Mathf.Min(heightAtPoint, dist3 / (WIDTH / Mathf.Lerp(5, 210, Mathf.Pow(Mathf.Max(0, heightAtPoint - 0.1f), 2))));
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
