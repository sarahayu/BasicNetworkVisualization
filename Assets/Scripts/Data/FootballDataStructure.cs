using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FootballFileData
{
    public FootballFileDataNode[] nodes;
    public FootballFileDataLink[] links;
}

[Serializable]
public class FootballFileDataNode
{
    public bool virtualNode;
    public int height;
    public int idx;
    public string label;
    public string color;
    public int ancIdx;
    public int[] childIdx;
    public float[] pos2D;
    public float[] pos3D;
}

[Serializable]
public class FootballFileDataLink
{
    public int sourceIdx;
    public int targetIdx;
}