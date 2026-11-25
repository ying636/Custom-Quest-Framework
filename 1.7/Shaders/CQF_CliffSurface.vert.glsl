#version 450
layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 2) in vec4 inColor; // @vertex Color required
layout(location = 3) in vec4 inUv0; // @vertex TexCoord0 required

layout(push_constant) uniform Push { mat4 modelMatrix; } push;
layout(set = 0, binding = 0) uniform VertexUniforms { mat4 ViewProj; } vertexData;

layout(location = 0) out vec4 vColor;
layout(location = 1) out vec2 vCliff;
layout(location = 2) out vec2 vWorld;

void main()
{
    vec4 world = push.modelMatrix * vec4(inPosition, 1.0);
    gl_Position = vertexData.ViewProj * world;
    vColor = inColor;
    vCliff = inUv0.xy;
    vWorld = world.xz;
}
