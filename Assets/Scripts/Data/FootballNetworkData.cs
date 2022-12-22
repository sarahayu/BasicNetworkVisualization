using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using EpForceDirectedGraph.cs;

public class FootballNetworkData : SharedNetworkData
{
    // nodes is node idx in groups, links is all links
    private static Dictionary<string, float[]> ForceDirectIntragroup(IEnumerable<int> nodeIdxs, IEnumerable<FootballFileDataLink> links, float[] origin)
    {
        Graph fdGraph = new Graph();

        fdGraph.CreateNodes(nodeIdxs.Select(n => n.ToString()).ToList());

        fdGraph.CreateEdges(
            links
                .Where(e => nodeIdxs.Any(n => n == e.sourceIdx) && nodeIdxs.Any(n => n == e.targetIdx))
                .Select(e => {
                    return new Triple<string, string, EdgeData>(
                        fdGraph.GetNode(e.sourceIdx.ToString()).ID, 
                        fdGraph.GetNode(e.targetIdx.ToString()).ID, 
                        new EdgeData{ label = e.sourceIdx.ToString() + "-" + e.targetIdx.ToString() }
                    );
                }).ToList()
        );

        float stiffness = 10000f;
        float repulsion = 0.01f;
        float damping   = 0.01f;

        ForceDirected3D fdPhysics = new ForceDirected3D(fdGraph, stiffness, repulsion, damping);
        FDRenderer renderer = new FDRenderer(fdPhysics);

        renderer.Draw(0.01f);

        // print(origin[0]);
        // print(origin[1]);
        // print(origin[2]);

        return renderer.positions.Select(p => {
            p.Value[0] = p.Value[0] * 0.1f + origin[0];
            p.Value[1] = p.Value[1] * 0.1f + origin[1];
            p.Value[2] = p.Value[2] * 0.1f + origin[2];
            return p;
        }).ToDictionary(p => p.Key, p => p.Value);
    }

    private static Dictionary<string, float[]> ForceDirectIntergroup(FootballFileData fileData)
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
        foreach (var node in fileData.nodes)
        {
            if (!node.virtualNode && !groupSizes.ContainsKey(node.ancIdx))
            {
                groupSizes.Add(node.ancIdx, fileData.nodes[node.ancIdx].childIdx.Length);
                groupCols.Add(node.ancIdx, ColorUtil.TryGetHSV(node.color));
                if (fileData.nodes[node.ancIdx].pos2D != null)
                    groupPos.Add(node.ancIdx, (float[])fileData.nodes[node.ancIdx].pos2D.Clone());
            }
        }

        // loop through json links and bookkeep them to groupLinks, also find out group degrees
        foreach (var link in fileData.links)
        {
            // check if the ancestor of the one of the link's source nodes is inside our groupSizes
            // (since both link nodes are presumably leaves, we only need to check one of them)
            if (groupSizes.ContainsKey(fileData.nodes[link.sourceIdx].ancIdx))
            {
                int group1 = fileData.nodes[link.sourceIdx].ancIdx, group2 = fileData.nodes[link.targetIdx].ancIdx;

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
                    var hash = fileData.nodes[group1].label.ToString() + "-" + fileData.nodes[group2].label.ToString();

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

        Graph fdGraph = new Graph();

        fdGraph.CreateNodes(groupSizes.Keys.Select(n => {
            return new EpForceDirectedGraph.cs.NodeData{
                label = n.ToString(),
                mass = groupSizes[n]
            };
        }).ToList());

        fdGraph.CreateEdges(groupLinks.Select(e => {
            return new Triple<string, string, EdgeData>(
                fdGraph.GetNode(e.Value.Item1.ToString()).ID, 
                fdGraph.GetNode(e.Value.Item2.ToString()).ID, 
                new EpForceDirectedGraph.cs.EdgeData{ 
                    label = e.Value.Item1.ToString() + "-" + e.Value.Item2.ToString(), 
                    length = e.Value.Item3
                }
            );
        }).ToList());

        float stiffness = 100f;
        float repulsion = 0.001f;
        float damping   = 0.001f;

        ForceDirected3D fdPhysics = new ForceDirected3D(fdGraph, stiffness, repulsion, damping);
        FDRenderer renderer = new FDRenderer(fdPhysics);

        renderer.Draw(0.05f);

        return groupDegs.Keys.SelectMany(g => ForceDirectIntragroup(fileData.nodes[g].childIdx, fileData.links, renderer.positions[g.ToString()])).ToDictionary(g => g.Key, g => g.Value);
    }

    public override void ParseFromString()
    {
        var fileData = JsonUtility.FromJson<FootballFileData>(dataFile.text);

        // groupColors = idx: int, groupColor: Color
        var groupColors = new Dictionary<int, HSV>();

        foreach (var node in fileData.nodes)
        {
            // if this node is leaf node and we haven't init'ed a color for the group...
            // this also assumes nodes of the same group have the same color (bad idea??)
            if (!node.virtualNode && !groupColors.ContainsKey(node.ancIdx))
                groupColors[node.ancIdx] = node.color != null ? ColorUtil.TryGetHSV(node.color) : ColorUtil.GenerateRandomHSV();
        }

        var nodeList = new List<NodeData>();

        int perRow = 11, counter = 0;
        float step = 0.2f, startX = -(perRow * step) / 2;

        var poss = ForceDirectIntergroup(fileData);

        foreach (var fileNode in fileData.nodes)
        {

            if (fileNode.virtualNode)
            {
                nodeList.Add(new NodeData() {
                    name = fileNode.label,
                    id = fileNode.idx,
                    group = fileNode.ancIdx,
                    color = new HSV(Color.black),
                    pos3D = fileNode.pos3D == null
                        ? new float[] {
                            0, 0, 0
                        }
                        : new float[] {
                            fileNode.pos3D[0],
                            fileNode.pos3D[1],
                            fileNode.pos3D[2]
                        },
                    pos2D = fileNode.pos2D == null
                        ? null
                        : new float[] {
                            fileNode.pos2D[0],
                            fileNode.pos2D[1]
                        },
                    active = true,
                    children = fileNode.childIdx,
                    isVirtual = true,
                });
            }
            else
            {
                var pos = poss[fileNode.idx.ToString()];
                float curX = (counter % perRow) * step + startX, curY = (counter / perRow) * step;

                nodeList.Add(new NodeData() {
                    name = fileNode.label,
                    id = fileNode.idx,
                    group = fileNode.ancIdx,
                    color = groupColors[fileNode.ancIdx],
                    pos3D = fileNode.pos3D == null
                        ? new float[] {
                            curX, curY, 0
                        }
                        : new float[] {
                            pos[0],
                            pos[1],
                            pos[2]
                        },
                    pos2D = fileNode.pos2D == null
                        ? null
                        : new float[] {
                            fileNode.pos2D[0],
                            fileNode.pos2D[1]
                        },
                    active = true,
                    children = fileNode.childIdx,
                    isVirtual = false,
                });

                counter++;
            }
        }
        
        var linkList = new List<LinkData>();

        int linkIdx = 0;
        foreach (var fileLink in fileData.links)
        {
            linkList.Add(new LinkData() {
                source = fileLink.sourceIdx,
                target = fileLink.targetIdx,
                value = 1,
                id = linkIdx++,
            });
        }

        nodes = nodeList.ToArray();
        links = linkList.ToArray();
    }
}
