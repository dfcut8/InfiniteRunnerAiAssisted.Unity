Shader "PixelRunner/FlatSprite"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Silhouette("Silhouette", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; UNITY_SKINNED_VERTEX_INPUTS };
            struct Varyings { COMMON_2D_OUTPUTS half4 color:COLOR; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Silhouette;
            CBUFFER_END
            Varyings vert(Attributes a)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(a);
                SetUpSpriteInstanceProperties();
                a.positionOS = UnityFlipSprite(a.positionOS, unity_SpriteProps.xy);
                Varyings o = CommonUnlitVertex(a);
                o.color = a.color * _Color * unity_SpriteColor;
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                half4 c = CommonUnlitFragment(i, i.color);
                clip(c.a - 0.5);
                return half4(lerp(c.rgb, i.color.rgb, _Silhouette), i.color.a);
            }
            ENDHLSL
        }
    }
}
