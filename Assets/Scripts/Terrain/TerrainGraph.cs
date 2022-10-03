using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGraph
{
    public List<TerrainNode> nodes = new List<TerrainNode>();
    public List<TerrainLink> links = new List<TerrainLink>();
    public float width;
    public float height;
    float _maxLinks;

    public TerrainGraph(float width, float height, int maxLinks)
    {
        this.width = width;
        this.height = height;

        _maxLinks = maxLinks;
    }

    // we take in Texture as parameter instead of TerrainOutline just so this can be generalizeable (anti-KISS? perhaps)
    public List<int> GetNodeIdxsContainedInOutline(Texture2D outlineTex)
    {
        List<int> inOutline = new List<int>();
        foreach (var node in nodes)
            if (TextureUtil.IsWhite(outlineTex, new Vector2(node.x / width, node.y / height)))
                foreach (var childInd in node.parsedNode.childIdx)
                    inOutline.Add(childInd);
        return inOutline;
    }

    // TODO refactor all of this
    public void CreateFromJSONData(FootballFileData JSONData)
    {

        // keeps track of group sizes
        Dictionary<int, int> groupSizes = new Dictionary<int, int>();
        // keeps track of group degree
        Dictionary<int, int> groupDegs = new Dictionary<int, int>();
        // keeps track of group colors
        Dictionary<int, HSV> groupCols = new Dictionary<int, HSV>();
        // keeps track of group pos
        Dictionary<int, float[]> groupPos = new Dictionary<int, float[]>();
        // keeps track of index in groupSizes mapped to actual idx in json data
        List<int> groupIndToJsonIdx = new List<int>();
        // keep track of group links; Item1 = group1 index, Item2 = group2 index, Item3 = weight
        Dictionary<string, Tuple<int, int, int>> groupLinks = new Dictionary<string, Tuple<int, int, int>>();

        // loop through json nodes and keep track of the groups made by leaf nodes
        foreach (var node in JSONData.nodes)
        {
            if (!node.virtualNode && !groupSizes.ContainsKey(node.ancIdx))
            {
                groupSizes.Add(node.ancIdx, JSONData.nodes[node.ancIdx].childIdx.Length);
                groupCols.Add(node.ancIdx, ColorUtil.TryGetHSV(node.color));
                if (JSONData.nodes[node.ancIdx].pos2D != null)
                    groupPos.Add(node.ancIdx, (float[])JSONData.nodes[node.ancIdx].pos2D.Clone());
            }
        }

        // loop through json links and bookkeep them to groupLinks, also find out group degrees
        foreach (var link in JSONData.links)
        {
            // check if the ancestor of the one of the link's source nodes is inside our groupSizes
            // (since both link nodes are presumably leaves, we only need to check one of them)
            if (groupSizes.ContainsKey(JSONData.nodes[link.sourceIdx].ancIdx))
            {
                int group1 = JSONData.nodes[link.sourceIdx].ancIdx, group2 = JSONData.nodes[link.targetIdx].ancIdx;

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
                    var hash = JSONData.nodes[group1].label.ToString() + "-" + JSONData.nodes[group2].label.ToString();

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

        if (groupPos.Count != 0)
            MathUtil.Normalize2DPointsAsCircle(groupPos);

        // make actual nodes in our terrain data and keep track of its index in graphData
        // the way we have it, distributing via sunflower means nodes near the back of the list will be rendered closer to rim of map
        // so let's sort so that those with highest degree are near the rim because this makes center of map looks less cluttered
        // (also then sort by height because why not)
        foreach (var idxAndDeg in groupDegs.OrderBy(_idxAndDeg => -_idxAndDeg.Value).ThenBy(_idxAndDeg => groupSizes[_idxAndDeg.Key]))
        {
            nodes.Add(new TerrainNode { 
                size = groupSizes[idxAndDeg.Key],
                idx = idxAndDeg.Key,
                color = groupCols[idxAndDeg.Key],
                parsedNode = JSONData.nodes[idxAndDeg.Key],
                x = groupPos.Count == 0 ? 0 : (int)(groupPos[idxAndDeg.Key][0] * width),
                y = groupPos.Count == 0 ? 0 : (int)(groupPos[idxAndDeg.Key][1] * height),
            });
            groupIndToJsonIdx.Add(idxAndDeg.Key);

        }

        if (groupPos.Count == 0)
        {
            // distribute nodes using sunflower points
            float rad = Mathf.Max(width, height) / 2;
            List<Vector3> points = new List<Vector3>();
            MathUtil.PopulateSunflowerPoints(points, rad * 0.8f, groupSizes.Count, 1);

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].x = (int)(points[i].x + rad);
                nodes[i].y = (int)(points[i].z + rad);
            }
        }

        if (groupLinks.Count > _maxLinks)
        {
            Debug.Log("Number of links exceeded MAX_LINKS " + _maxLinks + "! Deleting smallest " + (groupLinks.Count - _maxLinks) + " links...");
        }

        int numLinks = 0;

        // loop through groupLinks and create graph data links
        foreach (var groupLink in groupLinks.Values.OrderBy(_SrcTgtWgt => MathUtil.RelWeight(_SrcTgtWgt.Item3, groupSizes[_SrcTgtWgt.Item1], groupSizes[_SrcTgtWgt.Item2])))
        {
            numLinks++;
            if (numLinks > _maxLinks)
            {
                Debug.Log("Largest link deleted of relative weight " + MathUtil.RelWeight(groupLink.Item3, groupSizes[groupLink.Item1], groupSizes[groupLink.Item2]));
                break;
            }
            // unhash key to get idxs of source and target groups
            int sourceIdx = groupLink.Item1, targetIdx = groupLink.Item2;
            float weight = groupLink.Item3;
            links.Add(new TerrainLink { 
                source = groupIndToJsonIdx.IndexOf(sourceIdx), 
                target = groupIndToJsonIdx.IndexOf(targetIdx), 
                weight = (int)weight,
                });
        }
    }
}

// for specifying what data property maps to terrain peak height (note that peaks represent GROUPS of nodes)
enum QuantifiablePropertyNode
{
    SIZE,
    INTRACONNECTEDNESS,
}

// for specifying what data property maps to terrain edge slackness
enum QuantifiablePropertyEdge
{
    INTERCONNECTEDNESS_RELATIVE_PRODUCT,            // # of connections / max # of connections possible between the two groups
    INTERCONNECTEDNESS_RELATIVE_MAX,            // # of connections / max # of nodes in either group
}

// wrapper for parsed node data
public class TerrainNode
{
    public FootballFileDataNode parsedNode;
    public int x, y;
    public int size;
    public int idx;     // idx of ancestor node that owns this group
    public HSV color;
}

public class TerrainLink
{
    public int source;
    public int target;
    public int weight;
}