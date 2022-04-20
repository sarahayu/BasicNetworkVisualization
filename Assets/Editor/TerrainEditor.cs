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
            terrain.GenerateTerrain();
        }

        if (GUILayout.Button("Generate"))
        {
            terrain.GenerateTerrain();
        }
    }
}
