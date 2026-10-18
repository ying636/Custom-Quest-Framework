#version 450
layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;
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
float progress = clamp(uMaterial._Progress,0.0,1.0);
vec2 origin = uMaterial._Returning > 0.5 ? uMaterial._Destination.xy : uMaterial._Origin.xy;
float aspect = max(0.1,uMaterial._Origin.z);
vec2 screen = vec2(vUv.x,1.0 - vUv.y);
vec2 offset = (screen - origin) * vec2(aspect,1.0);
float distance = length(offset);
float extent = length(vec2(max(origin.x,1.0 - origin.x) * aspect,max(origin.y,1.0 - origin.y))) + 0.3;
vec2 flow = offset * uMaterial._Flow.z * 3.0 + progress * 4.0 * uMaterial._Flow.xy;
float n = field(flow) + texture(_NoiseTex,flow).r - 0.5;
float edgeWidth = max(0.005,uMaterial._EdgeWidth * 0.2);
float front = (extent + uMaterial._Flow.w + edgeWidth * 2.0) * smoothstep(0.0,uMaterial._CoverProgress,progress);
float cover = (1.0 - smoothstep(front - edgeWidth,front + edgeWidth,distance + (n - 0.5) * uMaterial._Flow.w)) * smoothstep(0.0,0.02,progress);
vec2 centerOffset = (screen - vec2(0.5,0.5)) * vec2(aspect,1.0);
float revealExtent = length(vec2(aspect * 0.5,0.5)) + uMaterial._Flow.w + edgeWidth * 3.0;
float aperture = mix(-uMaterial._Flow.w - edgeWidth * 3.0,revealExtent,smoothstep(uMaterial._RevealProgress,uMaterial._RevealEnd,progress));
float reveal = smoothstep(aperture - edgeWidth,aperture + edgeWidth,length(centerOffset) + (n - 0.5) * uMaterial._Flow.w);
vec2 warp = offset * (1.0 + progress * uMaterial._Flow.w);
warp.x /= aspect;
vec2 sourceUV = vec2(origin.x + warp.x,1.0 - origin.y - warp.y);
vec3 source = texture(_MainTex,clamp(sourceUV,vec2(0,0),vec2(1,1)) * uMaterial._SourceUV.zw + uMaterial._SourceUV.xy).rgb;
source = clamp(mix(vec3(dot(source,vec3(0.22,0.707,0.071))),source,uMaterial._SourceSaturation),0.0,1.0);
vec3 surface = texture(_SurfaceTex,flow + vec2(n,n) * uMaterial._Flow.w).rgb * mix(uMaterial._Color.rgb,uMaterial._SecondaryColor.rgb,clamp(n,0.0,1.0));
float rim = exp(-abs(distance - front) / edgeWidth) + exp(-abs(length(centerOffset) - aperture) / edgeWidth);
surface += rim * uMaterial._EdgeColor.rgb * uMaterial._EdgeColor.a;
vec3 color = mix(source,surface,smoothstep(0.0,uMaterial._CoverProgress * 0.8,progress));
return vec4(color,cover * reveal);
}
void main() { outColor = effect(); }
