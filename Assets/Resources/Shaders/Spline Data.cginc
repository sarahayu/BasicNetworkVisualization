// Link States
#define LINKSTATE_INACTIVE 0
#define LINKSTATE_ACTIVE 1

// Colors and Transparency
float COLOR_MINIMUM_ALPHA;
float COLOR_NORMAL_ALPHA_FACTOR;

// Structs for Data
struct SplineData {
    uint Idx;
	uint NumSegments;
	uint BeginSplineSegmentIdx;
	uint NumSamples;
	uint BeginSamplePointIdx;
	float3 StartPosition;
	float3 EndPosition;
	float4 StartColorRGBA;
	float4 EndColorRGBA;
	uint LinkState;
};

struct SplineSegmentData {
    uint SplineIdx;
	uint BeginControlPointIdx;
    uint BeginSamplePointIdx;
	uint NumSamples;
};

struct SplineControlPointData {
    float3 Position;
};

struct SplineSamplePointData {
	float3 Position;
	float4 ColorRGBA;
	uint SplineIdx;
};

// Data Buffers
StructuredBuffer<SplineData> InSplineData;
StructuredBuffer<SplineSegmentData> InSplineSegmentData;
StructuredBuffer<SplineControlPointData> InSplineControlPointData;

uint NumPoints;
// have to use normal (non RW) buffer for my <D3X11_1 graphics card >:(
#ifdef SHADER_CODE
	StructuredBuffer<SplineSamplePointData> OutSamplePointData;
#else
	RWStructuredBuffer<SplineSamplePointData> OutSamplePointData;
#endif