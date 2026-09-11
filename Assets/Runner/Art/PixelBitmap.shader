Shader "PixelRunner/Bitmap"
{
    Properties
    {
        [PerRendererData] _MainTex("Font Atlas",2D) = "white" {}
        _Color("Tint",Color) = (1,1,1,1)
        _StencilComp("Stencil Comparison",Float) = 8
        _Stencil("Stencil ID",Float) = 0
        _StencilOp("Stencil Operation",Float) = 0
        _StencilWriteMask("Stencil Write Mask",Float) = 255
        _StencilReadMask("Stencil Read Mask",Float) = 255
        _ColorMask("Color Mask",Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct App { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Vary { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex; fixed4 _Color;
            Vary vert(App v)
            {
                Vary o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o;
            }
            fixed4 frag(Vary i):SV_Target
            {
                fixed alpha = tex2D(_MainTex,i.uv).a;
                clip(alpha-0.5);
                return fixed4(i.color.rgb,i.color.a);
            }
            ENDCG
        }
    }
}
