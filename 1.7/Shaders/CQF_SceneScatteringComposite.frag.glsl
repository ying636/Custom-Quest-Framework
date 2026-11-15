#version 450
layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;
layout(location = 2) in vec3 vWorld;
layout(location = 0) out vec4 outColor;
layout(set = 2, binding = 0) uniform MaterialUniforms {
    #include "CQF_SceneLightingUniforms.glsl"
} uMaterial;
layout(set = 2, binding = 1) uniform sampler2D _MainTex;
float hash(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
void main()
{
    vec4 fog=texture(_MainTex,vUv);
    if(fog.a<=.00005) { outColor=vec4(fog.rgb,0.0); return; }
    float time=uMaterial._CQFBeamTime.x;
    vec2 p=vWorld.xz*2.4+vec2(time*.025,-time*.055);
    vec2 cell=floor(p);
    float seed=hash(cell);
    vec2 center=vec2(hash(cell+13.4),hash(cell+48.7));
    float dust=(1.0-smoothstep(.025,.09,length(fract(p)-center)))*step(.988,seed);
    dust*=.7+.3*sin(time*.8+seed*100.0);
    float visibility=clamp(fog.a/max(.0001,uMaterial._CQFBeamColor.a),0.0,1.0);
    float alpha=fog.a+dust*visibility*uMaterial._CQFBeamParameters.z;
    outColor=vec4(fog.rgb,clamp(alpha,0.0,.15));
}
