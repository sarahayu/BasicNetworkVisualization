using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TerrainMeshGenerator
{
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
