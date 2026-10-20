#version 450
layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 2) in vec4 inColor; // @vertex Color optional @default = vec4(1.0, 1.0, 1.0, 1.0)
layout(location = 3) in vec4 inUv0; // @vertex TexCoord0 optional @default = vec4(0.0, 0.0, 0.0, 0.0)
layout(push_constant) uniform Push { mat4 modelMatrix; } push;
layout(set = 0, binding = 1) uniform FrameUniforms { vec4 Time; mat4 WorldToCamera; mat4 ViewProj; } uVertex;
layout(location = 0) out vec4 vColor;
layout(location = 1) out vec2 vUv;
layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;
    vec4 _SecondaryColor;
    vec4 _EdgeColor;
    vec4 _Flow;
    vec4 _Stages;
    vec4 _SourceUV;
    vec4 _Origin;
    vec4 _Destination;
    float _Progress;
    float _EffectTime;
    float _Radius;
    float _Mode;
    float _Diameter;
    float _FoldEnd;
    float _Height;
    float _EdgeWidth;
    float _CoverProgress;
    float _RevealProgress;
    float _RevealEnd;
    float _Returning;
    float _SourceSaturation;
} uMaterial;
layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _NoiseTex;
layout(set = 2, binding = 3) uniform sampler2D _SurfaceTex;
void main()
{
vec4 vertex = vec4(inPosition,1.0);
    gl_Position = uVertex.ViewProj * push.modelMatrix * vertex;
    vColor = inColor;
    vUv = inUv0.xy;
}
