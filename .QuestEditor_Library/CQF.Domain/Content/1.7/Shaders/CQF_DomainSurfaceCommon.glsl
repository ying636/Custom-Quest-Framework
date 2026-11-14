#include "CQF_DomainContact.glsl"
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
    vec4 _TerritoryProtectionState;
    float _Dissolve;
} uMaterial;
layout(set = 2, binding = 1) uniform sampler2D _TerritoryProtection;
float domainTerritoryMask(vec2 point)
{
    if (uMaterial._TerritoryProtectionState.w > 0.5
        && (any(lessThan(point,vec2(0.0))) || any(greaterThanEqual(point,uMaterial._TerritoryProtectionState.xy)))) return 0.0;
    float distance = (point.x-uMaterial._TerritoryState.x)*uMaterial._TerritoryState.y;
    float mask = smoothstep(-0.2,0.2,distance);
    if (uMaterial._TerritoryProtectionState.w > 0.5
        && all(greaterThanEqual(point,vec2(0.0))) && all(lessThan(point,uMaterial._TerritoryProtectionState.xy)))
    {
        ivec2 cell = clamp(ivec2(floor(point)),ivec2(0),ivec2(uMaterial._TerritoryProtectionState.xy)-1);
        vec4 protection = texelFetch(_TerritoryProtection,cell,0);
        if (protection.g > 0.5) mask = 1.0-abs(protection.r-uMaterial._TerritoryProtectionState.z);
        else if (protection.a > 0.5)
        {
            float front = texture(_TerritoryProtection,point/uMaterial._TerritoryProtectionState.xy).b*16.0-8.0;
            mask = smoothstep(-0.2,0.2,front*(uMaterial._TerritoryProtectionState.z > 0.5 ? 1.0 : -1.0));
        }
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
    if (uMaterial._SurfaceState.y > 0.5 && uMaterial._SurfaceClash.w > 2.5 && uMaterial._SurfaceClash.w < 3.5) alpha = 1.0;
    if (uMaterial._SurfaceState.w > 0.5)
    {
        float ownEdge = length(point-uMaterial._SurfaceCircle.xy)-uMaterial._SurfaceCircle.z;
        if (uMaterial._SurfaceClash.w > 4.5)
        {
            float time = uMaterial._SurfaceState.x;
            float distance = texture(_TerritoryProtection,point/uMaterial._TerritoryProtectionState.xy).b*16.0-8.0;
            float wave = sin(point.y*1.1-time*1.7+sin(point.y*0.43+time)*2.4)*0.38
                +sin(point.y*3.9+time*2.3)*0.13+uMaterial._SurfaceClash.y*sin(point.y*0.8-time*8.0)*0.25;
            float direction = uMaterial._TerritoryProtectionState.z > 0.5 ? 1.0 : -1.0;
            float front = (distance+wave)*direction;
            float envelope = 1.0-smoothstep(uMaterial._SurfaceCircle.z*0.55,uMaterial._SurfaceCircle.z,abs(distance));
            alpha *= smoothstep(-0.18,0.18,front)*envelope;
        }
        else if (uMaterial._SurfaceClash.w > 3.5)
        {
            float time = uMaterial._SurfaceState.x;
            vec2 flow = point*0.28+vec2(time*0.16,-time*0.11);
            vec2 warp = vec2(field(flow+7.4),field(flow-9.2));
            float erosion = (field(flow*1.7+warp*3.0)-0.5)*4.0
                +sin(point.x*0.7+point.y*1.1-time*2.2)*0.45;
            float wave = sin(point.y*1.1-time*1.7+sin(point.y*0.43+time)*2.4)*0.38
                +sin(point.y*3.9+time*2.3)*0.13;
            float line = (point.x-uMaterial._SurfaceRival.x+wave)*uMaterial._SurfaceRival.y;
            float front = mix(ownEdge+erosion,line,clamp(uMaterial._SurfaceClash.x,0.0,1.0));
            alpha *= 1.0-smoothstep(-0.3,0.3,front);
        }
        else if (uMaterial._SurfaceClash.w > 2.5)
        {
            float time = uMaterial._SurfaceState.x;
            vec2 flow = point*0.28+vec2(time*0.16,-time*0.11);
            vec2 warp = vec2(field(flow+7.4),field(flow-9.2));
            float erosion = (field(flow*1.7+warp*3.0)-0.5)*4.0
                +sin(point.x*0.7+point.y*1.1-time*2.2)*0.45;
            alpha *= 1.0-smoothstep(-0.3,0.3,ownEdge+erosion);
        }
        else if (uMaterial._SurfaceClash.w > 1.5)
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
                invasion = (domainContactFront(point,uMaterial._SurfaceRival.xyz,uMaterial._SurfaceCircle.xyz)+wave+uMaterial._SurfaceClash.x)*0.7;
            }
            float mask = smoothstep(-0.12,0.12,invasion);
            alpha *= mix(1.0,mask,1.0-smoothstep(-0.25,0.75,otherEdge));
        }
        if (uMaterial._SurfaceReveal.w > 0.5 && uMaterial._SurfaceReveal.w < 1.5)
            alpha *= smoothstep(-0.2,0.2,length(point-uMaterial._SurfaceReveal.xy)-uMaterial._SurfaceReveal.z);
        alpha *= uMaterial._SurfaceState.z;
    }
    if (uMaterial._Dissolve > 0.0)
    {
        float erosion = field(point*0.72+vec2(13.2,7.1))*0.78+noise(point*3.1)*0.22;
        float remaining = erosion-mix(-0.08,1.08,uMaterial._Dissolve);
        float mask = smoothstep(0.0,0.035,remaining);
        if (mask <= 0.001) discard;
        color += uMaterial._SecondaryColor.rgb*(1.0-smoothstep(0.02,0.11,remaining))*mask*1.8;
        alpha *= mask;
    }
    return vec4(color,alpha);
}
