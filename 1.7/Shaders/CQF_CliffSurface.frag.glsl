#version 450
layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vCliff;
layout(location = 2) in vec2 vWorld;

layout(location = 0) out vec4 outColor;
layout(set = 2, binding = 0) uniform MaterialUniforms { vec4 _Color; } surface;
layout(set = 2, binding = 1) uniform sampler2D _MainTex;

float hashRock(ivec2 cell)
{
    uvec2 key = uvec2(cell);
    uint hash = (key.x * 2246822519u) ^ (key.y * 3266489917u);
    hash = (hash ^ (hash >> 16)) * 2146121005u;
    hash = (hash ^ (hash >> 15)) * 2221713035u;
    return float((hash ^ (hash >> 16)) >> 9) / 8388608.0;
}

float rockRelief(vec2 point)
{
    ivec2 cell = ivec2(floor(point));
    vec2 local = fract(point);
    vec2 nearestFace = vec2(0.0);
    float nearest = 8.0;
    float second = 8.0;
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            ivec2 offset = ivec2(x, y);
            ivec2 face = cell + offset;
            vec2 center = vec2(hashRock(face), hashRock(face + ivec2(113, 67)));
            vec2 delta = vec2(offset) + .13 + .74 * center - local;
            float distanceToFace = dot(delta, delta);
            if (distanceToFace < nearest)
            {
                nearestFace = delta;
                second = nearest;
                nearest = distanceToFace;
            }
            else
            {
                second = min(second, distanceToFace);
            }
        }
    }
    float facet = clamp(.71 + .51 * nearestFace.x - .38 * nearestFace.y, .42, 1.0);
    float joint = smoothstep(.02, .16, second - nearest);
    return facet * mix(.54, 1.0, joint);
}

void main()
{
    float descent = vCliff.x;
    float across = dot(vWorld, vec2(.83, .56));
    float offset = dot(vWorld, vec2(-.56, .83));
    vec2 rockPoint = vec2(across * .68 + descent * .16, descent * .55 + offset * .16);
    vec2 grainUV = rockPoint * vec2(.89, 1.38);
    vec2 grainDx = dFdx(grainUV);
    vec2 grainDy = dFdy(grainUV);
    vec2 shoreUV = vWorld * .62;
    vec2 shoreDx = dFdx(shoreUV);
    vec2 shoreDy = dFdy(shoreUV);
    float opacity = vColor.a * surface._Color.a;
    if (opacity <= .002) discard;

    vec3 shade = vec3(0.0);
    if (vCliff.y < 0.0)
    {
        shade = vec3(.14 + .31 * (1.0 - smoothstep(0.0, .80, descent)));
    }
    else if (descent < 3.5 || vCliff.y > .001)
    {
        float chips = textureGrad(_MainTex, fract(shoreUV), shoreDx, shoreDy).r;
        vec3 lip = vColor.rgb * (vec3(.316, .288, .257) + vec3(.084, .039, .014) * vCliff.y) * (.72 + .52 * chips);
        if (descent < 0.0)
        {
            opacity *= .46 * (1.0 - smoothstep(.03, mix(.46, .80, chips), -descent));
            shade = lip;
        }
        else
        {
            float grain = textureGrad(_MainTex, fract(grainUV), grainDx, grainDy).r;
            float relief = rockRelief(rockPoint + (grain - .5) * vec2(.27, .16));
            float contact = mix(.64, 1.0, smoothstep(.40, 1.15, descent));
            float light = exp2(-1.24 * descent) * (1.0 - smoothstep(1.3, 3.5, descent));
            vec3 ambient = vec3(.185, .173, .158) * light;
            vec3 torch = vec3(.53, .235, .074) * vCliff.y * (1.0 - smoothstep(2.55, 5.2, descent));
            vec3 rock = vColor.rgb * (ambient + torch) * (.68 + .56 * grain) * relief * contact;
            float shoulder = 1.0 - smoothstep(.08, mix(.35, .65, chips), descent);
            shade = mix(rock, lip, shoulder);
        }
    }
    outColor = vec4(shade * surface._Color.rgb, opacity);
}
