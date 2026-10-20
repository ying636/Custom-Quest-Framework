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
    vec4 _EdgeColor;
    vec4 _Flow;
    vec4 _Stages;
    vec4 _SourceUV;
    vec4 _Origin;
    vec4 _Destination;
    vec4 _FusionCircle;
    vec4 _FusionRival;
    vec4 _FusionState;
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
if (uMaterial._Mode > 0.5)
{
    float rise = smoothstep(uMaterial._Stages.y,uMaterial._Stages.z,uMaterial._Progress);
    float fold = smoothstep(uMaterial._Stages.w,uMaterial._FoldEnd,uMaterial._Progress);
    float curve = inUv0.xy.y * 1.570796327;
    float ripple = sin(inUv0.xy.x * 9.0 - uMaterial._EffectTime * 4.0) * uMaterial._Flow.w * (1.0 - fold);
    float height = rise * (uMaterial._Height + ripple);
    vertex.xz *= mix(1.0,cos(curve),fold);
    vertex.z += height * mix(inUv0.xy.y,sin(curve),fold);
    vertex.y += inUv0.xy.y * (0.02 + 0.1 * fold);
    vertex.y += mix(0.05,-0.03,smoothstep(-0.15,0.15,sin(inUv0.xy.x)));
}
    gl_Position = uVertex.ViewProj * push.modelMatrix * vertex;
    vWorld = (push.modelMatrix * vertex).xz;
    vColor = inColor;
    vUv = inUv0.xy;
}
