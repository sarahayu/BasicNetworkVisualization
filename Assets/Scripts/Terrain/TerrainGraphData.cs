using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGraphData
{
    public List<TerrainNodeData> nodes;
    public List<TerrainLinkData> links;

    public static TerrainGraphData CreateFromJSONData(JSONNetworkData json, float width, float height)
    {
        List<int> groups = new List<int>();
        TerrainGraphData graphData = new TerrainGraphData();
        graphData.nodes = new List<TerrainNodeData>();
        graphData.links = new List<TerrainLinkData>();
        // loop through nodes and track which ones have children (for now, we'll consider these to be the root of "groups")
        foreach (var node in json.nodes)
        {
            if (node.childIdx.Length != 0)
            {
                groups.Add(node.idx);
                graphData.nodes.Add(new TerrainNodeData{ size = node.childIdx.Length });
            }
        }
        float rad = Mathf.Max(width, height) / 2;
        List<Vector3> points = new List<Vector3>();
        MathUtil.populateSunflower(points, rad * 0.8f, groups.Count, 1);

        for (int i = 0; i < graphData.nodes.Count; i++)
        {
            graphData.nodes[i].x = (int)(points[i].x + rad);
            graphData.nodes[i].y = (int)(points[i].z + rad);
        }

        // loop through links and depending on which groups it connects, assign weights to those group connections
        foreach(var link in json.links)
        {
            // Debug.Log("adding");
            int indSource = groups.IndexOf(link.sourceIdx), indTarget = groups.IndexOf(link.targetIdx);

            if (indSource != -1 && indTarget != -1)
            {
                graphData.links.Add(new TerrainLinkData{ source = indSource, target = indTarget, weight = 1 });
            }
        }
        // TODO find out if leaf nodes have links to other groups' leaf nodes???
        return graphData;
    }
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