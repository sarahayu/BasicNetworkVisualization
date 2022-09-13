using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using TriangleNet.Topology;

public static class TerrainUtil
{

    // unused for now
    public static void PopulateGridPoints(List<Vertex> points, int width, int height, int subdivide)
    {
        for (int y = 0; y < height + subdivide; y += subdivide)
        {
            for (int x = 0; x < width + subdivide; x += subdivide)
            {
                points.Add(new Vertex(Mathf.Min(x, width - 1), Mathf.Min(y, height - 1)));
            }
        }
    }

    public static void PopulateRidgePoints(List<Vertex> points, TerrainGraphData graph, int graphWidth, int graphHeight, int meshWidth, int meshLength, int subdivide)
    {
        foreach (var node in graph.nodes)
        {
            points.Add(new Vertex((float)node.x * (meshWidth - 1) / graphWidth, (float)node.y * (meshLength - 1) / graphHeight));
        }

        foreach (var link in graph.links)
        {
            var source = link.source;
            var target = link.target;
            var weight = link.weight;
            var first = graph.nodes[source];
            var second = graph.nodes[target];
            float x1 = first.x, y1 = first.y, size1 = first.size,
                x2 = second.x, y2 = second.y, size2 = second.size;
            float distX = Mathf.Abs(x2 - x1), distY = Mathf.Abs(y2 - y1);
            float dist = Mathf.Sqrt(distX * distX + distY * distY);

            var dir = new Vertex((x2 - x1) / dist / 4, (y2 - y1) / dist / 4);
            // dir.Normalize();
            // dir = dir / 2;
            float x = x1, y = y1;

            x += (float)dir.x * subdivide;
            y += (float)dir.y * subdivide;

            while (Mathf.Abs(x - x1) < distX && Mathf.Abs(y - y1) < distY)
            {
                points.Add(new Vertex(x * (meshWidth - 1) / graphWidth, y * (meshLength - 1) / graphHeight));
                x += (float)dir.x * subdivide;
                y += (float)dir.y * subdivide;
            }
            // points.Add(new Vertex(x2 * (meshWidth - 1) / graphWidth, y2 * (meshLength - 1) / graphHeight));
        }
    }

    public static void PopulateCirclePoints(List<Vertex> points, int width, int height, int subdivide)
    {
        float rad = (Mathf.Max(width, height) - 1) / 2; //Mathf.Sqrt((width - 1) * (width - 1) + (height - 1) * (height - 1)) / 2f;
        int n = (int)(Mathf.PI * rad * rad) / subdivide;
        var v3Points = new List<Vector3>();
        MathUtil.populateSunflower(v3Points, rad, n, 2);

        foreach (var point in v3Points)
            points.Add(new Vertex(point.x + (width - 1) / 2, point.z + (height - 1) / 2));
    }
}