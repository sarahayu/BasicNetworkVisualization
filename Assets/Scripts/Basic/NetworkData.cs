using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NetworkData
{
    public NodeData[] nodes;
    public LinkData[] links;
}

[Serializable]
public class NodeData
{
    public string id;
    public int group;
    public string color;
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class LinkData
{
    public int source;
    public int target;
    public int value;
}