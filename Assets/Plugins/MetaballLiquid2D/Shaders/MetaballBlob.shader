Shader "Metaball/BlobField"
{
    // Draws a single blob as a soft radial gradient (bright center -> 0 at the edge)
    // using ADDITIVE blending. This shader is never seen directly by the player -
    // it is only rendered by the offscreen "Metaball Camera" into the field
    // RenderTexture that MetaballComposite.shader reads and thresholds.
    //
    // Put sprites using this material on the dedicated "Liquid" layer only.

    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Field Color / Tint", Color) = (1,1,1,1)
        _Intensity ("Field Intensity", Range(0,4)) = 1.0
        _Falloff ("Edge Falloff (higher = sharper falloff)", Range(0.1,8)) = 2.0
        _InnerRadius ("Inner Radius (flat bright core, 0-0.9)", Range(0,0.9)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One   // additive: overlapping blobs sum their field values
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
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            half4 _Color;
            float _Intensity;
            float _Falloff;
            float _InnerRadius;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // IMPORTANT: this math assumes the sprite's UVs cover the full
                // 0-1 quad (i.e. the sprite's Mesh Type is "Full Rect" in its
                // import settings, not "Tight"). See README for details.
                float2 centered = IN.uv * 2.0 - 1.0;
                float dist = length(centered);

                float field = 1.0 - saturate((dist - _InnerRadius) / max(0.0001, 1.0 - _InnerRadius));
                field = pow(saturate(field), _Falloff);

                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float value = field * _Intensity * IN.color.a * texCol.a;

                return half4(IN.color.rgb * value, value);
            }
            ENDHLSL
        }
    }
}
