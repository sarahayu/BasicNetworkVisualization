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
//     public Vector3[] ControlPoints(Network network)
//     {
//         //TODO There has to some better way to do this, and also to provide a constant number of control points

//         var clusterS = network.communities[network[sourceIdx].communityIdx];
//         var clusterT = network.communities[network[targetIdx].communityIdx];
//         var sCenter = clusterS.massCenter;
//         var tCenter = clusterT.massCenter;
//         // if (network.is2D)
//         // {
//         //     sCenter.z = 0.25f;
//         //     tCenter.z = 0.25f;
//         // }
//         // // Between focus and context
//         // if (clusterS.focus && !clusterT.focus)
//         // {
//         //     return new [] {
//         //         network[sourceIdx].Position3D,
//         //         (sCenter + tCenter) / 2,
//         //         tCenter,
//         //         network[targetIdx].Position3D
//         //     };
//         // }
//         // if (clusterT.focus && !clusterS.focus)
//         // {
//         //     return new [] {
//         //         network[sourceIdx].Position3D,
//         //         sCenter,
//         //         (sCenter + tCenter) / 2,
//         //         network[targetIdx].Position3D
//         //     };
//         // }

//         Vector3[] result = new Vector3[pathInTree.Count];
//         for (int i = 0; i < pathInTree.Count; i++)
//         {
//             result[i] = pathInTree[i].Position3D;
//         }

//         // TODO Optimization
//         //pathInTree.ForEach((n) =>
//         //{
//         //    // normalization
//         //    retval.Add((n.Position3D));
//         //});

//         //return retval;

//         return result;
//     }
// }
// public class straighthenParam
// {
//     public Link l;
//     public Network network;
//     public float spaceScale;
//     public ManualResetEvent mrEvent;
//     public float beta;
// }

// public class BasisSpline
// {
//     protected Link link;

//     // the control points to generate B Spline
//     public Vector3[] ScaledStraighthenPoints;

//     // Methods for Multi-thread Computing Spline Control Points
//     public void ComputeSplineController(object param)
//     {
//         straighthenParam p = (straighthenParam)param;
//         link = p.l;
//         Straighten(p.network, p.beta, p.spaceScale);
//     }
    
//     public void Straighten(Network network, float beta, float spaceScale)
//     {
//         Vector3[] controlPoints = link.ControlPoints(network);
//         int length = controlPoints.Length;
//         Vector3 source = controlPoints[0];
//         Vector3 target = controlPoints[length - 1];
//         Vector3 dVector3 = target - source;

//         Vector3[] straightenPoints = new Vector3[length + 2];
//         ScaledStraighthenPoints = new Vector3[length + 2];

//         straightenPoints[0] = source;
//         ScaledStraighthenPoints[0] = straightenPoints[0] * spaceScale;

//         for (int i = 0; i < length; i++)
//         {
//             Vector3 point = controlPoints[i];

//             straightenPoints[i + 1].x = beta * point.x + (1 - beta) * (source.x + (i + 1) * dVector3.x / length);
//             straightenPoints[i + 1].y = beta * point.y + (1 - beta) * (source.y + (i + 1) * dVector3.y / length);
//             straightenPoints[i + 1].z = beta * point.z + (1 - beta) * (source.z + (i + 1) * dVector3.z / length);

//             ScaledStraighthenPoints[i + 1] = straightenPoints[i + 1] * spaceScale;
//         }
//         straightenPoints[length + 1] = target;
//         ScaledStraighthenPoints[length + 1] = straightenPoints[length + 1] * spaceScale;

//         link.straightenPoints = new List<Vector3>(straightenPoints);
//     }
// }