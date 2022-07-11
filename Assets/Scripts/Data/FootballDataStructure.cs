using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FootballFileData
{
    public FootballNodeFileData[] nodes;
    public FootballLinkFileData[] links;
}

[Serializable]
public class FootballNodeFileData
{
    public bool virtualNode;
    public int height;
    public int idx;
    public string label;
    public int ancIdx;
    public int[] childIdx;
}

[Serializable]
public class FootballLinkFileData
{
    public int sourceIdx;
    public int targetIdx;
}