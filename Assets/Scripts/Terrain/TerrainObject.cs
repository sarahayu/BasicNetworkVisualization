using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TerrainObject : MonoBehaviour
{
    public TextAsset networkJsonFile;
    // public GameObject nodePrefab;
    // public Camera worldCamera;
    // public Transform rightController;
    // public Transform leftController;
    
    // Transform cameraTransform;

    public bool autoUpdate = false;
    JSONNetworkData data;

    MeshFilter meshFilter = null;
    MeshRenderer meshRenderer = null;
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

    public void GenerateTerrain()
    {
        if (!meshFilter)
        {
            meshFilter = FindObjectOfType<MeshFilter>();
            meshRenderer = FindObjectOfType<MeshRenderer>();
        }

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
            new TerrainNodeData{ x = 130 * 241 / 720, y = 132 * 241 / 720, size = 1 },
            new TerrainNodeData{ x = 484 * 241 / 720, y = 77 * 241 / 720, size = 2 },
            new TerrainNodeData{ x = 267 * 241 / 720, y = 213 * 241 / 720, size = 5 },
            new TerrainNodeData{ x = 495 * 241 / 720, y = 268 * 241 / 720, size = 4 },
            new TerrainNodeData{ x = 171 * 241 / 720, y = 439 * 241 / 720, size = 3 },
            new TerrainNodeData{ x = 276 * 241 / 720, y = 600 * 241 / 720, size = 1 },
            new TerrainNodeData{ x = 543 * 241 / 720, y = 581 * 241 / 720, size = 3 },
        };

        meshFilter.sharedMesh = TerrainMeshGenerator.GenerateFromHeights(HeightMap.GenerateFromGraph(graph, 241, 241));

        print("hello?");
    }
}
