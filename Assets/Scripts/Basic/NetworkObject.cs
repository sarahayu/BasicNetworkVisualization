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
    public NetworkData networkData;
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
    int numControllersPressed = 0;
    Vector3 ogControllerRelPos = Vector3.zero;
    Vector3 ogControllersPos;
    Vector3 ogNetworkScale;
    Quaternion ogNetworkRot;
    Vector3 ogNetworkPos;
    Vector3[] grabPoints = new Vector3[2];
    int numGrabPoints = 0;

    void Start()
    {
        // cameraTransform = worldCamera.GetComponent<Transform>();

        int nodeIndex = 0;
        foreach (var node in networkData.nodes)
        {
            var interactableNodeP = Instantiate(nodePrefab, new Vector3(node.x, node.y, node.z) * 3, Quaternion.identity);
            interactableNodeP.GetComponent<NodeDisplay>().Init(this, node.color, node.name, nodeIndex);
            // interactableNodeP.GetComponent<Rigidbody>().detectCollisions = false;
            var nodeTransform = interactableNodeP.transform;
            nodeTransform.SetParent(GetComponent<Transform>());
            var objectPrefab = nodeTransform.Find("MeshContainer").Find("NodeObject");
            // var sphere = objectPrefab.Find("Sphere");
            // var label = objectPrefab.Find("Label");
            // TMP.faceColor = new Color(TMP.faceColor.r, TMP.faceColor.g, TMP.faceColor.b, textFadeOut);

            nodeObjects.Add(new NodeObject() {
                data = node,
                nodeObject = interactableNodeP,
                color = node.color,
                idInNetworkObject = nodeIndex
            });

            nodeIndex++;
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
            var rectPrism = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rectPrism.transform.SetParent(transform);

            var source = nodeObjects[link.source].nodeObject;
            var target = nodeObjects[link.target].nodeObject;
            var toTarget = target.transform.localPosition - source.transform.localPosition;
            var dist = toTarget.magnitude;

            var thickness = Mathf.Pow(Mathf.Lerp(minLinkThickness, maxLinkThickness, (float)link.value / max), 1.0f / lineThicknessGrowth);

            rectPrism.transform.localScale = new Vector3(thickness, dist, thickness);
            rectPrism.transform.localPosition = source.transform.localPosition + toTarget / 2;

            // var toTargetWorld = target.transform.position - source.transform.position;
            // var dec = Mathf.Atan2(Mathf.Sqrt(toTarget.x * toTarget.x + toTarget.z * toTarget.z), toTarget.y);
            // var ra = Mathf.Atan2(toTarget.x, toTarget.z);
            // rectPrism.transform.RotateAround(source.transform.position, transform.rotation * Vector3.right, dec * Mathf.Rad2Deg);
            // rectPrism.transform.RotateAround(source.transform.position, transform.rotation * Vector3.up, ra * Mathf.Rad2Deg);
            rectPrism.transform.LookAt(target.transform.position);
            rectPrism.transform.RotateAround(rectPrism.transform.position, rectPrism.transform.right, 90);

            rectPrism.GetComponent<MeshRenderer>().material.shader = Shader.Find("Shaders/Particle Standard Surface");
            

            var mesh = rectPrism.GetComponent<MeshFilter>().mesh;
            var vertices = mesh.vertices;
            var colors = new Color[vertices.Length];

            Color colorSource = nodeObjects[link.source].color.CloneAndDesat(0.5f).ToRGB(), 
                colorTarget = nodeObjects[link.target].color.CloneAndDesat(0.5f).ToRGB();
            foreach (var ind in color1Ind)
                colors[ind] = colorSource;
            foreach (var ind in color2Ind)
                colors[ind] = colorTarget;
            
            mesh.colors = colors;

            links.Add(rectPrism);
            nodeObjects[link.source].links.Add(new IndexedLink() {
                data = link,
                index = linkIndex
            });
            nodeObjects[link.target].links.Add(new IndexedLink() {
                data = link,
                index = linkIndex
            });

            linkIndex++;
            // spheres.Add(rectPrism);
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
        int countIdx = 0;
        foreach (var node in nodeObjects)
        {
            bool selected = eventData.groupsSelected.Any(item => item.id == countIdx);
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
            countIdx++;
        }
    }

    public void OnDeselectEvent(DeselectionEventData eventData)
    {
        foreach (var link in links)
            link.SetActive(true);
        foreach (var node in nodeObjects)
            node.nodeObject.SetActive(true);
    }

    public void Rearrange(int movedIndex)
    {
        List<int> nodesWithMovedAsTarget = new List<int>();
        foreach (var link in nodeObjects[movedIndex].links)
        {
            var srcInd = link.data.source;
            var tarInd = link.data.target;
            
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
}
