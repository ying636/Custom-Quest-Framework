#version 450
layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;
layout(location = 2) in vec2 vWorld;
layout(location = 0) out vec4 outColor;
#include "CQF_DomainSurfaceCommon.glsl"
void main() { outColor = domainSurface(vWorld) * vColor; }
