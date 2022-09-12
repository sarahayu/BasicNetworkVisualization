using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootballNetworkData : NetworkData
{
    public override void ParseFromString()
    {
        var fileData = JsonUtility.FromJson<FootballFileData>(dataFile.text);

        // groupColors = idx: int, groupColor: Color
        var groupColors = new Dictionary<int, HSV>();

        foreach (var node in fileData.nodes)
        {
            // if this node is leaf node and we haven't init'ed a color for the group...
            if (!node.virtualNode && !groupColors.ContainsKey(node.ancIdx))
                groupColors[node.ancIdx] = ColorUtil.GenerateRandomHSV();
        }

        var nodeList = new List<NodeData>();

        int perRow = 11, counter = 0;
        float step = 0.2f, startX = -(perRow * step) / 2;

        foreach (var fileNode in fileData.nodes)
        {
            if (!fileNode.virtualNode)
            {
                float curX = (counter % perRow) * step + startX, curY = (counter / perRow) * step;

                nodeList.Add(new NodeData() {
                    name = fileNode.label,
                    id = fileNode.idx,
                    group = fileNode.ancIdx,
                    color = groupColors[fileNode.ancIdx],
                    x = curX,
                    y = curY,
                    z = 0,
                    active = true
                });

                counter++;
            }
        }
        
        var linkList = new List<LinkData>();

        foreach (var fileLink in fileData.links)
        {
            linkList.Add(new LinkData() {
                source = fileLink.sourceIdx,
                target = fileLink.targetIdx,
                value = 1,
            });
        }

        nodes = nodeList.ToArray();
        links = linkList.ToArray();
    }
}
