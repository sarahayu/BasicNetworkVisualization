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
            terrain.GenerateTerrainTextureLines();
        }

        if (GUILayout.Button("Generate Low Poly"))
        {
            terrain.GenerateTerrainLowQuality();
        }

        if (GUILayout.Button("Toggle Line Texture"))
        {
            terrain.ToggleAlbedoLines();
        }

        if (GUILayout.Button("Toggle Height Map Texture"))
        {
            terrain.ToggleHeightMap();
        }

        if (GUILayout.Button("Toggle Normals"))
        {
            terrain.ToggleNormalMap();
        }

        if (GUILayout.Button("Toggle Node Colors"))
        {
            terrain.ToggleNodeColors();
        }

        if (GUILayout.Button("Reset"))
        {
            terrain.Reset();
        }
    }
}