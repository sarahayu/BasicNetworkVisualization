using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGraphData
{
    public TerrainNodeData[] nodes;
    public TerrainLinkData[] links;
}

public class TerrainNodeData
{
    public int x, y;
    public int size;
}

public class TerrainLinkData
{
    public int source;
    public int target;
    public int weight;
}