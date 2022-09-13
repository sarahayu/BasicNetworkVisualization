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
        
        TerrainUtil.PopulateCirclePoints(points, _meshWidth, _meshLength, _subdivide);
        TerrainUtil.PopulateRidgePoints(points, _graph, _graphWidth, _graphHeight, _meshWidth, _meshLength, _subdivide);
    
        // Choose triangulator: Incremental, SweepLine or Dwyer.
        var triangulator = new Dwyer();

        // Generate a default mesher.
        var mesher = new GenericMesher(triangulator);
        
        // Generate mesh.
        var mesh = mesher.Triangulate(points);

        return CreateMeshFromTriangles(mesh.Triangles, _heightMap, _meshHeight, _meshWidth, _meshLength, _radius, _useNormalMap);
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
