using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(TerrainObject))]
public class TerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainObject terrain = (TerrainObject)target;

        if (DrawDefaultInspector() && terrain.autoUpdate)
        {
            terrain.GenerateTerrainTextureAlbedo();
        }

        if (GUILayout.Button("Generate Low Poly"))
        {
            terrain.GenerateTerrainLowQuality();
        }

        if (GUILayout.Button("Generate Line Texture"))
        {
            terrain.GenerateTerrainTextureAlbedo();
        }

        if (GUILayout.Button("Generate Height Map Texture"))
        {
            terrain.GenerateTerrainTextureHeightMap();
        }

        if (GUILayout.Button("Generate With Normals"))
        {
            terrain.GenerateTerrainSmoothNormals();
        }
    }
}