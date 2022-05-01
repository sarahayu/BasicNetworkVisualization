using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TerrainObject : MonoBehaviour
{
    const int GRAPH_AREA_LEN = 81;
    const int TEX_RES = 360;

    public TextAsset networkJsonFile;
    // public GameObject nodePrefab;
    // public Camera worldCamera;
    // public Transform rightController;
    // public Transform leftController;
    
    // Transform cameraTransform;

    public bool autoUpdate = false;
    JSONNetworkData data;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    // int numControllersPressed = 0;
    // Vector3 ogControllerRelPos = Vector3.zero;
    // Vector3 ogControllersPos;
    // Vector3 ogNetworkScale;
    // Quaternion ogNetworkRot;
    // Vector3 ogNetworkPos;
    // Vector3[] grabPoints = new Vector3[2];
    // int numGrabPoints = 0;

    // void Start()
    // {        
    //     data = JsonUtility.FromJson<JSONNetworkData>(networkJsonFile.text);
    //     foreach (var node in data.nodes)
    //     {
    //         print(node.label);
    //     }
    // }

    // void FixedUpdate()
    // {
    // }

    public float scaleHeight = 50f;
    public float falloff = 1f;
    public int subdivide = 1;

    HeightMap _heightMap = null;
    Texture2D _terrainTex = null;

    public void GenerateTerrainLowQuality()
    {
        var graph = new TerrainGraphData();
        graph.links = new TerrainLinkData[] {
            new TerrainLinkData{ source = 0, target = 2, weight = 1 },
            new TerrainLinkData{ source = 1, target = 2, weight = 2 },
            new TerrainLinkData{ source = 1, target = 3, weight = 1 },
            new TerrainLinkData{ source = 2, target = 3, weight = 3 },
            new TerrainLinkData{ source = 2, target = 4, weight = 2 },
            new TerrainLinkData{ source = 3, target = 4, weight = 2 },
            new TerrainLinkData{ source = 4, target = 5, weight = 3 },
            new TerrainLinkData{ source = 5, target = 6, weight = 1 },
        };
        graph.nodes = new TerrainNodeData[] {
            new TerrainNodeData{ x = 130 * GRAPH_AREA_LEN / 720, y = 132 * GRAPH_AREA_LEN / 720, size = 1 },
            new TerrainNodeData{ x = 484 * GRAPH_AREA_LEN / 720, y = 77 * GRAPH_AREA_LEN / 720, size = 2 },
            new TerrainNodeData{ x = 267 * GRAPH_AREA_LEN / 720, y = 213 * GRAPH_AREA_LEN / 720, size = 5 },
            new TerrainNodeData{ x = 495 * GRAPH_AREA_LEN / 720, y = 268 * GRAPH_AREA_LEN / 720, size = 4 },
            new TerrainNodeData{ x = 171 * GRAPH_AREA_LEN / 720, y = 439 * GRAPH_AREA_LEN / 720, size = 3 },
            new TerrainNodeData{ x = 276 * GRAPH_AREA_LEN / 720, y = 600 * GRAPH_AREA_LEN / 720, size = 1 },
            new TerrainNodeData{ x = 543 * GRAPH_AREA_LEN / 720, y = 581 * GRAPH_AREA_LEN / 720, size = 3 },
        };

        _heightMap = new HeightMap(
            graph: graph,
            falloff: falloff * GRAPH_AREA_LEN, 
            graphWidth: GRAPH_AREA_LEN, 
            graphHeight: GRAPH_AREA_LEN
        );

        meshFilter.sharedMesh = TerrainMeshGenerator.GenerateFromGraph(
            graph: graph,
            graphWidth: GRAPH_AREA_LEN, 
            graphHeight: GRAPH_AREA_LEN,
            heightMap: _heightMap, 
            meshHeight: scaleHeight, 
            meshWidth: GRAPH_AREA_LEN, 
            meshLength: GRAPH_AREA_LEN,
            subdivide: subdivide,
            useNormalMap: false
        );


        var colFlat = new Color[TEX_RES * TEX_RES];
        for (int y = 0; y < TEX_RES; y++)
            for (int x = 0; x < TEX_RES; x++)
                colFlat[y * TEX_RES + x] = Color.white;

        var texFlat = new Texture2D(TEX_RES, TEX_RES);
        texFlat.SetPixels(colFlat);
        texFlat.Apply();
        meshRenderer.sharedMaterial.mainTexture = texFlat;
        meshRenderer.sharedMaterial.SetTexture("_BumpMap", null);
    }

    public void GenerateTerrainSmoothNormals()
    {
        if (_heightMap == null)
        {
            GenerateTerrainLowQuality();
        }

        TerrainMeshGenerator.FlattenNormals(meshFilter.sharedMesh);

        var texHeight = _heightMap.GenerateTexture(TEX_RES, TEX_RES);
        // meshRenderer.sharedMaterial.SetTexture("_HeightMap", texHeight);
        meshRenderer.sharedMaterial.SetTexture("_BumpMap", TextureUtil.GenerateNormalFromHeight(texHeight, scaleHeight, GRAPH_AREA_LEN));
    }

    void ModifyTerrain()
    {

    }
}
