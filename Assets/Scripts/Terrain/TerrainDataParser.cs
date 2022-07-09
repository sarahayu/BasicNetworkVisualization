using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDataParser : NetworkDataParser
{
    public override NetworkData ParseFromString(string JSONStr)
    {
        var data = new NetworkData();
        var links = new List<LinkData>();
        var nodes = new List<NodeData>();


        var terrainJSON = JsonUtility.FromJson<JSONNetworkData>(JSONStr);

        
    }
}
