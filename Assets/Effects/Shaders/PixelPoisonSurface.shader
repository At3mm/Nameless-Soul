Shader "Game/PixelPoisonSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _PixelsPerUnit ("Pixel density", Float) = 16
        _FramesPerSecond ("Animation frames per second", Float) = 4
        _Speed ("Poison movement speed", Float) = 0.3
        _BubbleStrength ("Bubble brightness", Range(0,1)) = 0.28
        _TintStrength ("Poison green tint", Range(0,1)) = 0.75
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
            float _PixelsPerUnit,_FramesPerSecond,_Speed,_BubbleStrength,_TintStrength;
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv;
                o.world=mul(unity_ObjectToWorld,v.vertex).xy; o.color=v.color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                float mask=step(c.r*1.06+0.003,c.g)*step(c.b*0.85,c.g)*step(0.1,c.a);
                float2 p=floor(i.world*max(1,_PixelsPerUnit))/max(1,_PixelsPerUnit);
                float t=floor(_Time.y*max(1,_PixelAnimationFPS))/max(1,_PixelAnimationFPS)*_Speed;
                float luminance=dot(c.rgb,float3(.299,.587,.114));
                float3 poison=float3(luminance*.65,luminance*1.65,luminance*.32);
                c.rgb=lerp(c.rgb,poison,_TintStrength*mask);
                float drift=sin(p.x*3.0+p.y*1.5+t)*sin(p.y*2.0-t*.8);
                c.rgb*=1+drift*.065*mask;
                float2 cell=floor(p*1.4), local=frac(p*1.4);
                float seed=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
                float age=frac(t+seed*7.0);
                float radius=lerp(.04,.19,age);
                float ring=1-step(.035,abs(length(local-float2(.35+seed*.25,.5))-radius));
                float bubble=ring*step(.62,seed)*step(.12,age)*(1-step(.72,age));
                c.rgb+=bubble*mask*_BubbleStrength*float3(.45,.65,.12);
                return c*i.color;
            }
            ENDCG
        }
    }
}
