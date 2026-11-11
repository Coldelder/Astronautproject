// LineRenderer 叠加发光（Additive）着色器
//
// 用法：把本材质挂到 LineRenderer 的 Material 上。
//   - 沿宽度方向：中心最亮，向两侧平滑衰减到全黑（不会硬边）。
//   - 叠加混合（Blend One One）：多条线重叠会自然积累亮度，配合 Bloom 出光晕。
//   - 不写深度、双面渲染，适合激光/能量束这类物体。
//
// 提示：想要"真正发光"的效果，需要在 URP Volume 里加 Bloom，
// 并把 Tint 或 Intensity 调到 1 以上（HDR 值）。
Shader "Custom/LineRenderer/LaserGlow"
{
    Properties
    {
        [MainTexture] _MainTex ("Beam Texture (可选)", 2D) = "white" {}
        [HDR] _Color ("Tint (HDR)", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 16)) = 2.0
        _CoreWidth ("Core Width 核心宽度", Range(0, 1)) = 0.25
        _CoreBoost ("Core Boost 核心增益", Range(0, 4)) = 1.0
        _Falloff ("Edge Falloff 边缘衰减", Range(0.25, 8)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        // 叠加混合：source + destination，越叠越亮
        Blend One One
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
            half4  color      : COLOR;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float2 uv          : TEXCOORD0;
            half4  color       : COLOR;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4  _Color;
            half   _Intensity;
            half   _CoreWidth;
            half   _CoreBoost;
            half   _Falloff;
        CBUFFER_END

        Varyings LaserGlowVert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
            OUT.color       = IN.color;
            return OUT;
        }

        half4 LaserGlowFrag(Varyings IN) : SV_Target
        {
            half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

            // LineRenderer 的 uv.y 是横跨线宽方向：0 和 1 在两条边上。
            // across: 中心为 0，两侧边缘为 1。
            half across = abs(IN.uv.y * 2.0h - 1.0h);

            // 1) 平滑的核心：越靠中心越亮，到 _CoreWidth 之外降为 0
            half coreWidth = max(_CoreWidth, 1e-4h);
            half core = 1.0h - smoothstep(0.0h, coreWidth, across);

            // 2) 向两侧的柔和拖尾：pow 曲线控制衰减速度
            half falloff = max(_Falloff, 1e-4h);
            half tail = pow(saturate(1.0h - across), falloff);

            // 两者叠加，核心负责"亮心"，tail 负责"渐隐到黑"
            half mask = saturate(core * _CoreBoost + tail);

            // 顶点色 / 贴图 / Tint 一起乘上去，方便 LineRenderer 用颜色渐变
            half3 rgb = _Color.rgb * tex.rgb * IN.color.rgb;
            half  alpha = _Color.a * tex.a * IN.color.a;

            // 加法混合下 alpha 也要参与，否则半透明线段会整条发亮
            return half4(rgb * mask * alpha * _Intensity, 1.0h);
        }
        ENDHLSL

        // 3D / Forward 渲染器
        Pass
        {
            Name "LaserGlowForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex LaserGlowVert
            #pragma fragment LaserGlowFrag
            #pragma target 2.0
            ENDHLSL
        }

        // 2D Renderer（本项目 Assets/Render/2d_Renderer.asset 用的就是它）
        Pass
        {
            Name "LaserGlow2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex LaserGlowVert
            #pragma fragment LaserGlowFrag
            #pragma target 2.0
            ENDHLSL
        }
    }

    FallBack Off
}
