Shader "Metaball/LiquidComposite"
{
    // Reads the metaball field RenderTexture (written by blobs using
    // MetaballBlob.shader through the offscreen Metaball Camera) and turns it
    // into the actual liquid look: a hard-edged (but anti-aliased) shape
    // wherever the summed field exceeds a threshold, plus an optional rim
    // highlight near the edge to sell the "liquid" read.
    //
    // This is the ONLY thing the player actually sees - put this material on
    // a quad that covers the same world-space area the Metaball Camera views.

    Properties
    {
        _FieldTex ("Field Texture (from Metaball Camera)", 2D) = "black" {}
        _LiquidColor ("Liquid Color", Color) = (0.2, 0.6, 1.0, 1)
        _Threshold ("Threshold", Range(0, 2)) = 0.55
        _EdgeSoftness ("Edge Softness", Range(0.001, 1)) = 0.08
        _RimColor ("Rim / Highlight Color", Color) = (1, 1, 1, 1)
        _RimWidth ("Rim Width", Range(0, 1)) = 0.18
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.5
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_FieldTex);
            SAMPLER(sampler_FieldTex);

            half4 _LiquidColor;
            half4 _RimColor;
            float _Threshold;
            float _EdgeSoftness;
            float _RimWidth;
            float _RimIntensity;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half field = SAMPLE_TEXTURE2D(_FieldTex, sampler_FieldTex, IN.uv).r;

                // Solid body: anti-aliased threshold cutoff.
                float body = smoothstep(_Threshold - _EdgeSoftness, _Threshold + _EdgeSoftness, field);

                // Rim highlight: a soft band just inside the edge, fading out
                // deeper into the blob's interior.
                float rim = body * (1.0 - smoothstep(_Threshold, _Threshold + _RimWidth, field));
                rim *= _RimIntensity;

                half3 color = _LiquidColor.rgb + _RimColor.rgb * rim;
                half alpha = body * _LiquidColor.a;

                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
