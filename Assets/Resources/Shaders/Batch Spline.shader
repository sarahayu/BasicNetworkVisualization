Shader "Custom/Batch Spline"
{
    Properties
    {
        _LineWidth ("LineWidth", Range (0, 0.05)) = 0.015
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 200
        //Blend SrcAlpha OneMinusSrcAlpha
        //Blend SrcAlpha One
        // BlendOp Add
		// Blend SrcAlpha One
        BlendOp Add
		Blend SrcAlpha OneMinusSrcAlpha
	    ZWrite Off

        Pass
        {
            CGPROGRAM
			#pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
           

            #include "UnityCG.cginc"
			#include "BSplineData.cginc"

            float _LineWidth; // Used to adjust the line thickness

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert (uint vid : SV_VertexID)
            {
                // Find current sample point index 
                uint SplineSampleIdx = vid/6; // Every samplepoint translates to 6 vertices = 2 triangles

                v2f o;
                // Skip early if this is the end of a line 
                if (OutSamplePointData[SplineSampleIdx].SplineIdx != OutSamplePointData[SplineSampleIdx + 1].SplineIdx) {
                    // Set some invalid values
                    // It does not matter as the fragment is going to be discarded anyway in the fragment stage
                    o.vertex = float4(0,0,0,1);
                    o.color = float4(1,1,1,0);
                    o.uv = float2(1,1);
                    return o;
                }

                // We will use this offset to determine which points to pick for tangent calculation
                // If the vid % 6 is either 0, 1 or 3 means that it belongs to the current point -> SplineSampleIdx
                // However since we create triangles that reach to the next point from each current point we need
                // another tangent for that follow-up point
                // With this offset we control which points are picked for tangent calculation
                int idxOffset = 0;
                if (vid % 6 == 2 || vid % 6 == 4 || vid % 6 == 5) { // Checks if the vertex that vid indicates lies on the current or next point
                    idxOffset = 1;
                }
                SplineSampleIdx += idxOffset;

                // Calculate the normal and from that the offset for the current line segment
                float3 curr = OutSamplePointData[SplineSampleIdx].Position;
                float3 prev = OutSamplePointData[SplineSampleIdx - 1].Position;
                float3 next = OutSamplePointData[SplineSampleIdx + 1].Position;


                float3 tangent = (next - prev);
                float3 viewDir = normalize(curr.xyz - _WorldSpaceCameraPos.xyz);
                float3 normal = normalize(cross(viewDir, tangent));

                float3 offset = normalize(mul(UNITY_MATRIX_MV, normal)) * (_LineWidth/2);

                // Transform the point to ViewSpace and add the offset
                curr = UnityObjectToViewPos(OutSamplePointData[SplineSampleIdx].Position);
                if (vid % 6 == 0 || vid % 6 == 2 || vid % 6 == 5) { // Checks if the vertex that vid indicates lies on the bottom or top of the line
                    curr -= offset;
                } else {
                    curr += offset;
                }
                
                // Depending on the position get the correct sample point, apply the offset and get the color
                // Here we render the segment from the current point to the next one using 2 tris
             

                o.vertex = mul(UNITY_MATRIX_P, float4(curr, 1));
                o.color = OutSamplePointData[SplineSampleIdx].ColorRGBA;
                o.uv = float2(0,0);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Discard line ends if the marker was set
                if (i.uv.x == 1) {
                    discard;
                }
                return i.color;
            }

            ENDCG
        }
    }
}