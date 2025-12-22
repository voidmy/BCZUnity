Shader "Universal Render Pipeline/GrassBezier"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        _P0_WS ("P0 Root", Vector) = (0,0,0,0)
        _P1_WS ("P1 Control 1", Vector) = (0,0.33,0,0)
        _P2_WS ("P2 Control 2", Vector) = (0,0.66,0,0)
        _P3_WS ("P3 Tip", Vector) = (0,1,0,0)
        _GrassHeight ("Grass Height (OS)", Float) = 1.0
        _BendAmount ("Bend Amount", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue"      = "Transparent"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // SRP Batcher / GPU Instancing（可以后续再按需加）
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "GrassBezier.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;

            float3 _P0_WS;
            float3 _P1_WS;
            float3 _P2_WS;
            float3 _P3_WS;
            float  _GrassHeight;
            float  _BendAmount;

            struct Attributes
            {
                float3 positionOS : POSITION;   // 模型空间顶点
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert (Attributes input)
            {
                Varyings output;

                float3 posOS = input.positionOS;

                // 顶点沿草高度的参数 t (0=根, 1=顶)
                float t = (_GrassHeight > 0.0001) ? saturate(posOS.y / _GrassHeight) : 0.0;

                // 直立形态（未弯曲）的位置
                float3 straightWS = TransformObjectToWorld(posOS);

                // 以 P0 为基准，只使用四个点之间的“形状差值”，忽略绝对世界位置
                float3 d1 = _P1_WS - _P0_WS;
                float3 d2 = _P2_WS - _P0_WS;
                float3 d3 = _P3_WS - _P0_WS;

                // 当前物体在世界空间的根位置（草根），贝塞尔曲线锚定在这里
                float3 rootWS = TransformObjectToWorld(float3(0, 0, 0));

                float3 p0WS = rootWS;
                float3 p1WS = rootWS + d1;
                float3 p2WS = rootWS + d2;
                float3 p3WS = rootWS + d3;

                // 贝塞尔曲线中轴（世界空间）
                float3 centerWS = Bezier3(p0WS, p1WS, p2WS, p3WS, t);

                // 近似切线和右向量，用于给草片一点宽度
                float3 tangentWS = Bezier3Tangent(p0WS, p1WS, p2WS, p3WS, t);
                float3 upWS      = float3(0, 1, 0);
                float3 rightWS   = normalize(cross(upWS, tangentWS));

                // 使用模型空间的 x 作为左右宽度偏移
                float width = posOS.x;
                float3 bentWS = centerWS + rightWS * width;

                // 使用全局 BendAmount 在直立与贝塞尔形状之间过渡
                float blend = saturate(_BendAmount); // 0=完全直立, 1=完全按照贝塞尔弯曲
                float3 finalWS = lerp(straightWS, bentWS, blend);

                float4 finalCS = TransformWorldToHClip(finalWS);

                output.positionHCS = finalCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = _Color;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 col = texColor * input.color;
                return col;
            }
            ENDHLSL
        }
    }
}
