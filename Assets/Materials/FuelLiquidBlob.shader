// Per-droplet field contribution. Each droplet sprite draws a soft radial
// gradient that fades to zero at the sprite edge. The field camera renders these
// ADDITIVELY, so where droplets overlap the summed value rises above the
// composite threshold and they read as one connected mass. Standard 2D metaball
// field pass, written for URP.
Shader "FuelLiquid/Blob"
{
    Properties
    {
        _MainTex ("Sprite (unused, for SpriteRenderer)", 2D) = "white" {}
        _Intensity ("Intensity", Range(0,4)) = 1
        _Falloff ("Falloff Power", Range(0.25,8)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        // Additive blending — droplets sum into the field.
        Blend One One
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

            float _Intensity;
            float _Falloff;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                // Distance from sprite centre (uv 0.5,0.5), normalised so edge = 1.
                float2 d = IN.uv - 0.5;
                float dist = saturate(length(d) * 2.0);

                // Smooth falloff to zero at the edge; pow shapes the gradient.
                float field = pow(saturate(1.0 - dist), _Falloff) * _Intensity;

                // Field magnitude carried in all channels; only strength matters.
                // (The composite reads .r, but writing RGBA keeps it debuggable.)
                return float4(field, field, field, field);
            }
            ENDHLSL
        }
    }
}
