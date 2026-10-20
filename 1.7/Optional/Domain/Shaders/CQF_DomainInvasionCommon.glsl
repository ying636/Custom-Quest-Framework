float domainInvasionRadius(float angle, float phase)
{
    return 0.76+0.16*sin(angle*3.0+phase)+0.08*sin(angle*7.0-phase*1.7);
}
float domainInvasionDistance(vec2 point, vec4 hole, float phase)
{
    vec2 local = (point-hole.xy)/hole.zw;
    float radius = length(local);
    float angle = radius > 0.0 ? atan(local.y,local.x) : 0.0;
    if (angle < 0.0) angle += 6.28318530718;
    float step = 6.28318530718/48.0;
    float start = floor(angle/step)*step;
    vec2 a = vec2(cos(start),sin(start))*domainInvasionRadius(start,phase);
    vec2 b = vec2(cos(start+step),sin(start+step))*domainInvasionRadius(start+step,phase);
    vec2 direction = radius > 0.0 ? local/radius : vec2(1.0,0.0);
    float edge = (a.x*b.y-a.y*b.x)/(direction.x*(b.y-a.y)-direction.y*(b.x-a.x));
    return (edge-radius)*min(hole.z,hole.w);
}
