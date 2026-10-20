#version 450
layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;
layout(location = 0) out vec4 outColor;
#include "CQF_DomainSurfaceCommon.glsl"
void main()
{
    vec2 point = vec2(vUv.x,1.0-vUv.y) * uMaterial._SurfaceView.zw + uMaterial._SurfaceView.xy;
    outColor = domainSurface(point);
    if (uMaterial._SurfaceReveal.w > 1.5)
    {
        vec2 borderPoint = vec2(vUv.x*uMaterial._SurfaceReveal.x,vUv.y)*22.0
            +vec2(uMaterial._EffectTime*0.35,-uMaterial._EffectTime*0.22);
        outColor = domainSurface(borderPoint);
        vec2 edge = min(vUv,1.0-vUv);
        float distance = min(edge.x*uMaterial._SurfaceReveal.x,edge.y);
        float ripple = field(vec2(vUv.x*uMaterial._SurfaceReveal.x,vUv.y)*13.0
            +vec2(uMaterial._EffectTime*0.13,-uMaterial._EffectTime*0.11));
        float width = uMaterial._SurfaceReveal.y*(0.65+ripple*0.65);
        outColor.rgb *= mix(0.48,0.85,smoothstep(0.0,width,distance));
        outColor.a *= 1.0-smoothstep(width*0.12,width,distance);
        if (uMaterial._TerritoryState.w > 0.5)
            outColor.a *= domainTerritoryMask(point);
        else if (abs(uMaterial._SurfaceCircle.y) > 0.5)
            outColor.a *= smoothstep(-0.35,0.35,(point.x-uMaterial._SurfaceCircle.x)*uMaterial._SurfaceCircle.y);
    }
}
