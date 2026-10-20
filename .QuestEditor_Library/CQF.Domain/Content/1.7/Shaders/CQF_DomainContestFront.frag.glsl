#version 450
#include "CQF_DomainInvasionCommon.glsl"
layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;
layout(location = 2) in vec2 vWorld;
layout(location = 0) out vec4 outColor;
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
    vec4 _FrontState;
    vec4 _FrontShape;
    vec4 _FrontHole;
    vec4 _FrontWarp;
} uMaterial;
void main()
{
    float t = uMaterial._FrontState.y;
    float x = vWorld.x-uMaterial._FrontState.x;
    float z = vWorld.y;
    if (uMaterial._FrontHole.z > 0.0 && uMaterial._FrontHole.w > 0.0)
    {
        vec2 local = (vWorld-uMaterial._FrontHole.xy)/uMaterial._FrontHole.zw;
        x = -domainInvasionDistance(vWorld,uMaterial._FrontHole,uMaterial._FrontWarp.x);
        z = atan(local.y,local.x)*sqrt(uMaterial._FrontHole.z*uMaterial._FrontHole.w);
    }
    float recoil = uMaterial._FrontState.w;
    float wave = sin(z*1.1-t*1.7+sin(z*0.43+t)*2.4)*0.38+sin(z*3.9+t*2.3)*0.13
        +recoil*sin(z*0.8-t*8.0)*0.25;
    float front = x+wave;
    float core = exp(-front*front*38.0);
    float streams = pow(max(0.0,sin(z*2.8-t*4.7+sin(z*0.61+t)*2.0+cos(x*11.0-t*2.8)*1.2)),6.0);
    float fingers = exp(-front*front*1.5)*(0.08+streams*0.26);
    float pressure = uMaterial._FrontState.z;
    float push = sin(x*10.0-t*(3.0+abs(pressure)*8.0)*sign(pressure+0.001)+sin(z*0.9+t));
    float wake = pow(max(0.0,push),10.0)*exp(-abs(x)*2.0)*abs(pressure)*0.45;
    float shock = exp(-pow(abs(x)-uMaterial._FrontShape.w*4.0,2.0)*28.0)*abs(recoil)*0.65;
    float sparks = pow(max(0.0,sin(z*13.0+t*17.0)),18.0)*core;
    float envelope = 1.0-smoothstep(uMaterial._FrontShape.x*0.65,uMaterial._FrontShape.x,abs(x));
    vec3 tint = mix(uMaterial._Color.rgb,uMaterial._SecondaryColor.rgb,
        smoothstep(-0.25,0.25,front*(-uMaterial._FrontShape.y)));
    tint = mix(tint,vec3(0.96,0.92,0.86),core*(0.3+sparks*0.5));
    float center = exp(-x*x*200.0)*0.12;
    float alpha = clamp(core*0.18+fingers+wake+shock+sparks*0.45+center,0.0,0.85)*envelope;
    outColor = vec4(tint,alpha);
}
