using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;


// [Serializable]
// public class Link
// {
//     public bool spline = true;
//     public int linkIdx;
//     public int sourceIdx;
//     public int targetIdx;
//     public bool localLayout = false;

//     // the shortest path between two nodes in the hierarchical tree
//     // public List<Node> pathInTree = new List<Node>();

//     public IList<Vector3> straightenPoints = new List<Vector3>();
//     // public LinkDraw linkDraw = new LinkDraw();

//     // Control Points for Edge Bundling
//     // public Vector3[] ControlPoints(Network network)
//     // {
//     //     //TODO There has to some better way to do this, and also to provide a constant number of control points

//     //     var clusterS = network.communities[network[sourceIdx].communityIdx];
//     //     var clusterT = network.communities[network[targetIdx].communityIdx];
//     //     var sCenter = clusterS.massCenter;
//     //     var tCenter = clusterT.massCenter;
//     //     // if (network.is2D)
//     //     // {
//     //     //     sCenter.z = 0.25f;
//     //     //     tCenter.z = 0.25f;
//     //     // }
//     //     // // Between focus and context
//     //     // if (clusterS.focus && !clusterT.focus)
//     //     // {
//     //     //     return new [] {
//     //     //         network[sourceIdx].Position3D,
//     //     //         (sCenter + tCenter) / 2,
//     //     //         tCenter,
//     //     //         network[targetIdx].Position3D
//     //     //     };
//     //     // }
//     //     // if (clusterT.focus && !clusterS.focus)
//     //     // {
//     //     //     return new [] {
//     //     //         network[sourceIdx].Position3D,
//     //     //         sCenter,
//     //     //         (sCenter + tCenter) / 2,
//     //     //         network[targetIdx].Position3D
//     //     //     };
//     //     // }

//     //     Vector3[] result = new Vector3[pathInTree.Count];
//     //     for (int i = 0; i < pathInTree.Count; i++)
//     //     {
//     //         result[i] = pathInTree[i].Position3D;
//     //     }

//     //     // TODO Optimization
//     //     //pathInTree.ForEach((n) =>
//     //     //{
//     //     //    // normalization
//     //     //    retval.Add((n.Position3D));
//     //     //});

//     //     //return retval;

//     //     return result;
//     // }
// }
public class straighthenParam
{
    public LinkData l;
    public SharedNetworkData networkData;
    public Dictionary<int, Vector3> cms;
    public ManualResetEvent mrEvent;
    public float beta;
    public float throttleDist;
}

public class BasisSpline
{
    protected LinkData link;

    // the control points to generate B Spline
    public Vector3[] StraightenPoints;

    // Methods for Multi-thread Computing Spline Control Points
    public void ComputeSplineController(object param)
    {
        straighthenParam p = (straighthenParam)param;
        link = p.l;
        Straighten(p.networkData, p.beta, p.cms, p.throttleDist);
    }
    
    public void Straighten(SharedNetworkData networkData, float beta, Dictionary<int, Vector3> cms, float throttleDist)
    {
        NodeData srcNode = networkData.nodes[link.source], tarNode = networkData.nodes[link.target];

        Vector3 source = new Vector3(srcNode.pos3D[0], srcNode.pos3D[1], srcNode.pos3D[2]);
        Vector3 target = new Vector3(tarNode.pos3D[0], tarNode.pos3D[1], tarNode.pos3D[2]);
        Vector3 dVector3 = target - source;
        var lenSq = dVector3.sqrMagnitude;

        Vector3[] controlPoints = srcNode.group == tarNode.group ? 
        new Vector3[] { 
            source,
            target,
         } : 
         new Vector3[] {
            source,
            Vector3.Lerp(cms[srcNode.group], cms[tarNode.group], (throttleDist * throttleDist) / lenSq),
            Vector3.Lerp(cms[srcNode.group], cms[tarNode.group], 1 - (throttleDist * throttleDist) / lenSq),
            target,
         };

        int length = controlPoints.Length;

        StraightenPoints = new Vector3[length + 2];

        StraightenPoints[0] = source;

        for (int i = 0; i < length; i++)
        {
            Vector3 point = controlPoints[i];

            StraightenPoints[i + 1].x = beta * point.x + (1 - beta) * (source.x + (i + 1) * dVector3.x / length);
            StraightenPoints[i + 1].y = beta * point.y + (1 - beta) * (source.y + (i + 1) * dVector3.y / length);
            StraightenPoints[i + 1].z = beta * point.z + (1 - beta) * (source.z + (i + 1) * dVector3.z / length);
        }
        StraightenPoints[length + 1] = target;

        // return new List<Vector3>(straightenPoints);
    }
}