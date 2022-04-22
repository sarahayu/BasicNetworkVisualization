using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TerrainObject : MonoBehaviour
{
    const int MAX_WIDTH = 81;

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

    public void GenerateTerrain()
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
            new TerrainNodeData{ x = 130 * MAX_WIDTH / 720, y = 132 * MAX_WIDTH / 720, size = 1 },
            new TerrainNodeData{ x = 484 * MAX_WIDTH / 720, y = 77 * MAX_WIDTH / 720, size = 2 },
            new TerrainNodeData{ x = 267 * MAX_WIDTH / 720, y = 213 * MAX_WIDTH / 720, size = 5 },
            new TerrainNodeData{ x = 495 * MAX_WIDTH / 720, y = 268 * MAX_WIDTH / 720, size = 4 },
            new TerrainNodeData{ x = 171 * MAX_WIDTH / 720, y = 439 * MAX_WIDTH / 720, size = 3 },
            new TerrainNodeData{ x = 276 * MAX_WIDTH / 720, y = 600 * MAX_WIDTH / 720, size = 1 },
            new TerrainNodeData{ x = 543 * MAX_WIDTH / 720, y = 581 * MAX_WIDTH / 720, size = 3 },
        };

        // var heightMap = new HeightMap(
        //     scaleHeight: scaleHeight, 
        //     falloff: falloff * MAX_WIDTH, 
        //     width: MAX_WIDTH, 
        //     height: MAX_WIDTH
        // ).GenerateFromGraph(graph);

        meshFilter.sharedMesh = TerrainMeshGenerator.GenerateFromGraph(
            graph: graph,
            scaleHeight: scaleHeight, 
            falloff: falloff * MAX_WIDTH, 
            width: MAX_WIDTH, 
            height: MAX_WIDTH
        );

        print("rip?");
    }
}
