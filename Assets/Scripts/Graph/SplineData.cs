using System;
using UnityEngine;

// Sample data, used to render lines
public struct SplineSamplePointData
{
    public Vector3 Position;
    public Color ColorRGBA;
    public uint SplineIdx;

    public SplineSamplePointData(Vector3 Position, Color ColorRGBA, uint SplineIdx)
    {
        this.Position = Position;
        this.ColorRGBA = ColorRGBA;
        this.SplineIdx = SplineIdx;
    }

    public static int size()
    {
        return sizeof(float) * 3 + sizeof(float) * 4 + sizeof(uint) * 1;
    }
}

// Control point of a spline
public struct SplineControlPointData
{
    public Vector3 Position;

    public SplineControlPointData(Vector3 Position)
    {
        this.Position = Position;
    }

    public static int size()
    {
        return sizeof(float) * 3;
    }
}

// Metadata about a spline segment
// A spline segment is made up of multiple control- and sample points
public struct SplineSegmentData
{
    public uint SplineIdx;             // Index of the Spline this segment belongs to
    public uint BeginControlPointIdx;  // Index of the first Vertex that is part of this Spline Segment
    public uint BeginSamplePointIdx;   // Index of the first SplineSamplePoint for this Spline Segment
    public uint NumSamples;            // Number of Samples in this Spline Segment

    public SplineSegmentData(uint SplineIdx, uint BeginControlPointIdx, uint NumSamples, uint BeginSamplePointIdx)
    {
        this.SplineIdx = SplineIdx;
        this.BeginControlPointIdx = BeginControlPointIdx;
        this.NumSamples = NumSamples;
        this.BeginSamplePointIdx = BeginSamplePointIdx;
    }

    public static int size()
    {
        return sizeof(uint) * 4;
    }
}

// Metadata about a spline
// A spline is made up of multiple segments
public struct SplineData
{
    public uint Idx;                   // Index of this spline
    public uint NumSegments;           // Number of total segments in this spline
    public uint BeginSplineSegmentIdx; // Index of the first spline segment
    public uint NumSamples;       // Number of total samples in this splines (sum of samples in all segments)
    public uint BeginSamplePointIdx;   // Index of the first SplineSamplePoint in this spline

    public Vector3 StartPosition;       // First point of the spline
    public Vector3 EndPosition;         // Last point of the spline
    public Color StartColorRGBA;        // Start color of the spline
    public Color EndColorRGBA;          // End color of the spline
    public uint LinkState;              // HighLight (0), Context (1), Focus2Context (2), Focus (3), Normal (4). This will influence how the spline is drawn in terms of shape (straight/curved) color and alpha.

    public SplineData(uint Idx, uint NumSegments, uint BeginSplineSegmentIdx, uint NumSamples, uint BeginSamplePointIdx, Vector3 StartPosition, Vector3 EndPosition, Color StartColorRGBA, Color EndColorRGBA, uint LinkState = 4)
    {
        this.Idx = Idx;
        this.NumSegments = NumSegments;
        this.BeginSplineSegmentIdx = BeginSplineSegmentIdx;
        this.NumSamples = NumSamples;
        this.BeginSamplePointIdx = BeginSamplePointIdx;

        this.StartPosition = StartPosition;
        this.EndPosition = EndPosition;
        this.StartColorRGBA = StartColorRGBA;
        this.EndColorRGBA = EndColorRGBA;
        this.LinkState = LinkState;
    }

    public static int size()
    {
        return sizeof(uint) * 5 + sizeof(float) * 3 * 2 + sizeof(float) * 4 * 2 + sizeof(uint) * 1;
    }
}