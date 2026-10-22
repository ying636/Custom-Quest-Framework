float domainContactFront(vec2 point, vec3 first, vec3 second)
{
    float a = length(point-first.xy)-first.z;
    float b = length(point-second.xy)-second.z;
    float depth = abs(first.z-second.z)-length(first.xy-second.xy);
    vec3 inner = first.z < second.z ? first : second;
    float ring = (length(point-inner.xy)-inner.z*0.8)*(first.z < second.z ? 1.0 : -1.0);
    float nested = smoothstep(0.0,max(0.2,min(1.0,inner.z*0.2)),depth);
    return mix(a-b,ring,nested);
}