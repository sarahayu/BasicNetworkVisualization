using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class TerrainObject : MonoBehaviour
{
    const int GRAPH_AREA_LEN = 81;
    const int TEX_RES_NORMAL = 480;
    const int TEX_RES_ALBEDO = 1280;

    public SelectionEvent OnSelected;
    public DeselectionEvent OnDeselected;

    public NetworkData networkData;

    public bool autoUpdate = false;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;
    public Transform laserBox;

    public float scaleHeight = 50f;
    public float falloff = 1f;
    public int subdivide = 1;
    public float lineColorIntensity = 0.2f;
    public AnimationCurve falloffShapeFunc;
    public AnimationCurve peakHeightFunc;
    public AnimationCurve slackFunc;
    /* public */ bool slackIsLevel;         // obsolete field, may remove later
    public float curvatureRadius = 100f;
    public Transform parent;

    FootballFileData _parsedFileData;
    TerrainGraphData _graph;
    TerrainMeshGenerator _meshGenerator;
    TerrainOutline _terrainOutline;
    HeightMap _heightMap = null;
    Texture2D _lineTex = null;
    Texture2D _heightTex = null;
    Texture2D _normalTex = null;
    Texture2D _nodeColTex = null;
    Texture2D _selectionTex = null;
    bool _rightGripPressed = false;
    bool _leftGripPressed = false;


    public void Start()
    {
        _selectionTex = new Texture2D(360, 360);
        _terrainOutline = new TerrainOutline(_selectionTex);

        transform.parent = parent;

        Reset();
        ToggleAlbedoLines();
        ToggleHeightMap();
        ToggleNormalMap();
        ToggleNodeColors();

        GetMeshMaterial().SetTexture("_SelectionTex", _selectionTex);
    }

    public void ControllerTriggerPressRight()
    {
        _rightGripPressed = true;
    }

    public void ControllerTriggerReleaseRight()
    {
        _rightGripPressed = false;
        _terrainOutline.ConnectAndUpdate();
        OnSelected.Invoke(new SelectionEventData() {
            groupsSelected = _graph.GetContainedInOutline(_terrainOutline)
        });
    }

    public void ControllerTriggerPressleft()
    {
        _leftGripPressed = true;
        _terrainOutline.ClearPoints();
        OnDeselected.Invoke(new DeselectionEventData());
    }

    public void ControllerTriggerReleaseleft()
    {
        _leftGripPressed = false;
    }

    public void FixedUpdate()
    {
        if (_rightGripPressed)
        {
            var sourcePos = laserBox.position - laserBox.forward * laserBox.localScale.z / 2;
            Ray ray = new Ray(sourcePos, laserBox.forward);
            RaycastHit hit;

            if (meshCollider.Raycast(ray, out hit, Mathf.Infinity))
            {
                var worldCollPos = sourcePos + laserBox.forward * hit.distance;
                var localPos = transform.InverseTransformPoint(worldCollPos);
                var texPos = _meshGenerator.LocalToTexPos(localPos);

                _terrainOutline.AddPointAndUpdate(texPos);
            }    
        }
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

        networkData.ParseFromString();

        _parsedFileData = JsonUtility.FromJson<FootballFileData>(networkData.dataFile.text);
        _graph = TerrainGraphData.CreateFromJSONData(networkData, _parsedFileData, (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2), (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2));

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

        _meshGenerator = new TerrainMeshGenerator(
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
        
        _lineTex = null;
        _heightTex = null;
        _normalTex = null;
        _nodeColTex = null;

        var mMaterial = GetMeshMaterial();

        mMaterial.SetTexture("_LineTex", null);
        mMaterial.SetTexture("_BumpMap", null);
        mMaterial.SetTexture("_HeightMap", null);
        mMaterial.SetTexture("_NodeColTex", null);
        mMaterial.SetFloat("_MaxHeight", scaleHeight);
        mMaterial.SetFloat("_CurvatureRadius", curvatureRadius);
        mMaterial.SetInt("_UseHeightMap", 0);
    }

    public void GenerateTerrainLowQuality()
    {
        meshFilter.sharedMesh = _meshGenerator.GenerateFromGraph();

        var colFlat = new Color[TEX_RES_NORMAL * TEX_RES_NORMAL];
        for (int y = 0; y < TEX_RES_NORMAL; y++)
            for (int x = 0; x < TEX_RES_NORMAL; x++)
                colFlat[y * TEX_RES_NORMAL + x] = Color.black;

        var texFlat = new Texture2D(TEX_RES_NORMAL, TEX_RES_NORMAL);
        texFlat.SetPixels(colFlat);
        texFlat.Apply();

        var mMaterial = GetMeshMaterial();

        mMaterial.SetTexture("_LineTex", texFlat);
        mMaterial.SetFloat("_MaxHeight", scaleHeight);
        mMaterial.SetFloat("_CurvatureRadius", curvatureRadius);
    }

    public void ToggleAlbedoLines()
    {
        var mMaterial = GetMeshMaterial();

        if (mMaterial.GetTexture("_LineTex") == null)
        {
            GenerateTerrainTextureLines();
            mMaterial.SetTexture("_LineTex", _lineTex);
        }
        else
            mMaterial.SetTexture("_LineTex", null);
    }

    public void ToggleHeightMap()
    {
        var mMaterial = GetMeshMaterial();

        if (mMaterial.GetTexture("_HeightMap") == null)
        {
            GenerateTerrainTextureHeightMap();
            mMaterial.SetTexture("_HeightMap", _heightTex);
            mMaterial.SetInt("_UseHeightMap", 1);
        }
        else
        {
            mMaterial.SetTexture("_HeightMap", null);
            mMaterial.SetInt("_UseHeightMap", 0);
        }
    }

    public void ToggleNormalMap()
    {
        var mMaterial = GetMeshMaterial();

        if (mMaterial.GetTexture("_BumpMap") == null)
        {
            GenerateTerrainSmoothNormals();
            MeshUtil.FlattenNormals(meshFilter.sharedMesh, new Vector3(0, -curvatureRadius, 0));
            mMaterial.SetTexture("_BumpMap", _normalTex);
        }
        else
        {
            meshFilter.sharedMesh.RecalculateNormals();
            mMaterial.SetTexture("_BumpMap", null);
        }
    }

    public void ToggleNodeColors()
    {
        var mMaterial = GetMeshMaterial();

        if (mMaterial.GetTexture("_NodeColTex") == null)
        {
            GenerateNodeColors();
            mMaterial.SetTexture("_NodeColTex", _nodeColTex);
        }
        else
        {
            mMaterial.SetTexture("_NodeColTex", null);
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

    Material GetMeshMaterial()
    {
        return Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;
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
}
