Shader "CQF/DomainTransition"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "white" {}
        _NoiseTex ("_NoiseTex", 2D) = "gray" {}
        _SurfaceTex ("_SurfaceTex", 2D) = "white" {}
        _Color ("_Color", Vector) = (.06,.06,.06,1)
        _SecondaryColor ("_SecondaryColor", Vector) = (.3,.3,.3,1)
        _EdgeColor ("_EdgeColor", Vector) = (1,1,1,1)
        _Flow ("_Flow", Vector) = (.1,-.2,1,.15)
        _Stages ("_Stages", Vector) = (.55,.48,.72,.7)
        _SourceUV ("_SourceUV", Vector) = (0,0,1,1)
        _Origin ("_Origin", Vector) = (.5,.5,1,0)
        _Destination ("_Destination", Vector) = (.5,.5,1,0)
        _Progress ("_Progress", Float) = 0
        _EffectTime ("_EffectTime", Float) = 0
        _Radius ("_Radius", Float) = 12
        _Mode ("_Mode", Float) = 0
        _Diameter ("_Diameter", Float) = 27
        _FoldEnd ("_FoldEnd", Float) = .94
        _Height ("_Height", Float) = 4.5
        _EdgeWidth ("_EdgeWidth", Float) = .12
        _CoverProgress ("_CoverProgress", Float) = .42
        _RevealProgress ("_RevealProgress", Float) = .46
        _RevealEnd ("_RevealEnd", Float) = .95
        _Returning ("_Returning", Float) = 0
        _SourceSaturation ("_SourceSaturation", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex, _NoiseTex, _SurfaceTex;
            float4 _Color;
            float4 _SecondaryColor;
            float4 _EdgeColor;
            float4 _Flow;
            float4 _Stages;
            float4 _SourceUV;
            float4 _Origin;
            float4 _Destination;
            float _Progress;
            float _EffectTime;
            float _Radius;
            float _Mode;
            float _Diameter;
            float _FoldEnd;
            float _Height;
            float _EdgeWidth;
            float _CoverProgress;
            float _RevealProgress;
            float _RevealEnd;
            float _Returning;
            float _SourceSaturation;
float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453); }
float noise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(hash(i),hash(i + float2(1,0)),f.x),lerp(hash(i + float2(0,1)),hash(i + float2(1,1)),f.x),f.y);
}
float field(float2 uv)
{
    return noise(uv) * 0.65 + noise(uv * 2.03 + float2(7.4,9.2)) * 0.35;
}

            v2f vert(appdata v)
            {
float4 vertex = float4(v.vertex.xyz,1.0);

                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);
                o.uv = v.uv;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
float progress = clamp(_Progress,0.0,1.0);
float2 origin = _Returning > 0.5 ? _Destination.xy : _Origin.xy;
float aspect = max(0.1,_Origin.z);
float2 screen = float2(i.uv.x,1.0 - i.uv.y);
float2 offset = (screen - origin) * float2(aspect,1.0);
float distance = length(offset);
float extent = length(float2(max(origin.x,1.0 - origin.x) * aspect,max(origin.y,1.0 - origin.y))) + 0.3;
float2 flow = offset * _Flow.z * 3.0 + progress * 4.0 * _Flow.xy;
float n = field(flow) + tex2D(_NoiseTex,flow).r - 0.5;
float edgeWidth = max(0.005,_EdgeWidth * 0.2);
float front = (extent + _Flow.w + edgeWidth * 2.0) * smoothstep(0.0,_CoverProgress,progress);
float cover = (1.0 - smoothstep(front - edgeWidth,front + edgeWidth,distance + (n - 0.5) * _Flow.w)) * smoothstep(0.0,0.02,progress);
float2 centerOffset = (screen - float2(0.5,0.5)) * float2(aspect,1.0);
float revealExtent = length(float2(aspect * 0.5,0.5)) + _Flow.w + edgeWidth * 3.0;
float aperture = lerp(-_Flow.w - edgeWidth * 3.0,revealExtent,smoothstep(_RevealProgress,_RevealEnd,progress));
float reveal = smoothstep(aperture - edgeWidth,aperture + edgeWidth,length(centerOffset) + (n - 0.5) * _Flow.w);
float2 warp = offset * (1.0 + progress * _Flow.w);
warp.x /= aspect;
float2 sourceUV = float2(origin.x + warp.x,1.0 - origin.y - warp.y);
float3 source = tex2D(_MainTex,clamp(sourceUV,float2(0,0),float2(1,1)) * _SourceUV.zw + _SourceUV.xy).rgb;
float luminance = dot(source,float3(0.22,0.707,0.071));
source = clamp(lerp(float3(luminance,luminance,luminance),source,_SourceSaturation),0.0,1.0);
float3 surface = tex2D(_SurfaceTex,flow + float2(n,n) * _Flow.w).rgb * lerp(_Color.rgb,_SecondaryColor.rgb,clamp(n,0.0,1.0));
float rim = exp(-abs(distance - front) / edgeWidth) + exp(-abs(length(centerOffset) - aperture) / edgeWidth);
surface += rim * _EdgeColor.rgb * _EdgeColor.a;
float3 color = lerp(source,surface,smoothstep(0.0,_CoverProgress * 0.8,progress));
return float4(color,cover * reveal);

            }
            ENDCG
        }
    }
}
