#include "CQF_DomainContact.glsl"
vec4 fusedDome(vec4 color,vec2 point)
{
    if (uMaterial._FusionState.w < 0.5) return color;
    float ownEdge = length(point-uMaterial._FusionCircle.xy)-uMaterial._FusionCircle.z;
    float otherEdge = length(point-uMaterial._FusionRival.xy)-uMaterial._FusionRival.z;
    float t = uMaterial._FusionState.x;
    float wave = sin(dot(point,vec2(0.75,1.13))-t*2.4+sin(point.y*0.65+t)*1.2)*0.55
        +sin(point.y*2.7+point.x*0.35+t*3.2)*0.18;
    bool second = uMaterial._FusionCircle.w > 1.5;
    float front = (second ? domainContactFront(point,uMaterial._FusionRival.xyz,uMaterial._FusionCircle.xyz) : domainContactFront(point,uMaterial._FusionCircle.xyz,uMaterial._FusionRival.xyz))+wave+uMaterial._FusionState.z;
    float mask = smoothstep(-0.17,0.17,front);
    if (!second) mask = 1.0-mask;
    float overlap = (1.0-smoothstep(-0.25,0.35,otherEdge))*uMaterial._FusionState.y;
    color.a *= mix(1.0,mask,overlap);
    float glow = exp(-front*front*7.0)*overlap;
    color.rgb += vec3(0.65,0.38,0.34)*glow*(0.35+0.4*pow(max(0.0,sin(point.x*5.3+point.y*7.4-t*12.0)),8.0));
    return color;
}
