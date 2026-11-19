Shader "Game/PixelWaterSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _PixelsPerUnit ("Water pixel density", Float) = 16
        _FramesPerSecond ("Animation frames per second", Float) = 6
        _WaveSpeed ("Wave speed", Float) = 0.4
        _RippleStrength ("Ripple brightness", Range(0,0.2)) = 0.045
        _HighlightStrength ("Small highlights", Range(0,0.4)) = 0.12
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
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; fixed4 color:COLOR; };
            sampler2D _MainTex;
            float _PixelAnimationFPS;
            float _PixelsPerUnit, _FramesPerSecond, _WaveSpeed, _RippleStrength, _HighlightStrength;
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv;
                o.world=mul(unity_ObjectToWorld,v.vertex).xy; o.color=v.color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                // Only water-colored pixels move in brightness. Rock/earth borders retain their original pixels.
                float mask=step(c.r*1.12+0.005,c.g)*step(c.r*1.02,c.b+0.02)*step(0.1,c.a);
                float ppu=max(1,_PixelsPerUnit), fps=max(1,_PixelAnimationFPS);
                float2 p=floor(i.world*ppu)/ppu;
                float t=floor(_Time.y*fps)/fps*_WaveSpeed;
                float wave=sin(p.y*6.0+p.x*0.8-t*2.4)*sin(p.x*2.1+p.y*0.4+t);
                float crest=step(0.94,sin(p.y*8.5+p.x*0.35-t*2.0));
                float dash=step(0.76,sin(p.x*5.0-p.y*2.0+t*1.8));
                float glint=crest*dash;
                c.rgb*=1.0+wave*_RippleStrength*mask;
                c.rgb=lerp(c.rgb,c.rgb+fixed3(.28,.35,.31),glint*_HighlightStrength*mask);
                return c*i.color;
            }
            ENDCG
        }
    }
}
