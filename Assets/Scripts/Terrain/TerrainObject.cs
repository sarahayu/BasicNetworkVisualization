using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TerrainObject : MonoBehaviour
{
    const int GRAPH_AREA_LEN = 81;
    const int TEX_RES_NORMAL = 480;
    const int TEX_RES_ALBEDO = 1280;

    public TextAsset networkJsonFile;
    // public GameObject nodePrefab;
    // public Camera worldCamera;
    // public Transform rightController;
    // public Transform leftController;
    
    // Transform cameraTransform;

    public bool autoUpdate = false;

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
    public float lineColorIntensity = 0.2f;
    public AnimationCurve falloffShapeFunc;
    public AnimationCurve peakHeightFunc;
    public AnimationCurve slackFunc;
    /* public */ bool slackIsLevel;         // obsolete field, may remove later
    public float curvatureRadius = 100f;

    JSONNetworkData _data;
    TerrainGraphData _graph;
    HeightMap _heightMap = null;
    Texture2D _lineTex = null;
    Texture2D _heightTex = null;
    Texture2D _normalTex = null;
    Texture2D _nodeColTex = null;

    public void Awake()
    {
        print("Awaken");
    }

    public void GenerateTerrainLowQuality()
    {
        meshFilter.sharedMesh = TerrainMeshGenerator.GenerateFromGraph(
            graph: _graph,
            graphWidth: (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2), 
            graphHeight: (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2),
            heightMap: _heightMap, 
            meshHeight: scaleHeight, 
            meshWidth: (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2), 
            meshLength: (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2),
            subdivide: subdivide,
            radius: curvatureRadius,
            useNormalMap: false
        );

        var colFlat = new Color[TEX_RES_NORMAL * TEX_RES_NORMAL];
        for (int y = 0; y < TEX_RES_NORMAL; y++)
            for (int x = 0; x < TEX_RES_NORMAL; x++)
                colFlat[y * TEX_RES_NORMAL + x] = Color.black;

        var texFlat = new Texture2D(TEX_RES_NORMAL, TEX_RES_NORMAL);
        texFlat.SetPixels(colFlat);
        texFlat.Apply();
        meshRenderer.sharedMaterial.SetTexture("_LineTex", texFlat);
        meshRenderer.sharedMaterial.SetFloat("_MaxHeight", scaleHeight);
        meshRenderer.sharedMaterial.SetFloat("_CurvatureRadius", curvatureRadius);
    }

    public void ToggleAlbedoLines()
    {
        if (meshRenderer.sharedMaterial.GetTexture("_LineTex") == null)
        {
            GenerateTerrainTextureLines();
            meshRenderer.sharedMaterial.SetTexture("_LineTex", _lineTex);
        }
        else
            meshRenderer.sharedMaterial.SetTexture("_LineTex", null);
    }

    public void ToggleHeightMap()
    {
        if (meshRenderer.sharedMaterial.GetTexture("_HeightMap") == null)
        {
            GenerateTerrainTextureHeightMap();
            meshRenderer.sharedMaterial.SetTexture("_HeightMap", _heightTex);
            meshRenderer.sharedMaterial.SetInt("_UseHeightMap", 1);
        }
        else
        {
            meshRenderer.sharedMaterial.SetTexture("_HeightMap", null);
            meshRenderer.sharedMaterial.SetInt("_UseHeightMap", 0);
        }
    }

    public void ToggleNormalMap()
    {
        if (meshRenderer.sharedMaterial.GetTexture("_BumpMap") == null)
        {
            GenerateTerrainSmoothNormals();
            TerrainMeshGenerator.FlattenNormals(meshFilter.sharedMesh);
            meshRenderer.sharedMaterial.SetTexture("_BumpMap", _normalTex);
        }
        else
        {
            meshFilter.sharedMesh.RecalculateNormals();
            meshRenderer.sharedMaterial.SetTexture("_BumpMap", null);
        }
    }

    public void ToggleNodeColors()
    {
        if (meshRenderer.sharedMaterial.GetTexture("_NodeColTex") == null)
        {
            GenerateNodeColors();
            meshRenderer.sharedMaterial.SetTexture("_NodeColTex", _nodeColTex);
        }
        else
        {
            meshRenderer.sharedMaterial.SetTexture("_NodeColTex", null);
        }
    }

    public void GenerateTerrainTextureLines()
    {
        if (_heightMap == null)
            GenerateTerrainLowQuality();

        // regenerate line tex every time, it's not computationally expensive and we might change line opacity often
        // if (_lineTex == null)
        _lineTex = _heightMap.GenerateTextureLines(TEX_RES_ALBEDO, TEX_RES_ALBEDO, lineColorIntensity);
    }

    void GenerateTerrainTextureHeightMap()
    {
        if (_heightMap == null)
            GenerateTerrainLowQuality();

        if (_heightTex == null)
            _heightTex = _heightMap.GenerateTextureHeight(TEX_RES_NORMAL, TEX_RES_NORMAL);
    }

    void GenerateTerrainSmoothNormals()
    {
        if (_heightMap == null)
            GenerateTerrainLowQuality();

        if (_heightTex == null)
            GenerateTerrainTextureHeightMap();

        if (_normalTex == null)
            _normalTex = TextureUtil.GenerateNormalFromHeight(_heightTex, scaleHeight, GRAPH_AREA_LEN);

    }

    void GenerateNodeColors()
    {
        if (_heightMap == null)
            GenerateTerrainLowQuality();

        if (_nodeColTex == null)
            _nodeColTex = TextureUtil.GenerateNodeColsFromGraph(_graph, _heightMap, TEX_RES_ALBEDO, TEX_RES_ALBEDO);

    }

    public void Reset()
    {
        // var graph = new TerrainGraphData();
        // graph.links = new List<TerrainLinkData> {
        //     new TerrainLinkData{ source = 0, target = 2, weight = 1 },
        //     new TerrainLinkData{ source = 1, target = 2, weight = 2 },
        //     new TerrainLinkData{ source = 1, target = 3, weight = 1 },
        //     new TerrainLinkData{ source = 2, target = 3, weight = 3 },
        //     new TerrainLinkData{ source = 2, target = 4, weight = 2 },
        //     new TerrainLinkData{ source = 3, target = 4, weight = 2 },
        //     new TerrainLinkData{ source = 4, target = 5, weight = 3 },
        //     new TerrainLinkData{ source = 5, target = 6, weight = 1 },
        // };

        // int offset = (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) / 2) - GRAPH_AREA_LEN / 2;
        // graph.nodes = new List<TerrainNodeData> {
        //     new TerrainNodeData{ x = 130 * GRAPH_AREA_LEN / 720  + offset, y = 132 * GRAPH_AREA_LEN / 720  + offset, size = 1 },
        //     new TerrainNodeData{ x = 484 * GRAPH_AREA_LEN / 720  + offset, y = 77 * GRAPH_AREA_LEN / 720  + offset, size = 2 },
        //     new TerrainNodeData{ x = 267 * GRAPH_AREA_LEN / 720  + offset, y = 213 * GRAPH_AREA_LEN / 720  + offset, size = 5 },
        //     new TerrainNodeData{ x = 495 * GRAPH_AREA_LEN / 720  + offset, y = 268 * GRAPH_AREA_LEN / 720  + offset, size = 4 },
        //     new TerrainNodeData{ x = 171 * GRAPH_AREA_LEN / 720  + offset, y = 439 * GRAPH_AREA_LEN / 720  + offset, size = 3 },
        //     new TerrainNodeData{ x = 276 * GRAPH_AREA_LEN / 720  + offset, y = 600 * GRAPH_AREA_LEN / 720  + offset, size = 1 },
        //     new TerrainNodeData{ x = 543 * GRAPH_AREA_LEN / 720  + offset, y = 581 * GRAPH_AREA_LEN / 720  + offset, size = 3 },
        // };

        _data = JsonUtility.FromJson<JSONNetworkData>(networkJsonFile.text);
        _graph = TerrainGraphData.CreateFromJSONData(_data, (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2), (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2));

        _heightMap = new HeightMap(
            graph: _graph,
            graphWidth: (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2), 
            graphHeight: (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2),
            falloffDistance: falloff * GRAPH_AREA_LEN, 
            falloffShapeFunc: falloffShapeFunc,
            peakHeightFunc: peakHeightFunc,
            slackFunc: slackFunc,
            slackIsLevel: slackIsLevel
        );
        
        _lineTex = null;
        _heightTex = null;
        _normalTex = null;
        _nodeColTex = null;
        meshRenderer.sharedMaterial.SetTexture("_LineTex", null);
        meshRenderer.sharedMaterial.SetTexture("_BumpMap", null);
        meshRenderer.sharedMaterial.SetTexture("_HeightMap", null);
        meshRenderer.sharedMaterial.SetTexture("_NodeColTex", null);
        meshRenderer.sharedMaterial.SetFloat("_MaxHeight", scaleHeight);
        meshRenderer.sharedMaterial.SetFloat("_CurvatureRadius", curvatureRadius);
        meshRenderer.sharedMaterial.SetInt("_UseHeightMap", 0);
    }
}
