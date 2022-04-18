using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TerrainNetworkData
{
    public TerNodeData[] nodes;
    public TerLinkData[] links;
}

[Serializable]
public class TerNodeData
{
    public bool virtualNode;
    public int height;
    public int idx;
    public string label;
    public int ancIdx;
    public int[] childIdx;
}

[Serializable]
public class TerLinkData
{
    public int sourceIdx;
    public int targetIdx;
}