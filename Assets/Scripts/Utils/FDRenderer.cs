using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpForceDirectedGraph.cs;

public class FDRenderer : AbstractRenderer
{
    public Dictionary<string, float[]> positions { get; } = new Dictionary<string, float[]>();
    
    public FDRenderer(IForceDirected iForceDirected) : base(iForceDirected)
    {
    }

    public override void Clear()
    {
    }

    protected override void drawEdge(Edge iEdge, AbstractVector iPosition1, AbstractVector iPosition2)
    {
    }

    protected override void drawNode(Node iNode, AbstractVector iPosition)
    {
        positions.Add(iNode.Data.label, new float[] { iPosition.x, iPosition.y, iPosition.z });
    }
}
