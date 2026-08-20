// Per-droplet contribution to the SHARED liquid field. Each droplet draws its soft
// circle sprite tinted by its own _Color (set per-droplet via MaterialPropertyBlock).
// Rendered ADDITIVELY so overlapping droplets sum. To support many liquids in one
// field, we accumulate:
//   RGB = sum of (dropletColor.rgb * weight)     -> weighted color
//   A   = sum of weight                          -> total field strength
// The composite then divides RGB by A to recover the blended color, and thresholds A
// for the silhouette. This is what lets fuel/water/etc. share one field yet keep their
// colors (and blend where they overlap). Original HLSL; technique inspired by Code
// Monkey's additive-field metaball approach.
Shader "LiquidField/Blob"
{
    Properties
    {
        _MainTex ("Sprite (soft circle)", 2D) = "white" {}
        _Color ("Tint (set per-droplet)", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0,4)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One            // additive accumulation into the field
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

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            // _Color is set per-droplet via MaterialPropertyBlock; declared here so it can
            // be instanced without creating material copies.
            float4 _Color;
            float _Intensity;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                // Soft radial falloff from the sprite's own alpha.
                float w = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a * _Intensity * _Color.a;
                // Premultiply color by weight; alpha carries the weight itself.
                return float4(_Color.rgb * w, w);
            }
            ENDHLSL
        }
    }
}
