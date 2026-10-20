#include "CQF_DomainInvasionCommon.glsl"
layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;
    vec4 _SecondaryColor;
    vec4 _Flow;
    vec4 _SurfaceState;
    vec4 _SurfaceCircle;
    vec4 _SurfaceRival;
    vec4 _SurfaceView;
    vec4 _SurfaceReveal;
    vec4 _SurfaceClash;
    float _EffectTime;
    vec4 _TerritoryState;
    vec4 _TerritoryHole0;
    vec4 _TerritoryHole1;
    vec4 _TerritoryHole2;
    vec4 _TerritoryWarp;
    vec4 _TerritoryProtectionState;
} uMaterial;
layout(set = 2, binding = 1) uniform sampler2D _TerritoryProtection;
float domainTerritoryMask(vec2 point)
{
    float distance = (point.x-uMaterial._TerritoryState.x)*uMaterial._TerritoryState.y;
    vec4 holes[3] = vec4[3](uMaterial._TerritoryHole0,uMaterial._TerritoryHole1,uMaterial._TerritoryHole2);
    for (int i=0;i<3;i++)
    {
        vec4 hole = holes[i];
        if (hole.z <= 0.0 || hole.w <= 0.0) continue;
        float invasion = domainInvasionDistance(point,hole,uMaterial._TerritoryWarp[i]);
        distance = uMaterial._TerritoryState.z > 0.5 ? max(distance,invasion) : min(distance,-invasion);
    }
    float mask = smoothstep(-0.2,0.2,distance);
    if (uMaterial._TerritoryProtectionState.w > 0.5
        && all(greaterThanEqual(point,vec2(0.0))) && all(lessThan(point,uMaterial._TerritoryProtectionState.xy)))
    {
        vec4 protection = texture(_TerritoryProtection,point/uMaterial._TerritoryProtectionState.xy);
        if (protection.g > 0.5) mask = 1.0-abs(protection.r-uMaterial._TerritoryProtectionState.z);
    }
    return mask;
}
float hash(vec2 p)
{
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3,p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}
float noise(vec2 p)
{
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
float field(vec2 p) { return noise(p)*0.57+noise(p*2.03+17.4)*0.28+noise(p*4.07-8.2)*0.15; }
vec4 domainSurface(vec2 point)
{
    float t = uMaterial._EffectTime;
    vec2 flow = point * uMaterial._Flow.z * 0.28 + t * uMaterial._Flow.xy;
    vec2 warp = vec2(field(flow*1.7+vec2(0,t*0.08)),field(flow*1.7+vec2(9.2,-t*0.06)));
    float liquid = field(flow*2.6+warp*3.4);
    float streams = 1.0-smoothstep(0.025,0.14,abs(liquid-0.5));
    float sheen = pow(max(0.0,1.0-abs(liquid-0.59)*19.0),3.0);
    vec3 color = mix(uMaterial._Color.rgb,uMaterial._SecondaryColor.rgb,smoothstep(0.2,0.8,liquid));
    color += uMaterial._SecondaryColor.rgb*(streams*0.16+sheen*0.2);
    float alpha = uMaterial._Color.a;
    if (uMaterial._SurfaceState.w > 0.5)
    {
        float ownEdge = length(point-uMaterial._SurfaceCircle.xy)-uMaterial._SurfaceCircle.z;
        if (uMaterial._SurfaceClash.w > 1.5)
        {
            float time = uMaterial._SurfaceState.x;
            float wave = sin(point.y*1.1-time*1.7+sin(point.y*0.43+time)*2.4)*0.38
                +sin(point.y*3.9+time*2.3)*0.13+uMaterial._SurfaceClash.y*sin(point.y*0.8-time*8.0)*0.25;
            float front = (point.x-uMaterial._SurfaceCircle.x+wave)*uMaterial._SurfaceCircle.y;
            float envelope = 1.0-smoothstep(uMaterial._SurfaceCircle.z*0.55,uMaterial._SurfaceCircle.z,abs(point.x-uMaterial._SurfaceCircle.x));
            alpha *= smoothstep(-0.18,0.18,front)*envelope;
        }
        if (uMaterial._SurfaceCircle.w > 0.5) alpha *= 1.0-smoothstep(-0.12,0.12,ownEdge);
        if (uMaterial._SurfaceRival.w > 1.5 && uMaterial._SurfaceRival.z > 0.001)
        {
            float otherEdge = length(point-uMaterial._SurfaceRival.xy)-uMaterial._SurfaceRival.z;
            vec2 invasionFlow = point*0.42+vec2(uMaterial._SurfaceState.x*0.18,-uMaterial._SurfaceState.x*0.14);
            vec2 invasionWarp = vec2(field(invasionFlow+7.4),field(invasionFlow-9.2));
            float invasion = (otherEdge-ownEdge)*0.15+(field(invasionFlow*1.6+invasionWarp*2.6)-0.5)*1.4;
            if (uMaterial._SurfaceClash.w > 0.5)
            {
                float wave = sin(dot(point,vec2(0.75,1.13))-uMaterial._SurfaceState.x*2.4+sin(point.y*0.65+uMaterial._SurfaceState.x)*1.2)*0.55
                    +sin(point.y*2.7+point.x*0.35+uMaterial._SurfaceState.x*3.2)*0.18;
                invasion = (otherEdge-ownEdge+wave+uMaterial._SurfaceClash.x)*0.7;
            }
            float mask = smoothstep(-0.12,0.12,invasion);
            alpha *= mix(1.0,mask,1.0-smoothstep(-0.25,0.75,otherEdge));
        }
        if (uMaterial._SurfaceReveal.w > 0.5 && uMaterial._SurfaceReveal.w < 1.5)
            alpha *= smoothstep(-0.2,0.2,length(point-uMaterial._SurfaceReveal.xy)-uMaterial._SurfaceReveal.z);
        alpha *= uMaterial._SurfaceState.z;
    }
    return vec4(color,alpha);
}
