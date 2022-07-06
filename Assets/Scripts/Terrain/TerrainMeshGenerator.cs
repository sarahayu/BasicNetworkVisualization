using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using TriangleNet.Topology;

public class TerrainMeshGenerator
{
    TerrainGraphData _graph;
    int _graphWidth;
    int _graphHeight;
    HeightMap _heightMap;
    float _meshHeight;
    int _meshWidth;
    int _meshLength;
    int _subdivide;
    float _radius;
    bool _useNormalMap; // unused for now

    public TerrainMeshGenerator(TerrainGraphData graph, int graphWidth, int graphHeight, HeightMap heightMap, float meshHeight, int meshWidth, int meshLength, int subdivide, float radius, bool useNormalMap)
    {
        _graph = graph;
        _graphWidth = graphWidth;
        _graphHeight = graphHeight;
        _heightMap = heightMap;
        _meshHeight = meshHeight;
        _meshWidth = meshWidth;
        _meshLength = meshLength;
        _subdivide = subdivide;
        _radius = radius;
        _useNormalMap = useNormalMap;
    }

    public Mesh GenerateFromGraph()
    {
        var points = new List<Vertex>();
        
        PopulateCirclePoints(points, _meshWidth, _meshLength, _subdivide);
        PopulateRidgePoints(points, _graph, _graphWidth, _graphHeight, _meshWidth, _meshLength, _subdivide);
    
        // Choose triangulator: Incremental, SweepLine or Dwyer.
        var triangulator = new Dwyer();

        // Generate a default mesher.
        var mesher = new GenericMesher(triangulator);
        
        // Generate mesh.
        var mesh = mesher.Triangulate(points);

        return CreateMeshFromTriangles(mesh.Triangles, _heightMap, _meshHeight, _meshWidth, _meshLength, _radius, _useNormalMap);
    }

    // unused for now
    static void PopulateGridPoints(List<Vertex> points, int width, int height, int subdivide)
    {
        for (int y = 0; y < height + subdivide; y += subdivide)
        {
            for (int x = 0; x < width + subdivide; x += subdivide)
            {
                points.Add(new Vertex(Mathf.Min(x, width - 1), Mathf.Min(y, height - 1)));
            }
        }
    }

    static void PopulateRidgePoints(List<Vertex> points, TerrainGraphData graph, int graphWidth, int graphHeight, int meshWidth, int meshLength, int subdivide)
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

    static void PopulateCirclePoints(List<Vertex> points, int width, int height, int subdivide)
    {
        float rad = (Mathf.Max(width, height) - 1) / 2; //Mathf.Sqrt((width - 1) * (width - 1) + (height - 1) * (height - 1)) / 2f;
        int n = (int)(Mathf.PI * rad * rad) / subdivide;
        var v3Points = new List<Vector3>();
        MathUtil.populateSunflower(v3Points, rad, n, 2);

        foreach (var point in v3Points)
            points.Add(new Vertex(point.x + (width - 1) / 2, point.z + (height - 1) / 2));
    }

    // assume origin to be a point at xz-plane origin minus some y
    static Vector3 flatToRoundCoords(Vector3 flatCoord, Vector3 origin)
    {
        var newCoord = flatCoord;
        float origY = flatCoord.y;
        newCoord.y = 0;
        var ray = newCoord - origin;
        ray.Normalize();
        ray *= -origin.y;
        ray *= (1 + origY / -origin.y);
        ray.y += origin.y;

        return ray;
    }

    public Vector2 LocalToTexPos(Vector3 localPos)
    {
        localPos.y += _radius;
        localPos.Normalize();
        localPos.x *= _radius / localPos.y;
        localPos.z *= _radius / localPos.y;
        float minBoundX = (_meshWidth - 1) / -2f, maxBoundX = (_meshWidth - 1) / 2f,
            minBoundZ = (_meshLength - 1) / -2f, maxBoundZ = (_meshLength - 1) / 2f;
        
        return new Vector2(
            MathUtil.rlerp(localPos.x, minBoundX, maxBoundX),
            MathUtil.rlerp(localPos.z, minBoundZ, maxBoundZ)
            );
    }
    
    static Mesh CreateMeshFromTriangles(ICollection<Triangle> triangles, HeightMap heightMap, float meshHeight, float meshWidth, float meshLength, float radius, bool useNormalMap)
    {        
        float startSubdivX = (meshWidth - 1) / -2f, startSubdivZ = (meshLength - 1) / 2f;
        
        List<Vector3> vertices = new List<Vector3>(triangles.Count * 3);
        List<int> indices = new List<int>(triangles.Count * 3);
        List<Vector2> uvs = new List<Vector2>(triangles.Count * 3);

        var vecOrigin = new Vector3(0, -radius, 0);

        int i = 0;
        foreach (var triangle in triangles)
        {
            Vertex p0 = triangle.GetVertex(0), p1 = triangle.GetVertex(1), p2 = triangle.GetVertex(2);
            float x0 = (float)p0.x, y0 = (float)p0.y,
                x1 = (float)p1.x, y1 = (float)p1.y,
                x2 = (float)p2.x, y2 = (float)p2.y;
            vertices.Add(flatToRoundCoords(new Vector3(startSubdivX + x0, meshHeight * heightMap.MaxWeightAt(x0 / (meshWidth - 1), y0 / (meshLength - 1)), startSubdivZ - y0), vecOrigin));
            vertices.Add(flatToRoundCoords(new Vector3(startSubdivX + x1, meshHeight * heightMap.MaxWeightAt(x1 / (meshWidth - 1), y1 / (meshLength - 1)), startSubdivZ - y1), vecOrigin));
            vertices.Add(flatToRoundCoords(new Vector3(startSubdivX + x2, meshHeight * heightMap.MaxWeightAt(x2 / (meshWidth - 1), y2 / (meshLength - 1)), startSubdivZ - y2), vecOrigin));
            indices.Add(i * 3);
            indices.Add(i * 3 + 1);
            indices.Add(i * 3 + 2); // Changes order
            uvs.Add(new Vector2(x0 / (meshWidth - 1), y0 / (meshLength - 1)));
            uvs.Add(new Vector2(x1 / (meshWidth - 1), y1 / (meshLength - 1)));
            uvs.Add(new Vector2(x2 / (meshWidth - 1), y2 / (meshLength - 1)));
            i++;
        }

        Mesh mesh = new Mesh();
        mesh.subMeshCount = 1;
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        mesh.SetUVs(0, uvs);

        if (useNormalMap)
            MeshUtil.FlattenNormals(mesh, vecOrigin);
        else
            mesh.RecalculateNormals();
        return mesh;
    }
}
