using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this should contain all the information we need about the Network shared between the main view and terrain overview
// TODO make this templated to allow for more flexibility??
public abstract class SharedNetworkData : MonoBehaviour
{
    public TextAsset dataFile;
    public NodeData[] nodes;
    public LinkData[] links;

    void Awake()
    {
        ParseFromString();
    }

    public abstract void ParseFromString();
}

public class NodeData
{
    public string name;
    public int id;
    public int group;
    public HSV color;
    public float[] pos3D;
    public float[] pos2D;
    public bool active;
    public bool isVirtual;
}

public class LinkData
{
    public int source;
    public int target;
    public int value;
}
