Shader "Custom/Lambert"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _LineTex ("Albedo (RGB)", 2D) = "black" {}
        _NodeColTex ("Node Colors (RGBA)", 2D) = "black" {}
        // _Glossiness ("Smoothness", Range(0,1)) = 0.0
        // _Metallic ("Metallic", Range(0,1)) = 0.0
        _NumLevels("Num Levels", Float) = 10.0
        _HeightMap("Height Map", 2D) = "black" {}
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Lambert fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        fixed4 _Color;
        sampler2D _LineTex;
        sampler2D _NodeColTex;
        sampler2D _HeightMap;
        sampler2D _BumpMap; 

        float _NumLevels;
        float _MaxHeight;
        float _CurvatureRadius;
        int _UseHeightMap;

        struct Input
        {
            float2 uv_LineTex;
            float3 worldPos;
        };

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float inverseLerp(float a, float b, float value)
        {
            return saturate((value - a) / (b - a));
        }

        float getSphericalHeight(float3 worldPos)
        {
            float3 ray = worldPos - float3(0, -_CurvatureRadius, 0);
            return length(ray) - _CurvatureRadius;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 linkLineCol = tex2D (_LineTex, IN.uv_LineTex);
            fixed4 nodeCol = tex2D (_NodeColTex, IN.uv_LineTex);
            float stepFactor;
            if (_UseHeightMap == 1)
                stepFactor = tex2D (_HeightMap, IN.uv_LineTex);
            else
                stepFactor = inverseLerp(0, _MaxHeight + 0.01, getSphericalHeight(IN.worldPos));
            float modds = fmod(stepFactor, 1.0 / _NumLevels);
            stepFactor += -modds + ceil(modds * _NumLevels) / _NumLevels;
            stepFactor *= 0.7;
            fixed4 nodeColAndBg = fixed4(_Color.rgb * (1 - nodeCol.a) + nodeCol.rgb * nodeCol.a, 1);
            o.Albedo = 1 - (1 - nodeColAndBg) * (1 - (stepFactor + linkLineCol.rgb));
            o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_LineTex));
            o.Alpha = linkLineCol.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
