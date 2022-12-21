using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

class NodeObject
{
    public NodeData data;
    public GameObject nodeObject;

    public HSV color;
    public int idInNetworkObject;
    public List<IndexedLink> links = new List<IndexedLink>();
}

class IndexedLink
{
    public LinkData data;
    public int index;
}

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
    const int BSplineDegree = 1;
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

    // link index -> BasisSpline Obj
    // Dictionary<LinkData, BasisSpline> indexedBSpline = new Dictionary<LinkData, BasisSpline>();

    public int threadNum = 16;
    [Range(0.0f, 1.0f)] public float edgeBundlingStrength = 0.8f;

    public Color nodeHighlightColor;
    public Color linkHighlightColor;
    public Color linkFocusColor;
    [Range(0.0f, 0.1f)] public float linkMinimumAlpha = 0.01f;
    [Range(0.0f, 1.0f)] public float linkNormalAlphaFactor = 1f;
    [Range(0.0f, 1.0f)] public float linkContextAlphaFactor = 0.5f;
    [Range(0.0f, 1.0f)] public float linkContext2FocusAlphaFactor = 0.8f;
    Dictionary<LinkData, GameObject> _linkGroup = new Dictionary<LinkData, GameObject>();

    void Awake()
    {
        SetupComputeShaders();
    }

    void Start()
    {
        // have to wait for SharedNetworkData
        SetupGameObjects();
        LayoutUpdate();
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
                transform.position = ogNetworkPos + (leftController.position + controllerRelPos / 2 - ogControllersPos) * 20;
                // var newRot = Quaternion.LookRotation(controllerRelPos);

                var forward = Vector3.Cross(Vector3.up, controllerRelPos).normalized;
                var newRot = Quaternion.FromToRotation(ogControllerRelPos, controllerRelPos);

                transform.rotation = newRot * ogNetworkRot;
            }

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
        foreach (var link in links)
            link.SetActive(false);
        int objectIdx = 0;
        foreach (var node in nodeObjects)
        {
            bool selected = eventData.groupsSelected.Any(item => item.id == nodeObjects[objectIdx].idInNetworkObject);
            if (!selected)
                node.nodeObject.SetActive(false);
            else
            {
                node.nodeObject.SetActive(true);
                foreach (var link in node.links)
                {
                    bool srcSelected = eventData.groupsSelected.Any(item => item.id == link.data.source);
                    bool targetSelected = eventData.groupsSelected.Any(item => item.id == link.data.target);

                    if (srcSelected && targetSelected)
                        links[link.index].SetActive(true);
                }
            }
            objectIdx++;
        }
    }

    public void OnDeselectEvent(DeselectionEventData eventData)
    {
        foreach (var link in links)
            link.SetActive(true);
        foreach (var node in nodeObjects)
            node.nodeObject.SetActive(true);
    }

    // movedIndex is index in network object
    public void RearrangeLinks(int movedIndex)
    {
        List<int> nodesWithMovedAsTarget = new List<int>();

        int movedObjectIdx = networkIdxToObjectIdx[movedIndex];

        foreach (var link in nodeObjects[movedObjectIdx].links)
        {
            var srcInd = networkIdxToObjectIdx[link.data.source];
            var tarInd = networkIdxToObjectIdx[link.data.target];

            var sourceNode = nodeObjects[srcInd].nodeObject;
            var targetNode = nodeObjects[tarInd].nodeObject;

            var linkObject = links[link.index];

            var toTarget = targetNode.transform.localPosition - sourceNode.transform.localPosition;
            var dist = toTarget.magnitude;
            var newScale = linkObject.transform.localScale;
            newScale.y = dist;

            linkObject.transform.localScale = newScale;
            linkObject.transform.localPosition = sourceNode.transform.localPosition + toTarget / 2;

            linkObject.transform.LookAt(targetNode.transform.position);
            linkObject.transform.RotateAround(linkObject.transform.position, linkObject.transform.right, 90);

            // var dec = Mathf.Atan2(Mathf.Sqrt(toTarget.x * toTarget.x + toTarget.z * toTarget.z), toTarget.y);
            // var ra = Mathf.Atan2(toTarget.z, toTarget.x);
            // linkObject.transform.rotation = new Quaternion();
            // linkObject.transform.RotateAround(sourceNode.transform.localPosition, Vector3.right, dec * Mathf.Rad2Deg);
            // linkObject.transform.RotateAround(sourceNode.transform.localPosition, Vector3.up, Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg);

        }
    }

    void SetupGameObjects()
    {
        // cameraTransform = worldCamera.GetComponent<Transform>();

        int objectIdx = 0;
        foreach (var node in networkData.nodes)
        {
            if (node.isVirtual)
                continue;
            var interactableNodeP = Instantiate(nodePrefab, new Vector3(node.pos3D[0], node.pos3D[1], node.pos3D[2]) * 3, Quaternion.identity);
            interactableNodeP.GetComponent<NodeDisplay>().Init(this, node.color, node.name, node.id);
            // interactableNodeP.GetComponent<Rigidbody>().detectCollisions = false;
            var nodeTransform = interactableNodeP.transform;
            nodeTransform.SetParent(GetComponent<Transform>());
            var objectPrefab = nodeTransform.Find("MeshContainer").Find("NodeObject");
            // var sphere = objectPrefab.Find("Sphere");
            // var label = objectPrefab.Find("Label");
            // TMP.faceColor = new Color(TMP.faceColor.r, TMP.faceColor.g, TMP.faceColor.b, textFadeOut);

            nodeObjects.Add(new NodeObject()
            {
                data = node,
                nodeObject = interactableNodeP,
                color = node.color,
                idInNetworkObject = node.id
            });

            networkIdxToObjectIdx.Add(node.id, objectIdx);

            objectIdx++;
        }

        int[] color1Ind = { 0, 1, 6, 7, 16, 19, 20, 23 };
        int[] color2Ind = { 2, 3, 10, 11, 17, 18, 21, 22 };

        int max = -1;
        foreach (var link in networkData.links)
            if (link.value > max)
                max = link.value;

        // return;
        int linkIndex = 0;
        // foreach (var link in networkData.links)
        // {
        //     var rectPrism = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //     rectPrism.transform.SetParent(transform);

        //     int sourceIdx = networkIdxToObjectIdx[link.source], 
        //         targetIdx = networkIdxToObjectIdx[link.target];

        //     var source = nodeObjects[sourceIdx].nodeObject;
        //     var target = nodeObjects[targetIdx].nodeObject;
        //     var toTarget = target.transform.localPosition - source.transform.localPosition;
        //     var dist = toTarget.magnitude;

        //     var thickness = Mathf.Pow(Mathf.Lerp(minLinkThickness, maxLinkThickness, (float)link.value / max), 1.0f / lineThicknessGrowth);

        //     rectPrism.transform.localScale = new Vector3(thickness, dist, thickness);
        //     rectPrism.transform.localPosition = source.transform.localPosition + toTarget / 2;

        //     // var toTargetWorld = target.transform.position - source.transform.position;
        //     // var dec = Mathf.Atan2(Mathf.Sqrt(toTarget.x * toTarget.x + toTarget.z * toTarget.z), toTarget.y);
        //     // var ra = Mathf.Atan2(toTarget.x, toTarget.z);
        //     // rectPrism.transform.RotateAround(source.transform.position, transform.rotation * Vector3.right, dec * Mathf.Rad2Deg);
        //     // rectPrism.transform.RotateAround(source.transform.position, transform.rotation * Vector3.up, ra * Mathf.Rad2Deg);
        //     rectPrism.transform.LookAt(target.transform.position);
        //     rectPrism.transform.RotateAround(rectPrism.transform.position, rectPrism.transform.right, 90);

        //     rectPrism.GetComponent<MeshRenderer>().material.shader = Shader.Find("Particles/Standard Surface");


        //     var mesh = rectPrism.GetComponent<MeshFilter>().mesh;
        //     var vertices = mesh.vertices;
        //     var colors = new Color[vertices.Length];

        //     Color colorSource = nodeObjects[sourceIdx].color.CloneAndDesat(0.5f).ToRGB(),
        //         colorTarget = nodeObjects[targetIdx].color.CloneAndDesat(0.5f).ToRGB();
        //     foreach (var ind in color1Ind)
        //         colors[ind] = colorSource;
        //     foreach (var ind in color2Ind)
        //         colors[ind] = colorTarget;

        //     mesh.colors = colors;

        //     links.Add(rectPrism);
        //     nodeObjects[sourceIdx].links.Add(new IndexedLink()
        //     {
        //         data = link,
        //         index = linkIndex
        //     });
        //     nodeObjects[targetIdx].links.Add(new IndexedLink()
        //     {
        //         data = link,
        //         index = linkIndex
        //     });

        //     linkIndex++;
        //     // spheres.Add(rectPrism);
        // }
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

    public void LayoutUpdate()
    {
        print("Layout Update!");
        // layoutReady = false;
        // network = layout.networkObj;

        // foreach (var node in layout.networkObj.nodes)
        // {
        //     if (node.virtualNode && drawVirtualNode)
        //     {
        //         DrawVirtualNode(node.idx, node.Position3D);
        //     }
        //     else if (!node.virtualNode)
        //     {
        //         DrawNode(node.idx, node.Position3D, layout.networkObj);
        //     }
        // }

        // This will draw a 'debug' tree structure to emphasize the underlying hierarchy...
        // if (drawTreeStructure)
        // {
        //     foreach (var link in layout.treeLinks)
        //     {
        //         var start = layout.networkObj[link.sourceIdx].Position3D;
        //         var end = layout.networkObj[link.targetIdx].Position3D;
        //         var linkObj = DrawStraightLine3D(start * spaceScale, end * spaceScale, LinkColor);
        //         _linkGroup.Add(link, linkObj);
        //     }
        // }
        // else // ...whereas this is concerned with the visible links between nodes in the graph
        // {
        foreach (var link in networkData.links)
        {

            var source = nodeObjects[link.source].nodeObject;
            var target = nodeObjects[link.target].nodeObject;
            var start = source.transform.localPosition;
            var end = target.transform.localPosition;

            // Draws the graph using edge bundling and splines...
            if (true)//(isEdgeBundling)
            {
                // GameObject linkObj;
                // linkObj = DrawBSplineCurve(link);
                _linkGroup.Add(link, null);
                // linkIdxGroup.Add(link.linkIdx, linkObj);

                // indexedBSpline.Add(link, new BasisSpline());
            }
            else // ...or draws it using straight lines
            {
                // var linkObj = DrawStraightLine3D(start * spaceScale, end * spaceScale, LinkColor);
                // _linkGroup.Add(link, linkObj);
                // linkIdxGroup.Add(link.linkIdx, linkObj);
            }
        }

        ComputeControlPoints();
        InitializeComputeBuffers();
        Redraw();
        // }

        // layoutReady = true;
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
            // Draws the graph using edge bundling and splines...
            if (true)//(isEdgeBundling)
            {
                /*
                 * Add Compute Shader data
                 */

                int sourceIdx = networkIdxToObjectIdx[link.source], 
                    targetIdx = networkIdxToObjectIdx[link.target];

                var source = nodeObjects[sourceIdx];
                var target = nodeObjects[targetIdx];

                int NumSegments = 1;//link.straightenPoints.Count + BSplineDegree - 2; //NumControlPoints + Degree - 2 (First/Last Point)
                Color sourceColor = source.color.ToRGB();
                Color targetColor = target.color.ToRGB();

                Vector3 startPosition = source.nodeObject.transform.position;
                Vector3 endPosition = target.nodeObject.transform.position;
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
                // for (int i = 0; i < BSplineDegree - 1; i++)
                // {
                //     SplineControlPoints.Add(new SplineControlPointData(link.straightenPoints[0]));
                //     splineControlPointCount += 1;
                // }
                // for (int i = 0; i < link.straightenPoints.Count; i++)
                // {
                //     SplineControlPoints.Add(new SplineControlPointData(
                //             link.straightenPoints[i]
                //         ));
                //     splineControlPointCount += 1;
                // }
                // for (int i = 0; i < BSplineDegree - 1; i++)
                // {
                //     SplineControlPoints.Add(new SplineControlPointData(link.straightenPoints[link.straightenPoints.Count - 1]));
                //     splineControlPointCount += 1;
                // }
                
                SplineControlPoints.Add(new SplineControlPointData(startPosition));
                splineControlPointCount += 1;
                SplineControlPoints.Add(new SplineControlPointData(endPosition));
                splineControlPointCount += 1;
            }
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


        // Bind the buffers to the LineRenderer Material
        batchSplineMaterial.SetBuffer("InSplineData", InSplineData);
        batchSplineMaterial.SetBuffer("OutSamplePointData", OutSampleControlPointData);
    }
    void Redraw()
    {
        ComputeControlPoints();
        UpdateComputeBuffers();

        // Run the ComputeShader. 1 Thread per segment.
        int kernel = BSplineComputeShader.FindKernel("CSMain");
        BSplineComputeShader.Dispatch(kernel, SplineSegments.Count / 32, 1, 1);
    }

    void ComputeControlPoints()
    {
        // foreach (KeyValuePair<Link, BasisSpline> entry in indexedBSpline)
        // {
        //     straighthenParam param = new straighthenParam();
        //     param.l = entry.Key;
        //     param.network = network;
        //     param.spaceScale = spaceScale;
        //     param.beta = edgeBundlingStrength;
        //     entry.Value.ComputeSplineController(param);
        // }
    }

    void UpdateComputeBuffers()
    {
        // // Initialize Compute Shader data
        // SplineControlPoints = new List<SplineControlPointData>();

        // uint splineSegmentCount = 0;
        // uint splineControlPointCount = 0;
        // uint splineSampleCount = 0;

        // int splineIdx = 0;

        // foreach (var entry in _linkGroup)
        // {
        //     // BasisSpline basisSpline = indexedBSpline[entry.Key];
        //     int ControlPointCount = 2;//basisSpline.ScaledStraighthenPoints.Length;
        //     int NumSegments = ControlPointCount + BSplineDegree - 2; //NumControlPoints + Degree - 2 (First/Last Point)


        //     /*
        //     * Add Compute Shader data
        //     */
        //     Vector3 startPosition = basisSpline.ScaledStraighthenPoints[0];
        //     Vector3 endPosition = basisSpline.ScaledStraighthenPoints[ControlPointCount - 1];
        //     uint linkState = 4;// (uint)entry.Key.linkDraw._linkState;

        //     // Update spline information, we can preserve colors since their lookup is expensive
        //     SplineData spline = Splines[splineIdx];
        //     int OldNumSegments = (int)spline.NumSegments;
        //     spline.StartPosition = startPosition;
        //     spline.EndPosition = endPosition;
        //     spline.LinkState = linkState;
        //     spline.NumSegments = (uint)NumSegments;
        //     spline.BeginSplineSegmentIdx = splineSegmentCount;
        //     spline.NumSamples = (uint)NumSegments * BSplineSamplesPerSegment;
        //     spline.BeginSamplePointIdx = splineSampleCount;
        //     Splines[splineIdx++] = spline;

        //     // To improve performance, we differentiate between cases where there's the same number of segments and where the number differs
        //     // For same number of segments, we only update the data without creating now instances
        //     // For differing number of segments, we delete the old range of segment data and insert the new one in place
        //     if (NumSegments != OldNumSegments)
        //     {
        //         // Remove old segment data
        //         SplineSegments.RemoveRange((int)splineSegmentCount, OldNumSegments);

        //         // Add new segment data
        //         for (int i = 0; i < NumSegments; i++)
        //         {
        //             SplineSegments.Insert((int)splineSegmentCount, new SplineSegmentData(
        //                 spline.Idx,
        //                 splineControlPointCount + (uint)i,
        //                 BSplineSamplesPerSegment,
        //                 splineSegmentCount * BSplineSamplesPerSegment
        //                 ));

        //             splineSampleCount += BSplineSamplesPerSegment;
        //             splineSegmentCount += 1;
        //         }
        //     }
        //     else
        //     {
        //         // Update segment
        //         for (int i = 0; i < NumSegments; i++)
        //         {
        //             SplineSegmentData splineSegment = SplineSegments[(int)splineSegmentCount];
        //             splineSegment.SplineIdx = spline.Idx;
        //             splineSegment.BeginControlPointIdx = splineControlPointCount + (uint)i;
        //             splineSegment.NumSamples = BSplineSamplesPerSegment;
        //             splineSegment.BeginSamplePointIdx = splineSegmentCount * BSplineSamplesPerSegment;
        //             SplineSegments[(int)splineSegmentCount] = splineSegment;

        //             splineSampleCount += BSplineSamplesPerSegment;
        //             splineSegmentCount += 1;
        //         }
        //     }



        //     // Add all control points of this spline
        //     // We have to add *degree* times the first and last control points to make the spline coincide with its endpoints
        //     // Remember to add cp0 degree-1 times, because the loop that adds all the points will add the last remaining cp0
        //     // See: https://web.mit.edu/hyperbook/Patrikalakis-Maekawa-Cho/node17.html
        //     SplineControlPointData[] controlPoints = new SplineControlPointData[2 * (BSplineDegree - 1) + ControlPointCount];
        //     for (int i = 0; i < BSplineDegree; i++)
        //     {
        //         controlPoints[i].Position = basisSpline.ScaledStraighthenPoints[0];
        //     }
        //     for (int i = 1; i < ControlPointCount - 1; i++)
        //     {
        //         controlPoints[BSplineDegree - 1 + i].Position = basisSpline.ScaledStraighthenPoints[i];
        //     }
        //     for (int i = BSplineDegree + (ControlPointCount - 1) - 1; i < controlPoints.Length; i++)
        //     {
        //         controlPoints[i].Position = basisSpline.ScaledStraighthenPoints[ControlPointCount - 1];
        //     }
        //     SplineControlPoints.AddRange(controlPoints); // AddRange is faster than adding items in a loop
        //     splineControlPointCount += (uint)controlPoints.Length;
        // }


        // // Finally, set up buffers
        // InSplineData.SetData(Splines);
        // InSplineControlPointData.SetData(SplineControlPoints);
        // InSplineSegmentData.SetData(SplineSegments);
    }

    // private GameObject DrawStraightLine3D(Vector3 start, Vector3 end, Color color)
    // {
    //     var link = Instantiate(linkPrefab);
    //     link.transform.localPosition = (start + end) / 2.0f;
    //     link.transform.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
    //     link.transform.localScale = new Vector3(linkWidth, Vector3.Distance(start, end) * 0.5f, linkWidth);
    //     return link;
    // }

    // private GameObject DrawBSplineCurve(Link link)
    // {
    //     var newObj = Instantiate(bSplinePrefab, transform);
    //     //var basic = newObj.GetComponent<BasisSpline>();
    //     //basic.DrawSpline(link, network, spaceScale, nodeGroup);
    //     return newObj;
    // }
}
