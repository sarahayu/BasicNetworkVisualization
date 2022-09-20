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
            // this also assumes nodes of the same group have the same color (bad idea??)
            if (!node.virtualNode && !groupColors.ContainsKey(node.ancIdx))
                groupColors[node.ancIdx] = node.color != null ? ColorUtil.TryGetHSV(node.color) : ColorUtil.GenerateRandomHSV();
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
                    pos3D = fileNode.pos3D == null
                        ? new float[] {
                            curX, curY, 0
                        }
                        : new float[] {
                            fileNode.pos3D[0] * 3,
                            fileNode.pos3D[1] * 3,
                            fileNode.pos3D[2] * 3
                        },
                    pos2D = fileNode.pos2D == null
                        ? null
                        : new float[] {
                            fileNode.pos2D[0],
                            fileNode.pos2D[1]
                        },
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
