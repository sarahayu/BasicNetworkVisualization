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

    TerrainNetworkData data;
    // int numControllersPressed = 0;
    // Vector3 ogControllerRelPos = Vector3.zero;
    // Vector3 ogControllersPos;
    // Vector3 ogNetworkScale;
    // Quaternion ogNetworkRot;
    // Vector3 ogNetworkPos;
    // Vector3[] grabPoints = new Vector3[2];
    // int numGrabPoints = 0;

    void Start()
    {        
        data = JsonUtility.FromJson<TerrainNetworkData>(networkJsonFile.text);
        foreach (var node in data.nodes)
        {
            print(node.label);
        }
    }

    void FixedUpdate()
    {
    }
}
