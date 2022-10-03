using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using TriangleNet.Topology;

public class TerrainMeshPlotter
{
    TerrainGraph _graph;
    int _meshWidth;
    int _meshLength;
    int _subdivideSunflower;
    int _subdivideRidges;
    public List<Vertex> generatedPoints;        // make this public for convenience

    public TerrainMeshPlotter(TerrainGraph graph, int meshWidth, int meshLength, int subdivide = -1, int subdivideSunflower = -1, int subdivideRidges = -1)
    {
        _graph = graph;
        _meshWidth = meshWidth;
        _meshLength = meshLength;
        generatedPoints = new List<Vertex>();

        if (subdivide == -1)
        {
            _subdivideSunflower = subdivideSunflower;
            _subdivideRidges = subdivideRidges;
        }
        else
        {
            _subdivideSunflower = _subdivideRidges = subdivide;
        }
    }

    public IMesh GenerateMesh()
    {
        MeshUtil.PopulateCirclePoints(generatedPoints, _meshWidth, _meshLength, _subdivideSunflower);
        PopulateRidgePoints();
    
        var mesh = new GenericMesher(new Dwyer()).Triangulate(generatedPoints);

        return mesh;
    }
    
    void PopulateRidgePoints()
    {
        foreach (var node in _graph.nodes)
        {
            generatedPoints.Add(new Vertex((float)node.x * (_meshWidth - 1) / _graph.width, (float)node.y * (_meshLength - 1) / _graph.height));
        }

        foreach (var link in _graph.links)
        {
            var source = link.source;
            var target = link.target;
            var weight = link.weight;
            var first = _graph.nodes[source];
            var second = _graph.nodes[target];
            float x1 = first.x, y1 = first.y, size1 = first.size,
                x2 = second.x, y2 = second.y, size2 = second.size;
            float distX = Mathf.Abs(x2 - x1), distY = Mathf.Abs(y2 - y1);
            float dist = Mathf.Sqrt(distX * distX + distY * distY);

            var dir = new Vertex((x2 - x1) / dist / 4, (y2 - y1) / dist / 4);
            // dir.Normalize();
            // dir = dir / 2;
            float x = x1, y = y1;

            x += (float)dir.x * _subdivideSunflower;
            y += (float)dir.y * _subdivideSunflower;

            while (Mathf.Abs(x - x1) < distX && Mathf.Abs(y - y1) < distY)
            {
                generatedPoints.Add(new Vertex(x * (_meshWidth - 1) / _graph.width, y * (_meshLength - 1) / _graph.height));
                x += (float)dir.x * _subdivideSunflower;
                y += (float)dir.y * _subdivideSunflower;
            }
            // points.Add(new Vertex(x2 * (meshWidth - 1) / graphWidth, y2 * (meshLength - 1) / graphHeight));
        }
    }
}
