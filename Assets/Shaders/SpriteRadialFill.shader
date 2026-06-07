Shader "TBTD/Sprite Radial Fill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FilledColor ("Filled Color", Color) = (1, 1, 1, 1)
        _EmptyColor ("Empty Color", Color) = (1, 1, 1, 0.18)
        _FillAmount ("Fill Amount", Range(0, 1)) = 1
        _Feather ("Edge Feather", Range(0, 0.05)) = 0.01
        _Clockwise ("Clockwise", Float) = 1
        _StartAngle ("Start Angle Degrees", Range(-360, 360)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FilledColor;
                half4 _EmptyColor;
                float _FillAmount;
                float _Feather;
                float _Clockwise;
                float _StartAngle;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 spriteColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float fillAmount = saturate(_FillAmount);

                float2 centeredUv = input.uv - float2(0.5, 0.5);
                float angle = atan2(centeredUv.x, centeredUv.y) - radians(_StartAngle);
                float normalizedAngle = frac(angle * (1.0 / TWO_PI) + 1.0);

                if (_Clockwise < 0.5)
                {
                    normalizedAngle = frac(1.0 - normalizedAngle);
                }

                float sector = 0.0;
                if (fillAmount >= 0.999)
                {
                    sector = 1.0;
                }
                else if (fillAmount > 0.001)
                {
                    float feather = max(_Feather, 0.0001);
                    sector = 1.0 - smoothstep(fillAmount, fillAmount + feather, normalizedAngle);
                }

                half4 tint = lerp(_EmptyColor, _FilledColor, sector);
                return spriteColor * tint;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
