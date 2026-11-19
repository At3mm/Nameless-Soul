Shader "Game/DebuffColorOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _WaterLine ("Water line", Float) = -10000
        _Immersion ("Immersion", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float y:TEXCOORD1; fixed4 color:COLOR; };
            sampler2D _MainTex;
            float _WaterLine, _Immersion;
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.y=mul(unity_ObjectToWorld,v.vertex).y; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                if (_Immersion > 0.001) clip(i.y - _WaterLine);
                return fixed4(i.color.rgb, tex2D(_MainTex,i.uv).a * i.color.a);
            }
            ENDCG
        }
    }
}
