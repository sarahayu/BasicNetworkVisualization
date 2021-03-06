using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGraphData
{
    public NetworkData networkData;
    public List<TerrainNodeData> nodes;
    public List<TerrainLinkData> links;
    public float width;
    public float height;

    public List<NodeData> GetContainedInOutline(TerrainOutline outline)
    {
        List<NodeData> inOutline = new List<NodeData>();
        foreach (var node in nodes)
            if (outline.PointInOutline(new Vector2(node.x / width, node.y / height)))
                foreach (var childInd in node.parsedNode.childIdx)
                    inOutline.Add(networkData.nodes[childInd]);
        return inOutline;
    }

    public static TerrainGraphData CreateFromJSONData(NetworkData generalNetworkData, FootballFileData parsedFileData, float width, float height)
    {
        TerrainGraphData graphData = new TerrainGraphData();
        graphData.networkData = generalNetworkData;
        graphData.nodes = new List<TerrainNodeData>();
        graphData.links = new List<TerrainLinkData>();
        graphData.width = width;
        graphData.height = height;

        // keeps track of group sizes
        Dictionary<int, int> groupSizes = new Dictionary<int, int>();
        // keeps track of group degree
        Dictionary<int, int> groupDegs = new Dictionary<int, int>();
        // keeps track of group colors
        Dictionary<int, Color> groupCols = new Dictionary<int, Color>();
        // keeps track of index in groupSizes mapped to actual idx in json data
        List<int> groupIndToJsonIdx = new List<int>();
        // keep track of group links; Item1 = group1 index, Item2 = group2 index, Item3 = weight
        Dictionary<string, Tuple<int, int, int>> groupLinks = new Dictionary<string, Tuple<int, int, int>>();

        // loop through json nodes and keep track of the groups made by leaf nodes
        foreach (var node in parsedFileData.nodes)
        {
            if (!node.virtualNode && !groupSizes.ContainsKey(node.ancIdx))
            {
                groupSizes.Add(node.ancIdx, parsedFileData.nodes[node.ancIdx].childIdx.Length);
                groupCols.Add(node.ancIdx, generalNetworkData.nodes[node.idx].color);
            }
        }

        // loop through json links and bookkeep them to groupLinks, also find out group degrees
        foreach (var link in parsedFileData.links)
        {
            // check if the ancestor of the one of the link's source nodes is inside our groupSizes
            // (since both link nodes are presumably leaves, we only need to check one of them)
            if (groupSizes.ContainsKey(parsedFileData.nodes[link.sourceIdx].ancIdx))
            {
                int group1 = parsedFileData.nodes[link.sourceIdx].ancIdx, group2 = parsedFileData.nodes[link.targetIdx].ancIdx;

                if (group1 != group2)
                {
                    // don't want duplicate of links if source and target end up flipped in another link.
                    // we're creating a hash from node idxs of the groups, so keep things consistent by having group2 > group1 always
                    if (group1 > group2)
                    {
                        int temp = group1;
                        group1 = group2;
                        group2 = temp;
                    }

                    // create hash of two group idxs in order to make sure links between two groups always have the same entry in groupLinks
                    var hash = parsedFileData.nodes[group1].label.ToString() + "-" + parsedFileData.nodes[group2].label.ToString();

                    // increment link weight
                    if (!groupLinks.ContainsKey(hash))
                        groupLinks.Add(hash, Tuple.Create(group1, group2, 1));
                    else
                        groupLinks[hash] = Tuple.Create(group1, group2, groupLinks[hash].Item3 + 1);
                        
                    // increment group degrees
                    if (!groupDegs.ContainsKey(group1))
                        groupDegs.Add(group1, 1);
                    else
                        groupDegs[group1]++;
                    if (!groupDegs.ContainsKey(group2))
                        groupDegs.Add(group2, 1);
                    else
                        groupDegs[group2]++;
                }
            }
        }

        // make actual nodes in our terrain data and keep track of its index in graphData
        // the way we have it, distributing via sunflower means nodes near the back of the list will be rendered closer to rim of map
        // so let's sort so that those with highest degree are near the rim because this makes center of map looks less cluttered
        // (also then sort by height because why not)
        foreach (var idxAndDeg in groupDegs.OrderBy(_idxAndDeg => -_idxAndDeg.Value).ThenBy(_idxAndDeg => groupSizes[_idxAndDeg.Key]))
        {
            graphData.nodes.Add(new TerrainNodeData { size = groupSizes[idxAndDeg.Key], idx = idxAndDeg.Key, color = groupCols[idxAndDeg.Key], parsedNode = parsedFileData.nodes[idxAndDeg.Key] });
            groupIndToJsonIdx.Add(idxAndDeg.Key);

        }
        // distribute nodes using sunflower points
        float rad = Mathf.Max(width, height) / 2;
        List<Vector3> points = new List<Vector3>();
        MathUtil.populateSunflower(points, rad * 0.8f, groupSizes.Count, 1);

        for (int i = 0; i < graphData.nodes.Count; i++)
        {
            graphData.nodes[i].x = (int)(points[i].x + rad);
            graphData.nodes[i].y = (int)(points[i].z + rad);
        }

        // loop through groupLinks and create graph data links
        foreach (var groupLink in groupLinks.Values)
        {
            // unhash key to get idxs of source and target groups
            int sourceIdx = groupLink.Item1, targetIdx = groupLink.Item2;
            float weight = groupLink.Item3;
            graphData.links.Add(new TerrainLinkData { 
                source = groupIndToJsonIdx.IndexOf(sourceIdx), 
                target = groupIndToJsonIdx.IndexOf(targetIdx), 
                weight = (int)weight,
                });
        }

        return graphData;
    }
}

// wrapper for parsed node data
public class TerrainNodeData
{
    public FootballNodeFileData parsedNode;
    public int x, y;
    public int size;
    public int idx;     // idx of ancestor node that owns this group
    public Color color;
}

public class TerrainLinkData
{
    public int source;
    public int target;
    public int weight;
}