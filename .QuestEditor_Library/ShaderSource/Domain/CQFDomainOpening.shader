Shader "CQF/DomainOpening"
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
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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
if (_Mode > 0.5)
{
    float rise = smoothstep(_Stages.y,_Stages.z,_Progress);
    float fold = smoothstep(_Stages.w,_FoldEnd,_Progress);
    float curve = v.uv.y * 1.570796327;
    float ripple = sin(v.uv.x * 9.0 - _EffectTime * 4.0) * _Flow.w * (1.0 - fold);
    float height = rise * (_Height + ripple);
    vertex.xz *= lerp(1.0,cos(curve),fold);
    vertex.z += height * lerp(v.uv.y,sin(curve),fold);
    vertex.y += v.uv.y * (0.02 + 0.1 * fold);
    vertex.y += lerp(0.05,-0.03,smoothstep(-0.15,0.15,sin(v.uv.x)));
}

                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);
                o.uv = v.uv;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
float2 flow;
float edge;
float alpha;
if (_Mode < 0.5)
{
    float2 p = (i.uv - float2(0.5,0.5)) * _Diameter;
    flow = p * _Flow.z * 0.2 + _EffectTime * _Flow.xy;
    float turbulence = field(flow * 3.0) + tex2D(_NoiseTex,flow).r - 1.0;
    edge = _Radius * smoothstep(0.0,_Stages.x,_Progress) - length(p) + turbulence * _Flow.w;
    alpha = smoothstep(-_EdgeWidth,_EdgeWidth,edge) * smoothstep(0.0,0.035,_Progress);
}
else
{
    float fold = smoothstep(_Stages.w,_FoldEnd,_Progress);
    float longitude = i.uv.x * _Radius * 0.15;
    flow = float2(longitude,i.uv.y * 2.0) * _Flow.z + _EffectTime * _Flow.xy;
    flow.x *= 1.0 - fold * smoothstep(0.9,1.0,i.uv.y);
    float turbulence = field(flow * 3.0) + tex2D(_NoiseTex,flow).r - 1.0;
    float top = lerp(0.92 + turbulence * 0.08,1.08,fold);
    edge = top - i.uv.y;
    alpha = smoothstep(0.0,0.04,edge) * smoothstep(_Stages.y,_Stages.z,_Progress);
}
float n = field(flow * 3.0);
float4 surface = tex2D(_MainTex,flow + float2(n,n) * _Flow.w);
float3 color = lerp(_Color.rgb,_SecondaryColor.rgb,n) * surface.rgb;
float rim = exp(-abs(edge) / max(0.001,_EdgeWidth));
color += rim * _EdgeColor.rgb * _EdgeColor.a;
return float4(color,alpha * surface.a * _Color.a);

            }
            ENDCG
        }
    }
}
