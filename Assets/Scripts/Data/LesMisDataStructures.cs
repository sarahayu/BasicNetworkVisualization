using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LesMisFileData
{
    public LesMisNodeFileData[] nodes;
    public LesMisLinkFileData[] links;
}

[Serializable]
public class LesMisNodeFileData
{
    public string id;
    public int group;
    public string color;
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class LesMisLinkFileData
{
    public int source;
    public int target;
    public int value;
}