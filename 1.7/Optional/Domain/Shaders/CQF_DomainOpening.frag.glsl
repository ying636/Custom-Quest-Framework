#version 450
layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;
layout(location = 2) in vec2 vWorld;
layout(location = 0) out vec4 outColor;
layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;
    vec4 _SecondaryColor;
    vec4 _EdgeColor;
    vec4 _Flow;
    vec4 _Stages;
    vec4 _SourceUV;
    vec4 _Origin;
    vec4 _Destination;
    vec4 _FusionCircle;
    vec4 _FusionRival;
    vec4 _FusionState;
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
} uMaterial;
layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _NoiseTex;
layout(set = 2, binding = 3) uniform sampler2D _SurfaceTex;
float hash(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7))) * 43758.5453); }
float noise(vec2 p)
{
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i),hash(i + vec2(1,0)),f.x),mix(hash(i + vec2(0,1)),hash(i + vec2(1,1)),f.x),f.y);
}
float field(vec2 uv)
{
    return noise(uv) * 0.65 + noise(uv * 2.03 + vec2(7.4,9.2)) * 0.35;
}
vec4 effect()
{
vec2 flow;
float edge;
float alpha;
if (uMaterial._Mode < 0.5)
{
    vec2 p = (vUv - vec2(0.5,0.5)) * uMaterial._Diameter;
    flow = p * uMaterial._Flow.z * 0.2 + uMaterial._EffectTime * uMaterial._Flow.xy;
    float turbulence = field(flow * 3.0) + texture(_NoiseTex,flow).r - 1.0;
    edge = uMaterial._Radius * smoothstep(0.0,uMaterial._Stages.x,uMaterial._Progress) - length(p) + turbulence * uMaterial._Flow.w;
    alpha = smoothstep(-uMaterial._EdgeWidth,uMaterial._EdgeWidth,edge) * smoothstep(0.0,0.035,uMaterial._Progress);
}
else
{
    float fold = smoothstep(uMaterial._Stages.w,uMaterial._FoldEnd,uMaterial._Progress);
    float longitude = vUv.x * uMaterial._Radius * 0.15;
    flow = vec2(longitude,vUv.y * 2.0) * uMaterial._Flow.z + uMaterial._EffectTime * uMaterial._Flow.xy;
    flow.x *= 1.0 - fold * smoothstep(0.9,1.0,vUv.y);
    float turbulence = field(flow * 3.0) + texture(_NoiseTex,flow).r - 1.0;
    float top = mix(0.92 + turbulence * 0.08,1.08,fold);
    edge = top - vUv.y;
    alpha = smoothstep(0.0,0.04,edge) * smoothstep(uMaterial._Stages.y,uMaterial._Stages.z,uMaterial._Progress);
}
float n = field(flow * 3.0);
vec4 surface = texture(_MainTex,flow + vec2(n,n) * uMaterial._Flow.w);
vec3 color = mix(uMaterial._Color.rgb,uMaterial._SecondaryColor.rgb,n) * surface.rgb;
float rim = exp(-abs(edge) / max(0.001,uMaterial._EdgeWidth));
color += rim * uMaterial._EdgeColor.rgb * uMaterial._EdgeColor.a;
return vec4(color,alpha * surface.a * uMaterial._Color.a);
}
#include "CQF_DomainDomeFusion.glsl"
void main() { outColor = fusedDome(effect(),vWorld); }
