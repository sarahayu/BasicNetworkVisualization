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


    [SerializeField]
    SelectionEvent _onSelected;

    [SerializeField]
    DeselectionEvent _onDeselected;


    [SerializeField]
    SharedNetworkData _networkData;


    public bool autoUpdate = false;


    [SerializeField]
    MeshFilter _meshFilter;

    [SerializeField]
    MeshRenderer _meshRenderer;

    [SerializeField]
    MeshCollider _meshCollider;

    [SerializeField]
    Transform _laserBox;


    [SerializeField]
    float _scaleHeight = 50f;

    [SerializeField]
    float _falloff = 1f;

    [SerializeField]
    int _subdivide = 1;

    [SerializeField]
    float _lineColorIntensity = 0.2f;

    [SerializeField]
    AnimationCurve _falloffShapeFunc = AnimationCurve.Linear(0, 0, 1, 1);

    [SerializeField]
    AnimationCurve _peakHeightFunc = AnimationCurve.Linear(0, 0, 1, 1);

    [SerializeField]
    AnimationCurve _slackFunc = AnimationCurve.Linear(0, 0.5f, 1, 1);

    bool _slackIsLevel;         // obsolete field, may remove later

    [SerializeField]
    float _curvatureRadius = 100f;

    [SerializeField]
    int _maxNumLinks = 300;

    [SerializeField]
    Transform _parent;


    [SerializeField]
    TextMeshPro _debugDisplay;

    FootballFileData _JSONNetworkData;
    TerrainGraph _graph;
    TerrainMeshGenerator _meshGenerator;
    TerrainOutline _terrainOutline;
    HeightMap _heightMap = null;
    Texture2D _lineTex = null;
    Texture2D _heightTex = null;
    Texture2D _normalTex = null;
    Texture2D _nodeColTex = null;
    Texture2D _selectionTex = null;
    bool _inputTracerStarted = false;

    public void Awake()
    {
        _selectionTex = new Texture2D(360, 360);
        _terrainOutline = new TerrainOutline(_selectionTex);

        transform.parent = _parent;

        Reset();
        GenerateTerrainLowQuality();
        ToggleAlbedoLines();
        ToggleHeightMap();
        ToggleNormalMap();
        ToggleNodeColors();

        GetMeshMaterial().SetTexture("_SelectionTex", _selectionTex);
    }


    public void Start()
    {
    }

    public void HandleInputTracerPress()
    {
        _inputTracerStarted = true;
    }

    public void HandleInputTracerRelease()
    {
        _inputTracerStarted = false;
        if (_terrainOutline.ConnectAndUpdate())
        {
            var inOutline = _graph.GetNodeIdxsContainedInOutline(_terrainOutline.OutlineTexture);
            var selectedNodes = _networkData.nodes.Where(n => inOutline.Contains(n.id)).ToList();
            _onSelected.Invoke(new SelectionEventData()
            {
                nodesSelected = selectedNodes
            });
        }
    }

    public void HandleInputClearCanvasPress()
    {
        _terrainOutline.ClearPoints();
        _onDeselected.Invoke(new DeselectionEventData());
    }

    public void FixedUpdate()
    {
        CheckLaserIntersectsTerrain();
    }

    public void Reset()
    {
        ColorUtil.ResetRandomHSV();

        _networkData.ParseFromString();

        // get network data information from JSON
        _JSONNetworkData = JsonUtility.FromJson<FootballFileData>(_networkData.dataFile.text);

        // get basic graph data from JSON spread over 2D width/height bounds
        _graph = new TerrainGraph(
            width: (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2),
            height: (int)Mathf.Sqrt(Mathf.Pow(GRAPH_AREA_LEN, 2) * 2),
            maxLinks: _maxNumLinks
        );
        _graph.CreateFromJSONData(_JSONNetworkData);

        // make heightmap from graph
        _heightMap = new HeightMap(
            graph: _graph,
            falloffDistance: _falloff * GRAPH_AREA_LEN,
            falloffShapeFunc: _falloffShapeFunc,
            peakHeightFunc: _peakHeightFunc,
            slackFunc: _slackFunc,
            slackIsLevel: _slackIsLevel
        );

        // make mesh from heightmap
        _meshGenerator = new TerrainMeshGenerator(
            graph: _graph,
            heightMap: _heightMap,
            meshHeight: _scaleHeight,
            meshWidth: (int)_graph.width,
            meshLength: (int)_graph.height,
            subdivide: _subdivide,
            radius: _curvatureRadius,
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
        mMaterial.SetFloat("_MaxHeight", _scaleHeight);
        mMaterial.SetFloat("_CurvatureRadius", _curvatureRadius);
        mMaterial.SetInt("_UseHeightMap", 0);
    }

    public void GenerateTerrainLowQuality()
    {
        _meshFilter.sharedMesh = _meshGenerator.GenerateFromGraph();

        var colFlat = new Color[TEX_RES_NORMAL * TEX_RES_NORMAL];
        for (int y = 0; y < TEX_RES_NORMAL; y++)
            for (int x = 0; x < TEX_RES_NORMAL; x++)
                colFlat[y * TEX_RES_NORMAL + x] = Color.black;

        var texFlat = new Texture2D(TEX_RES_NORMAL, TEX_RES_NORMAL);
        texFlat.SetPixels(colFlat);
        texFlat.Apply();

        var mMaterial = GetMeshMaterial();

        mMaterial.SetTexture("_LineTex", texFlat);
        mMaterial.SetFloat("_MaxHeight", _scaleHeight);
        mMaterial.SetFloat("_CurvatureRadius", _curvatureRadius);
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
            MeshUtil.FlattenNormals(_meshFilter.sharedMesh, new Vector3(0, -_curvatureRadius, 0));
            mMaterial.SetTexture("_BumpMap", _normalTex);
        }
        else
        {
            _meshFilter.sharedMesh.RecalculateNormals();
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
        _lineTex = _heightMap.GenerateTextureLines(TEX_RES_ALBEDO, TEX_RES_ALBEDO, _lineColorIntensity);
    }

    void CheckLaserIntersectsTerrain()
    {
        if (_inputTracerStarted)
        {
            var sourcePos = _laserBox.position - _laserBox.forward * _laserBox.localScale.z / 2;
            Ray ray = new Ray(sourcePos, _laserBox.forward);
            RaycastHit hit;

            if (_meshCollider.Raycast(ray, out hit, Mathf.Infinity))
            {
                var worldCollPos = sourcePos + _laserBox.forward * hit.distance;
                var localPos = transform.InverseTransformPoint(worldCollPos);
                var texPos = _meshGenerator.LocalToTexPos(localPos);

                _terrainOutline.AddPointAndUpdate(texPos);
            }
        }
    }

    Material GetMeshMaterial()
    {
        return Application.isPlaying ? _meshRenderer.material : _meshRenderer.sharedMaterial;
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
            _normalTex = TextureUtil.GenerateNormalFromHeight(_heightTex, _scaleHeight, GRAPH_AREA_LEN);

    }

    void GenerateNodeColors()
    {
        if (_heightMap == null)
            GenerateTerrainLowQuality();

        if (_nodeColTex == null)
            _nodeColTex = TerrainTextureUtil.GenerateNodeColsFromGraph(_graph, _heightMap, TEX_RES_ALBEDO, TEX_RES_ALBEDO);

    }
}
