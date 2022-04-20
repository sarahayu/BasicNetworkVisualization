using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JSONNetworkData
{
    public JSONNodeData[] nodes;
    public JSONLinkData[] links;
}

[Serializable]
public class JSONNodeData
{
    public bool virtualNode;
    public int height;
    public int idx;
    public string label;
    public int ancIdx;
    public int[] childIdx;
}

[Serializable]
public class JSONLinkData
{
    public int sourceIdx;
    public int targetIdx;
}