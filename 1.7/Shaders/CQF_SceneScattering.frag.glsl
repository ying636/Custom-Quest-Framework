#version 450
layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;
layout(location = 2) in vec3 vWorld;
layout(location = 0) out vec4 outColor;
layout(set = 2, binding = 0) uniform MaterialUniforms {
    #include "CQF_SceneLightingUniforms.glsl"
    vec4 _CQFScatterBounds;
} uMaterial;
#include "CQF_SceneLighting.glsl"
float hash(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise(vec2 p)
{
    vec2 i=floor(p),f=fract(p); f=f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
void main()
{
    vec2 plane=uMaterial._CQFScatterBounds.xy+vUv*uMaterial._CQFScatterBounds.zw;
    float height=uMaterial._CQFBeamTime.y;
    vec3 p=vec3(plane.x,height,plane.y-height*uMaterial._CQFBeamTime.z);
    float band=cqfSceneBeamMask(p);
    if(band<=0.0) { outColor=vec4(uMaterial._CQFBeamColor.rgb,0.0); return; }
    float time=uMaterial._CQFBeamTime.x;
    float visible=cqfSceneBeamVisibility(p,vec3(0,1,0));
    float density=.65+.35*noise(plane*.12+vec2(time*.012,-time*.007));
    float across=dot(vec4(p,1.0),uMaterial._CQFBeamPlane);
    float filaments=.65+.35*noise(vec2(across*.8,time*.04));
    vec3 photon=normalize(p-uMaterial._CQFBeamOrigin.xyz);
    vec3 view=normalize(vec3(0,1,-uMaterial._CQFBeamTime.z));
    float g=.35,cosine=dot(photon,view);
    float phase=(1.0-g*g)/pow(max(.05,1.0+g*g-2.0*g*cosine),1.5);
    float scattering=band*visible*density*filaments*clamp(phase,.35,1.5);
    float alpha=scattering*uMaterial._CQFBeamColor.a*uMaterial._CQFBeamShape.z;
    alpha+=(hash(plane*vec2(3.2,5.7))-.5)/255.0;
    outColor=vec4(uMaterial._CQFBeamColor.rgb,clamp(alpha,0.0,.12));
}
