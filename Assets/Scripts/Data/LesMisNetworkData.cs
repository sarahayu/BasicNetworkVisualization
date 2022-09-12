using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LesMisNetworkData : NetworkData
{
    public override void ParseFromString()
    {
        var fileData = JsonUtility.FromJson<LesMisFileData>(dataFile.text);

        nodes = new NodeData[fileData.nodes.Length];
        links = new LinkData[fileData.links.Length];

        for (int i = 0; i < fileData.nodes.Length; i++)
        {
            var fileNode = fileData.nodes[i];

            nodes[i] = new NodeData() {
                name = fileNode.id,
                id = i,             // just use unique int as id for now
                group = fileNode.group,
                color = ColorUtil.TryGetHSV(fileNode.color),
                x = fileNode.x,
                y = fileNode.y,
                z = fileNode.z,
            };
        }

        for (int i = 0; i < fileData.links.Length; i++)
        {
            var fileLink = fileData.links[i];

            links[i] = new LinkData() {
                source = fileLink.source,
                target = fileLink.target,
                value = fileLink.value,
            };
        }
    }
}
