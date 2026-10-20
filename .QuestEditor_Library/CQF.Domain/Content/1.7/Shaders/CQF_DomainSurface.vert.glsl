#version 450
layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 2) in vec4 inColor; // @vertex Color optional @default = vec4(1.0, 1.0, 1.0, 1.0)
layout(location = 3) in vec4 inUv0; // @vertex TexCoord0 optional @default = vec4(0.0, 0.0, 0.0, 0.0)
layout(push_constant) uniform Push { mat4 modelMatrix; } push;
layout(set = 0, binding = 0) uniform VertexUniforms { mat4 ViewProj; } uVertex;
layout(location = 0) out vec4 vColor;
layout(location = 1) out vec2 vUv;
layout(location = 2) out vec2 vWorld;
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
    vec4 world = push.modelMatrix * vec4(inPosition,1.0);
    gl_Position = uVertex.ViewProj * world;
    vColor = inColor;
    vUv = inUv0.xy;
    vWorld = uMaterial._SurfaceState.y > 0.5
        ? vec2(inUv0.x,1.0-inUv0.y) * uMaterial._SurfaceView.zw + uMaterial._SurfaceView.xy : world.xz;
}
