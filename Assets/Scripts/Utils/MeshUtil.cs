using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
