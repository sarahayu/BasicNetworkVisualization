using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

class NodeObject
{
    public NodeData data;
    public GameObject gameObject;

    public HSV color;
    public int idInNetworkObject;
    public List<IndexedLink> links = new List<IndexedLink>();
}

class IndexedLink
{
    public LinkData data;
    public int index;
}

// TODO refactor, split responsibilities!
public class NetworkObject : MonoBehaviour
{
    public SharedNetworkData networkData;
    public GameObject nodePrefab;
    // public Camera worldCamera;
    public float minLinkThickness = 0.01f;
    public float maxLinkThickness = 0.15f;
    public float lineThicknessGrowth = 1.2f;
    public Transform rightController;
    public Transform leftController;

    Transform cameraTransform;

    List<NodeObject> nodeObjects = new List<NodeObject>();
    List<GameObject> links = new List<GameObject>();
    Dictionary<int, int> networkIdxToObjectIdx = new Dictionary<int, int>();
    int numControllersPressed = 0;
    Vector3 ogControllerRelPos = Vector3.zero;
    Vector3 ogControllersPos;
    Vector3 ogNetworkScale;
    Quaternion ogNetworkRot;
    Vector3 ogNetworkPos;
    Vector3[] grabPoints = new Vector3[2];
    int numGrabPoints = 0;

    [SerializeField]
    ComputeShader BSplineComputeShader;
    const int BSplineDegree = 3;
    const int BSplineSamplesPerSegment = 10;
    Material batchSplineMaterial;

    ComputeBuffer InSplineData;
    ComputeBuffer InSplineSegmentData;
    ComputeBuffer InSplineControlPointData;
    ComputeBuffer OutSampleControlPointData;

    List<SplineData> Splines;
    List<SplineSegmentData> SplineSegments;
    List<SplineControlPointData> SplineControlPoints;

    [Header("Edge Settings")]
    public Color LinkColor = Color.gray;
    [Range(0.0f, 0.1f)] public float linkWidth = 0.005f;
    [Range(0.001f, 20f)] public float spaceScale = 10f;

    List<BasisSpline> BaseSplines = new List<BasisSpline>();

    public int threadNum = 16;
    [Range(0.0f, 1.0f)] public float edgeBundlingStrength = 0.8f;
    [Range(0.0f, 10.0f)] public float edgeThrottlingDistance = 3f;

    public Color nodeHighlightColor;
    public Color linkHighlightColor;
    public Color linkFocusColor;
    [Range(0.0f, 1.0f)] public float linkMinimumAlpha = 0.01f;
    [Range(0.0f, 1.0f)] public float linkNormalAlphaFactor = 1f;
    [Range(0.0f, 1.0f)] public float linkContextAlphaFactor = 0.5f;
    [Range(0.0f, 1.0f)] public float linkContext2FocusAlphaFactor = 0.8f;
    Dictionary<int, Vector3> groupCMS = null;

    void Awake()
    {
        SetupComputeShaders();
    }

    void Start()
    {
        // have to wait for SharedNetworkData
        SetupGameObjects();
        SetupSplines();
    }

    void Update()
    {
        if (OutSampleControlPointData != null)
        {
            // print("drawing?" + OutSampleControlPointData.count);
            Graphics.DrawProcedural(batchSplineMaterial, new Bounds(Vector3.zero, Vector3.one * 500), MeshTopology.Triangles, OutSampleControlPointData.count * 6);
        }
    }

    void FixedUpdate()
    {
        if (numControllersPressed == 2)
        {
            Vector3 controllerRelPos = rightController.position - leftController.position;
            if (ogControllerRelPos == Vector3.zero)
            {
                ogControllerRelPos = controllerRelPos;
                ogControllersPos = leftController.position + controllerRelPos / 2;
                ogNetworkPos = transform.position;
                ogNetworkScale = transform.localScale;
                ogNetworkRot = transform.rotation;
            }
            else
            {
                var mag = controllerRelPos.magnitude / ogControllerRelPos.magnitude;
                // var newScale = ogNetworkTransform.localScale * mag;
                transform.localScale = ogNetworkScale * mag;
                // var newRot = Quaternion.LookRotation(controllerRelPos);

                // var forward = Vector3.Cross(Vector3.up, controllerRelPos).normalized;
                var newRot = Quaternion.FromToRotation(ogControllerRelPos, controllerRelPos);

                transform.rotation = newRot * ogNetworkRot;
                transform.position = ogNetworkPos + (leftController.position + controllerRelPos / 2 - ogControllersPos) * 20;
            }

            batchSplineMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);

        }
        else
        {
            ogControllerRelPos = Vector3.zero;
        }
    }

    public void ControllerTriggerPress()
    {
        numControllersPressed++;
    }

    public void ControllerTriggerRelease()
    {
        numControllersPressed--;
    }

    public void AddGrabber()
    {

    }

    public void RemoveGrabber()
    {

    }

    public void OnSelectEvent(SelectionEventData eventData)
    {
        List<int> activeLinks = new List<int>();
        
        foreach (var node in nodeObjects)
        {
            bool selected = eventData.nodesSelected.Any(item => item.id == node.idInNetworkObject);
            if (!selected)
                node.gameObject.SetActive(false);
            else
            {
                node.gameObject.SetActive(true);
                foreach (var link in node.links)
                {
                    int otherIdx = link.data.source == node.idInNetworkObject ? link.data.target : link.data.source;
                    bool otherSelected = eventData.nodesSelected.Any(_node => _node.id == otherIdx);

                    if (otherSelected)
                    {
                        activeLinks.Add(link.index);
                    }
                }
            }
        }

        foreach (var inactiveIdx in Enumerable.Range(0, networkData.links.Count()).Except(activeLinks))
        {
            var spline = Splines[inactiveIdx];
            spline.LinkState = 0;
            Splines[inactiveIdx] = spline;
        }

        Redraw(recomputeSplines: false);
    }

    public void OnDeselectEvent(DeselectionEventData eventData)
    {
        for (int i = 0; i < Splines.Count(); i++)
        {
            var spline = Splines[i];
            spline.LinkState = 1;
            Splines[i] = spline;
        }
        
        foreach (var node in nodeObjects)
            node.gameObject.SetActive(true);

        Redraw(recomputeSplines: false);
    }

    // movedIndex is index in network object
    public void Rearrange(int movedIndex)
    {
        List<int> nodesWithMovedAsTarget = new List<int>();

        var nodeObject = nodeObjects[networkIdxToObjectIdx[movedIndex]];

        UpdateNodePosInSharedNetwork(nodeObject.data, nodeObject.gameObject.transform);

        Redraw(recomputeSplines: true);
    }

    void UpdateNodePosInSharedNetwork(NodeData node, Transform transform)
    {        
        networkData.nodes[node.id].pos3D[0] = transform.localPosition.x;
        networkData.nodes[node.id].pos3D[1] = transform.localPosition.y;
        networkData.nodes[node.id].pos3D[2] = transform.localPosition.z;
    }

    GameObject CreateSphere(NodeData node)
    {
        var interactableNodeP = (GameObject)Instantiate(
            original: nodePrefab, 
            parent: transform,
            instantiateInWorldSpace: false);
        interactableNodeP.transform.localPosition = new Vector3(node.pos3D[0], node.pos3D[1], node.pos3D[2]) * 3;
        interactableNodeP.GetComponent<NodeDisplay>().Init(this, node.color, node.name, node.id);
        return interactableNodeP;
    }

    void SetupGameObjects()
    {
        int objectIdx = 0;
        foreach (var node in networkData.nodes)
        {
            if (node.isVirtual)
                continue;

            var nodeGameObject = CreateSphere(node);
            
            nodeObjects.Add(new NodeObject()
            {
                data = node,
                gameObject = nodeGameObject,
                color = node.color,
                idInNetworkObject = node.id
            });

            networkIdxToObjectIdx.Add(node.id, objectIdx++);
            UpdateNodePosInSharedNetwork(node, nodeGameObject.transform);
        }

        int[] color1Ind = { 0, 1, 6, 7, 16, 19, 20, 23 };
        int[] color2Ind = { 2, 3, 10, 11, 17, 18, 21, 22 };

        int max = -1;
        foreach (var link in networkData.links)
            if (link.value > max)
                max = link.value;
        
        int linkIndex = 0;
        foreach (var link in networkData.links)
        {
            nodeObjects[networkIdxToObjectIdx[link.source]].links.Add(new IndexedLink() {
                data = link,
                index = linkIndex
            });
            nodeObjects[networkIdxToObjectIdx[link.target]].links.Add(new IndexedLink() {
                data = link,
                index = linkIndex
            });

            linkIndex++;
        }
    }

    void SetupComputeShaders()
    {
        // Initialize the material, the shader will draw the splines/links
        batchSplineMaterial = new Material(Shader.Find("Custom/Batch Spline"));
        batchSplineMaterial.SetFloat("_LineWidth", linkWidth * spaceScale);

        // Configure the spline compute shader
        BSplineComputeShader.SetVector("COLOR_HIGHLIGHT", linkHighlightColor);
        BSplineComputeShader.SetVector("COLOR_FOCUS", linkFocusColor);
        BSplineComputeShader.SetFloat("COLOR_MINIMUM_ALPHA", linkMinimumAlpha);
        BSplineComputeShader.SetFloat("COLOR_NORMAL_ALPHA_FACTOR", linkNormalAlphaFactor);
        BSplineComputeShader.SetFloat("COLOR_CONTEXT_ALPHA_FACTOR", linkContextAlphaFactor);
        BSplineComputeShader.SetFloat("COLOR_FOCUS2CONTEXT_ALPHA_FACTOR", linkContext2FocusAlphaFactor);
    }

    void SetupSplines()
    {        
        foreach (var link in networkData.links)
        {
            BaseSplines.Add(new BasisSpline());
        }

        ComputeCenterMasses();
        ComputeControlPoints();
        InitializeComputeBuffers();
        
        // Run the ComputeShader. 1 Thread per segment.
        int kernel = BSplineComputeShader.FindKernel("CSMain");
        BSplineComputeShader.Dispatch(kernel, Mathf.CeilToInt(SplineSegments.Count / 32), 1, 1);
    }

    private void InitializeComputeBuffers()
    {
        // Initialize Compute Shader data
        Splines = new List<SplineData>();
        SplineSegments = new List<SplineSegmentData>();
        SplineControlPoints = new List<SplineControlPointData>();

        uint splineSegmentCount = 0;
        uint splineControlPointCount = 0;
        uint splineSampleCount = 0;

        uint splineIdx = 0;
        foreach (var link in networkData.links)
        {
            int sourceIdx = networkIdxToObjectIdx[link.source], 
                targetIdx = networkIdxToObjectIdx[link.target];

            var source = nodeObjects[sourceIdx];
            var target = nodeObjects[targetIdx];

            var straightenPoints = BaseSplines[link.id].StraightenPoints;
            int NumSegments = straightenPoints.Count() + BSplineDegree - 2; //NumControlPoints + Degree - 2 (First/Last Point)
            Color sourceColor = source.color.ToRGB();
            Color targetColor = target.color.ToRGB();

            Vector3 startPosition = source.gameObject.transform.localPosition;
            Vector3 endPosition = target.gameObject.transform.localPosition;
            // uint linkState = (uint)link.linkDraw.GetState();
            SplineData spline = new SplineData(splineIdx++, (uint)NumSegments, splineSegmentCount, (uint)NumSegments * BSplineSamplesPerSegment, splineSampleCount, startPosition, endPosition, sourceColor, targetColor);//, linkState);
            Splines.Add(spline);

            // Add all segments of this spline
            for (int i = 0; i < NumSegments; i++)
            {
                SplineSegments.Add(new SplineSegmentData(
                    spline.Idx,
                    splineControlPointCount + (uint)i,
                    BSplineSamplesPerSegment,
                    splineSegmentCount * BSplineSamplesPerSegment
                    ));

                splineSampleCount += BSplineSamplesPerSegment;
                splineSegmentCount += 1;
            }

            // Add all control points of this spline
            // We have to add *degree* times the first and last control points to make the spline coincide with its endpoints
            // Remember to add cp0 degree-1 times, because the loop that adds all the points will add the last remaining cp0
            // See: https://web.mit.edu/hyperbook/Patrikalakis-Maekawa-Cho/node17.html

            for (int i = 0; i < BSplineDegree - 1; i++)
            {
                SplineControlPoints.Add(new SplineControlPointData(straightenPoints[0]));
                splineControlPointCount += 1;
            }
            for (int i = 0; i < straightenPoints.Count(); i++)
            {
                SplineControlPoints.Add(new SplineControlPointData(
                        straightenPoints[i]
                    ));
                splineControlPointCount += 1;
            }
            for (int i = 0; i < BSplineDegree - 1; i++)
            {
                SplineControlPoints.Add(new SplineControlPointData(straightenPoints[straightenPoints.Count() - 1]));
                splineControlPointCount += 1;
            }
            
            SplineControlPoints.Add(new SplineControlPointData(startPosition));
            splineControlPointCount += 1;
            SplineControlPoints.Add(new SplineControlPointData(endPosition));
            splineControlPointCount += 1;
        }

        // Finally, set up buffers and bind them to the shader
        int kernel = BSplineComputeShader.FindKernel("CSMain");
        InSplineData = new ComputeBuffer(Splines.Count, SplineData.size());
        InSplineControlPointData = new ComputeBuffer(SplineControlPoints.Count, SplineControlPointData.size());
        InSplineSegmentData = new ComputeBuffer(SplineSegments.Count, SplineSegmentData.size());
        OutSampleControlPointData = new ComputeBuffer((int)splineSampleCount, SplineSamplePointData.size());

        InSplineData.SetData(Splines);
        InSplineControlPointData.SetData(SplineControlPoints);
        InSplineSegmentData.SetData(SplineSegments);

        BSplineComputeShader.SetBuffer(kernel, "InSplineData", InSplineData);
        BSplineComputeShader.SetBuffer(kernel, "InSplineControlPointData", InSplineControlPointData);
        BSplineComputeShader.SetBuffer(kernel, "InSplineSegmentData", InSplineSegmentData);
        BSplineComputeShader.SetBuffer(kernel, "OutSamplePointData", OutSampleControlPointData);
        BSplineComputeShader.SetInt("NumPoints", SplineSegments.Count);


        // Bind the buffers to the LineRenderer Material
        batchSplineMaterial.SetBuffer("OutSamplePointData", OutSampleControlPointData);
        batchSplineMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
    }

    // if recompute false, we may just be updating visibility, not position data. So don't recompute control points... more performant?
    void Redraw(bool recomputeSplines = true)
    {
        if (recomputeSplines)
        {
            ComputeControlPoints();
            UpdateSplines();
        }
        UpdateComputeBuffers();

        // Run the ComputeShader. 1 Thread per segment.
        int kernel = BSplineComputeShader.FindKernel("CSMain");
        BSplineComputeShader.Dispatch(kernel, Mathf.CeilToInt(SplineSegments.Count / 32), 1, 1);
    }

    void ComputeCenterMasses()
    {
        if (groupCMS == null)
            groupCMS = networkData.nodes
                .Where(node => !node.isVirtual)
                .Select(node => node.group)
                .Distinct()
                .ToDictionary(keySelector: groupNodeIdx => groupNodeIdx, elementSelector: _ => new Vector3());

        // print(networkData.nodes.Count());
        foreach (var groupIdx in groupCMS.Keys.ToList())
        {
            // print(groupIdx);
            var avgPos = networkData.nodes[groupIdx].children.Aggregate(
                new Vector3(), 
                (sum, childIdx) => sum + new Vector3(
                        networkData.nodes[childIdx].pos3D[0],
                        networkData.nodes[childIdx].pos3D[1],
                        networkData.nodes[childIdx].pos3D[2]
                    ));

            avgPos /= networkData.nodes[groupIdx].children.Count();

            groupCMS[groupIdx] = avgPos;
        }
    }

    void ComputeControlPoints()
    {
        // idx SHOULD line up with networkData.links
        int idx = 0;
        foreach (var spline in BaseSplines)
        {
            straighthenParam param = new straighthenParam()
            {
                l = networkData.links[idx++],
                networkData = networkData,
                beta = edgeBundlingStrength,
                cms = groupCMS,
                throttleDist = edgeThrottlingDistance
            };

            spline.ComputeSplineController(param);
        }
    }

    void UpdateSplines()
    {
        SplineControlPoints = new List<SplineControlPointData>();

        uint splineSegmentCount = 0;
        uint splineControlPointCount = 0;
        uint splineSampleCount = 0;

        int splineIdx = 0;

        foreach (var link in networkData.links)
        {
            int sourceIdx = networkIdxToObjectIdx[link.source], 
                targetIdx = networkIdxToObjectIdx[link.target];

            var source = nodeObjects[sourceIdx];
            var target = nodeObjects[targetIdx];

            var straightenPoints = BaseSplines[link.id].StraightenPoints;
            var ControlPointCount = straightenPoints.Count();
            int NumSegments = ControlPointCount + BSplineDegree - 2;
            
            Vector3 startPosition = source.gameObject.transform.localPosition;
            Vector3 endPosition = target.gameObject.transform.localPosition;

            // Update spline positions

            SplineData spline = Splines[splineIdx];
            spline.StartPosition = startPosition;
            spline.EndPosition = endPosition;
            Splines[splineIdx++] = spline;

            // Update segments

            for (int i = 0; i < NumSegments; i++)
            {
                SplineSegmentData splineSegment = SplineSegments[(int)splineSegmentCount];
                splineSegment.SplineIdx = spline.Idx;
                splineSegment.BeginControlPointIdx = splineControlPointCount + (uint)i;
                splineSegment.NumSamples = BSplineSamplesPerSegment;
                splineSegment.BeginSamplePointIdx = splineSegmentCount * BSplineSamplesPerSegment;
                SplineSegments[(int)splineSegmentCount] = splineSegment;

                splineSampleCount += BSplineSamplesPerSegment;
                splineSegmentCount += 1;
            }
            
            // Update control points

            SplineControlPointData[] controlPoints = new SplineControlPointData[2 * (BSplineDegree - 1) + ControlPointCount];
            for (int i = 0; i < BSplineDegree; i++)
            {
                controlPoints[i].Position = straightenPoints[0];
            }
            for (int i = 1; i < ControlPointCount - 1; i++)
            {
                controlPoints[BSplineDegree - 1 + i].Position = straightenPoints[i];
            }
            for (int i = BSplineDegree + (ControlPointCount - 1) - 1; i < controlPoints.Length; i++)
            {
                controlPoints[i].Position = straightenPoints[ControlPointCount - 1];
            }
            
            SplineControlPoints.AddRange(controlPoints); // AddRange is faster than adding items in a loop
            splineControlPointCount += (uint)controlPoints.Length;
        }
    }

    void UpdateComputeBuffers()
    {
        InSplineData.SetData(Splines);
        InSplineControlPointData.SetData(SplineControlPoints);
        InSplineSegmentData.SetData(SplineSegments);
    }
}
