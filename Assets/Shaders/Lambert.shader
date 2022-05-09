Shader "Custom/Lambert"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        // _Glossiness ("Smoothness", Range(0,1)) = 0.0
        // _Metallic ("Metallic", Range(0,1)) = 0.0
        _Levels("Levels", Float) = 10.0
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

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        sampler2D _BumpMap;
        sampler2D _HeightMap;
        float _Levels;

        float maxHeight;
        float useHeightMap;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        fixed4 _Color;

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

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            float stepFactor;
            if (useHeightMap == 1
                && IN.uv_MainTex.x <= _MainTex_TexelSize.z && IN.uv_MainTex.y <= _MainTex_TexelSize.w
                && IN.uv_MainTex.x >= 0 && IN.uv_MainTex.y >= 0)
                stepFactor = tex2D (_HeightMap, IN.uv_MainTex);
            else
                stepFactor = inverseLerp(0, maxHeight + 0.01, IN.worldPos.y);
            float modds = fmod(stepFactor, 1.0 / _Levels);
            stepFactor += -modds + ceil(modds * _Levels) / _Levels;
            o.Albedo = 1 - (1 - _Color) * (1 - (stepFactor + c.rgb));
            o.Normal = tex2D(_BumpMap, IN.uv_MainTex);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
