#version 450
layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;
layout(location = 0) out vec4 outColor;
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
    vec3 source = texture(_MainTex,vUv*uMaterial._SourceUV.zw+uMaterial._SourceUV.xy).rgb;
    source = clamp(mix(vec3(dot(source,vec3(0.22,0.707,0.071))),source,uMaterial._SourceSaturation),0.0,1.0);
    outColor = vec4(source,1.0-smoothstep(0.42,0.46,uMaterial._Progress));
    if (uMaterial._Mode > 0.5)
    {
        float cover = smoothstep(0.0,0.42,uMaterial._Progress)*(1.0-smoothstep(0.46,0.95,uMaterial._Progress));
        outColor = vec4(mix(source,vec3(0.025,0.027,0.03),cover),uMaterial._Progress < 0.42 ? 1.0 : cover);
    }
}
