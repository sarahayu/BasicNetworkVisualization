using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using TriangleNet.Topology;

public static class TerrainMeshGenerator
{
    static void PopulateGridPoints(List<Vertex> points, float w, float h)
    {
        for (int y = 0; y < h; y += 1)
        {
            for (int x = 0; x < w; x += 1)
            {
                points.Add(new Vertex(x, y));
            }
        }
    }

    static void PopulateRidgePoints(List<Vertex> points, TerrainGraphData graph)
    {
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

            while (Mathf.Abs(x - x1) < distX && Mathf.Abs(y - y1) < distY)
            {
                points.Add(new Vertex(x, y));
                x += (float)dir.x;
                y += (float)dir.y;
            }
            points.Add(new Vertex(x2, y2));
        }
    }

    public static Mesh GenerateFromGraph(TerrainGraphData graph, float scaleHeight, float falloff, int width, int height)
    {
        var points = new List<Vertex>();
        // points.Add(new Vector2(-100,100));
        // points.Add(new Vector2(100,100));
        // points.Add(new Vector2(100, 100));
        // points.Add(new Vector2(-100, 100));
        PopulateGridPoints(points, width, height);
        PopulateRidgePoints(points, graph);

    
        // Choose triangulator: Incremental, SweepLine or Dwyer.
        var triangulator = new Dwyer();

        // Generate a default mesher.
        var mesher = new GenericMesher(triangulator);
        
        // Generate mesh.
        var mesh = mesher.Triangulate(points);

        // var triangulation = new DelaunayTriangulation();
        // triangulation.Triangulate(points);
        // var triangles = new List<Triangle2D>();
        // triangulation.GetTrianglesDiscardingHoles(triangles);
        
        var heightMap = new HeightMap(
            graph: graph,
            scaleHeight: scaleHeight,
            falloff: falloff,
            width: width,
            height: height
        );

        return CreateMeshFromTriangles(mesh.Triangles, heightMap, scaleHeight, width, height);
    }
    
    static Mesh CreateMeshFromTriangles(ICollection<Triangle> triangles, HeightMap heightMap, float scaleHeight, float width, float height)
    {        
        float startX = (width - 1) / -2f, startZ = (height - 1) / 2f;
        
        List<Vector3> vertices = new List<Vector3>(triangles.Count * 3);
        List<int> indices = new List<int>(triangles.Count * 3);

        int i = 0;
        foreach (var triangle in triangles)
        {
            vertices.Add(new Vector3(startX + (float)triangle.GetVertex(0).x, scaleHeight * heightMap.maxWeightAt((float)triangle.GetVertex(0).x, (float)triangle.GetVertex(0).y), startZ - (float)triangle.GetVertex(0).y));
            vertices.Add(new Vector3(startX + (float)triangle.GetVertex(1).x, scaleHeight * heightMap.maxWeightAt((float)triangle.GetVertex(1).x, (float)triangle.GetVertex(1).y), startZ - (float)triangle.GetVertex(1).y));
            vertices.Add(new Vector3(startX + (float)triangle.GetVertex(2).x, scaleHeight * heightMap.maxWeightAt((float)triangle.GetVertex(2).x, (float)triangle.GetVertex(2).y), startZ - (float)triangle.GetVertex(2).y));
            indices.Add(i * 3);
            indices.Add(i * 3 + 1);
            indices.Add(i * 3 + 2); // Changes order
            i++;
        }


        // for (int i = 0; i < triangles.Count; ++i)
        // {
        //     vertices.Add(new Vector3(startX + triangles[i].GetVertex(0).x, scaleHeight * heightMap.maxWeightAt(triangles[i].p0.x, triangles[i].p0.y), startZ - triangles[i].p0.y));
        //     vertices.Add(new Vector3(startX + triangles[i].GetVertex(0).x, scaleHeight * heightMap.maxWeightAt(triangles[i].p1.x, triangles[i].p1.y), startZ - triangles[i].p1.y));
        //     vertices.Add(new Vector3(startX + triangles[i].GetVertex(0).x, scaleHeight * heightMap.maxWeightAt(triangles[i].p2.x, triangles[i].p2.y), startZ - triangles[i].p2.y));
        //     indices.Add(i * 3);
        //     indices.Add(i * 3 + 1);
        //     indices.Add(i * 3 + 2); // Changes order
        // }
        
        // List<Vector3> vertices = new List<Vector3>();
        // List<int> indices = new List<int>();
        
        // vertices.Add(new Vector3(-100, 0, 100));
        // vertices.Add(new Vector3(100, 0, 100));
        // vertices.Add(new Vector3(100, 0, -100));

        // vertices.Add(new Vector3(-100, 0, 100));
        // vertices.Add(new Vector3(100, 0, -100));
        // vertices.Add(new Vector3(-100, 0, -100));
        // indices.Add(0);
        // indices.Add(1);
        // indices.Add(2);
        // indices.Add(3);
        // indices.Add(4);
        // indices.Add(5);

        Mesh mesh = new Mesh();
        mesh.subMeshCount = 1;
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    public static Mesh GenerateFromHeights(float [,] heightMap)
    {
        int width = heightMap.GetLength(0), height = heightMap.GetLength(1);

        var vertices = new Vector3[width * height];
        var uvs = new Vector2[width * height];
        var triangles = new int[(width - 1) * (height - 1) * 6];

        float startX = (width - 1) / -2f, startZ = (height - 1) / 2f;
        int indVert = 0, indTri = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                vertices[indVert] = new Vector3(startX + x, heightMap[x, y], startZ - y);
                uvs[indVert] = new Vector2(x / (float)width, y / (float)height);

                if (x < width - 1 && y < height - 1)
                {
                    triangles[indTri++] = indVert;
                    triangles[indTri++] = indVert + width + 1;
                    triangles[indTri++] = indVert + width;
                    
                    triangles[indTri++] = indVert + width + 1;
                    triangles[indTri++] = indVert;
                    triangles[indTri++] = indVert + 1;
                }

                indVert++;
            }
        }

        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}
