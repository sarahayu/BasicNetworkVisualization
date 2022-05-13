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
        Dictionary<int, int> childIndToGroupInd = new Dictionary<int, int>();
        List<int> groupSizes = new List<int>();
        TerrainGraphData graphData = new TerrainGraphData();
        graphData.nodes = new List<TerrainNodeData>();
        graphData.links = new List<TerrainLinkData>();
        int nodeInd = 0;

        // loop through nodes and track which ones have children (for now, we'll consider these to be the root of "groups")
        foreach (var node in json.nodes)
        {
            // only group those whose children are leaves
            if (node.childIdx.Length > 1 && !json.nodes[node.childIdx[0]].virtualNode)
            {
                foreach (var child in node.childIdx)
                {
                    childIndToGroupInd.Add(child, nodeInd);
                }
                groupSizes.Add(node.childIdx.Length);
                graphData.nodes.Add(new TerrainNodeData{ size = node.childIdx.Length });
                nodeInd++;
            }
        }
        float rad = Mathf.Max(width, height) / 2;
        List<Vector3> points = new List<Vector3>();
        MathUtil.populateSunflower(points, rad * 0.8f, nodeInd, 1);

        for (int i = 0; i < graphData.nodes.Count; i++)
        {
            graphData.nodes[i].x = (int)(points[i].x + rad);
            graphData.nodes[i].y = (int)(points[i].z + rad);
        }

        Dictionary<int, int> linkWeights = new Dictionary<int, int>();

        // loop through links and depending on which groups it connects, assign weights to those group connections
        foreach(var link in json.links)
        {
            // Debug.Log("adding");
            if (childIndToGroupInd.ContainsKey(link.sourceIdx) && childIndToGroupInd.ContainsKey(link.targetIdx))
            {
                int group1 = childIndToGroupInd[link.sourceIdx], group2 = childIndToGroupInd[link.targetIdx];

                if (group1 != group2)
                {
                    if (group1 > group2)
                    {
                        int temp = group1;
                        group1 = group2;
                        group2 = temp;
                    }

                    int indID = group2 * 100 + group1;
                    if (!linkWeights.ContainsKey(indID))
                        linkWeights.Add(indID, 1);
                    else
                        linkWeights[indID]++;
                }
                    // graphData.links.Add(new TerrainLinkData{ source = group1, target = group2, weight = 1 });
            }
        }

        foreach (var entry in linkWeights)
        {
            int sourceInd = entry.Key / 100, targetInd = entry.Key % 100;
            // Debug.LogFormat("source {0} target {1} groupSizes {2}", sourceInd, targetInd, groupSizes.Count);
            float weight = Mathf.Min(groupSizes[sourceInd], groupSizes[targetInd]) * 100 / entry.Value;
            graphData.links.Add(new TerrainLinkData{ source = sourceInd, target = targetInd, weight = (int)weight });
            // Debug.LogFormat("Group 1 size {0} ind {4} g2 {1} ind {5} links {2} prop {3}", groupSizes[sourceInd], groupSizes[targetInd], entry.Value, 100 / weight, sourceInd, targetInd);
        }

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