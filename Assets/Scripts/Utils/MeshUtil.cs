/**
 * File Description: Utility functions for general mesh-making
 **/

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using TriangleNet.Topology;

public static class MeshUtil
{
    public static void FlattenNormals(Mesh mesh, Vector3 origin)
    {
        List<Vector3> normals = new List<Vector3>(mesh.vertices.Length);

        foreach (var vert in mesh.vertices)
        {
            var normal = vert - origin;
            normal.Normalize();
            normals.Add(normal);
        }
        mesh.SetNormals(normals);
        // mesh.SetNormals(Enumerable.Repeat(Vector3.up, mesh.vertices.Length).ToList());
    }

    public static void PopulateCirclePoints(List<Vertex> points, int width, int height, int subdivide)
    {
        float rad = (Mathf.Max(width, height) - 1) / 2; //Mathf.Sqrt((width - 1) * (width - 1) + (height - 1) * (height - 1)) / 2f;
        int n = (int)(Mathf.PI * rad * rad) / subdivide;
        var v3Points = new List<Vector3>();
        MathUtil.PopulateSunflowerPoints(v3Points, rad, n, 2);

        foreach (var point in v3Points)
            points.Add(new Vertex(point.x + (width - 1) / 2, point.z + (height - 1) / 2));
    }

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
}
