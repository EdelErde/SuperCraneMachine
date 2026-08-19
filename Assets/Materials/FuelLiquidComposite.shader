// Reads the accumulated field RenderTexture and thresholds it into the visible
// liquid. Below the threshold = transparent; above = solid liquid colour, with a
// soft anti-aliased edge and a light fake-specular sheen derived from the field
// gradient. Standard 2D metaball composite pass, written for URP.
Shader "FuelLiquid/Composite"
{
    Properties
    {
        _FieldTex ("Field Texture", 2D) = "black" {}
        _Threshold ("Threshold", Range(0,1)) = 0.5
        _EdgeSoftness ("Edge Softness", Range(0.001,0.5)) = 0.05
        _LiquidColor ("Liquid Color", Color) = (0.95, 0.75, 0.15, 1)
        _Sheen ("Sheen Strength", Range(0,1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

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
            float4 _FieldTex_TexelSize;

            float _Threshold;
            float _EdgeSoftness;
            float4 _LiquidColor;
            float _Sheen;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float SampleField(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_FieldTex, sampler_FieldTex, uv).r;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float field = SampleField(IN.uv);

                // Soft threshold -> alpha. smoothstep gives the anti-aliased edge.
                float alpha = smoothstep(_Threshold - _EdgeSoftness,
                                         _Threshold + _EdgeSoftness,
                                         field);
                if (alpha <= 0.0)
                    return float4(0, 0, 0, 0);

                // Fake wet sheen: use the field gradient as a pseudo-normal and add a
                // soft highlight where the surface faces "up-left".
                float2 texel = _FieldTex_TexelSize.xy;
                float fx = SampleField(IN.uv + float2(texel.x, 0))
                         - SampleField(IN.uv - float2(texel.x, 0));
                float fy = SampleField(IN.uv + float2(0, texel.y))
                         - SampleField(IN.uv - float2(0, texel.y));
                float3 n = normalize(float3(-fx, -fy, 0.35));
                float3 lightDir = normalize(float3(-0.5, 0.7, 0.5));
                float sheen = saturate(dot(n, lightDir));
                sheen = pow(sheen, 4.0) * _Sheen;

                float3 rgb = _LiquidColor.rgb + sheen;
                return float4(rgb, alpha * _LiquidColor.a);
            }
            ENDHLSL
        }
    }
}
