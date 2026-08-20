// Reads the SHARED liquid field (RGB = sum of color*weight, A = sum of weight) and
// thresholds it into the visible liquid. Recovers per-pixel blended color by dividing
// RGB by A, then hard-steps A for the merged silhouette (like Code Monkey's Step-based
// Fluid graph). Because color comes from the field itself, many liquids render at once
// and blend where they overlap. Original URP HLSL.
Shader "LiquidField/Composite"
{
    Properties
    {
        _FieldTex ("Field Texture", 2D) = "black" {}
        _Threshold ("Threshold", Range(0,1)) = 0.5
        _EdgeSoftness ("Edge Softness", Range(0,0.3)) = 0.02
        _Tint ("Global Tint (multiplies all liquids)", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_FieldTex); SAMPLER(sampler_FieldTex);
            float _Threshold; float _EdgeSoftness; float4 _Tint;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float4 f = SAMPLE_TEXTURE2D(_FieldTex, sampler_FieldTex, IN.uv);
                float strength = f.a;                 // sum of weights

                // Hard threshold on total strength -> merged silhouette.
                float alpha = (_EdgeSoftness <= 0.0001)
                    ? step(_Threshold, strength)
                    : smoothstep(_Threshold - _EdgeSoftness, _Threshold + _EdgeSoftness, strength);
                if (alpha <= 0.0) return float4(0,0,0,0);

                // Recover blended color: RGB was accumulated as color*weight, so divide
                // by total weight. Guard against divide-by-zero at the fringe.
                float3 color = f.rgb / max(strength, 1e-4);
                return float4(color * _Tint.rgb, alpha * _Tint.a);
            }
            ENDHLSL
        }
    }
}
