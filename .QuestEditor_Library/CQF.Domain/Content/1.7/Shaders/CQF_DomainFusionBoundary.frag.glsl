#version 450
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
} uMaterial;
void main()
{
    vec2 point = vWorld;
    float t = uMaterial._SurfaceState.x;
    float firstEdge = length(point-uMaterial._SurfaceCircle.xy)-uMaterial._SurfaceCircle.z;
    float secondEdge = length(point-uMaterial._SurfaceRival.xy)-uMaterial._SurfaceRival.z;
    float wave = sin(dot(point,vec2(0.75,1.13))-t*2.4+sin(point.y*0.65+t)*1.2)*0.55
        +sin(point.y*2.7+point.x*0.35+t*3.2)*0.18;
    float front = firstEdge-secondEdge+wave+uMaterial._SurfaceClash.x;
    float overlap = 1.0-smoothstep(-0.3,0.15,max(firstEdge,secondEdge));
    float seam = exp(-front*front*7.0)*overlap;
    float sparks = pow(max(0.0,sin(point.x*5.3+point.y*7.4-t*12.0)),8.0);
    float rimDistance = min(firstEdge,secondEdge);
    float rim = exp(-rimDistance*rimDistance*24.0);
    vec2 axis = uMaterial._SurfaceRival.xy-uMaterial._SurfaceCircle.xy;
    float axisLength = max(0.001,length(axis));
    vec2 direction = axis/axisLength;
    float along = (axisLength*axisLength+uMaterial._SurfaceCircle.z*uMaterial._SurfaceCircle.z
        -uMaterial._SurfaceRival.z*uMaterial._SurfaceRival.z)/(2.0*axisLength);
    vec2 contact = uMaterial._SurfaceCircle.xy+direction*along;
    float age = uMaterial._SurfaceClash.y;
    float pulseDistance = length(point-contact)-age*9.0;
    float pulse = exp(-pulseDistance*pulseDistance*5.0)*(1.0-smoothstep(0.3,1.6,age));
    pulse *= 1.0-smoothstep(-0.1,0.2,rimDistance);
    vec3 color = mix(uMaterial._Color.rgb,uMaterial._SecondaryColor.rgb,smoothstep(-0.4,0.4,front));
    color = mix(color,vec3(1.0,0.87,0.84),seam*(0.35+sparks*0.45));
    float alpha = clamp(seam*(0.35+sparks*0.6)+rim*0.65+pulse*0.6,0.0,1.0)*uMaterial._SurfaceState.z;
    outColor = vec4(color,alpha);
}
