// Link States 
// #define LINKSTATE_HIGHLIGHT 0
// #define LINKSTATE_CONTEXT 1
// #define LINKSTATE_FOCUS2CONTEXT 2
// #define LINKSTATE_FOCUS 3
// #define LINKSTATE_NORMAL 4
// #define LINKSTATE_HIGHLIGHTFOCUS 5
#define LINKSTATE_INACTIVE 0
#define LINKSTATE_ACTIVE 1

// Colors and Transparency
float4 COLOR_FOCUS;
float4 COLOR_HIGHLIGHT;
float COLOR_MINIMUM_ALPHA;
float COLOR_NORMAL_ALPHA_FACTOR;
float COLOR_CONTEXT_ALPHA_FACTOR;
float COLOR_FOCUS2CONTEXT_ALPHA_FACTOR;

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