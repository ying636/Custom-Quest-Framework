layout(set = 2, binding = 4) uniform sampler2D _CQFShadowAtlas;

float cqfSceneVisibility(vec3 position, vec3 geometryNormal, vec4 positionRange, vec4 parameters)
{
    if(parameters.w<.5) return 1.0;
    vec3 origin=positionRange.xyz;
    vec3 d=position-origin,a=abs(d);
    vec3 q;
    float face;
    if(a.x>=a.y && a.x>=a.z)
    {
        if(d.x>=0.0) { face=0.0; q=vec3(-d.z,d.y,d.x); }
        else { face=1.0; q=vec3(d.z,d.y,-d.x); }
    }
    else if(a.y>=a.z)
    {
        if(d.y>=0.0) { face=2.0; q=vec3(d.x,-d.z,d.y); }
        else { face=3.0; q=vec3(d.x,d.z,-d.y); }
    }
    else
    {
        if(d.z>=0.0) { face=4.0; q=vec3(d.x,d.y,d.z); }
        else { face=5.0; q=vec3(-d.x,d.y,-d.z); }
    }
    float resolution=uMaterial._CQFShadowInfo.z;
    vec2 local=(q.xy/max(q.z,.001)*.5+.5)*resolution;
    vec3 n=face<.5 ? vec3(-geometryNormal.z,geometryNormal.y,geometryNormal.x)
        : face<1.5 ? vec3(geometryNormal.z,geometryNormal.y,-geometryNormal.x)
        : face<2.5 ? vec3(geometryNormal.x,-geometryNormal.z,geometryNormal.y)
        : face<3.5 ? vec3(geometryNormal.x,geometryNormal.z,-geometryNormal.y)
        : face<4.5 ? geometryNormal : vec3(-geometryNormal.x,geometryNormal.y,-geometryNormal.z);
    float kernel=clamp(parameters.y*resolution/max(q.z*2.0,1.0),.6,2.0);
    vec2 base=vec2(face*resolution,parameters.z*resolution);
    float visible=0.0;
    for(int tap=0;tap<5;tap++)
    {
        vec2 offset=tap==0 ? vec2(0) : tap==1 ? vec2(-kernel,-kernel) : tap==2 ? vec2(kernel,-kernel) : tap==3 ? vec2(-kernel,kernel) : vec2(kernel,kernel);
        vec2 samplePosition=clamp(local+offset,vec2(.5),vec2(resolution-.5));
        vec3 ray=vec3(samplePosition/resolution*2.0-1.0,1.0);
        float denominator=dot(n,ray);
        float receiverDepth=abs(denominator)>.001 ? dot(n,q)/denominator : q.z;
        float testDepth=positionRange.w/max(receiverDepth-.18,.01);
        vec2 pixel=base+samplePosition;
        visible+=step(texture(_CQFShadowAtlas,pixel*uMaterial._CQFShadowInfo.xy).r,testDepth);
    }
    return visible*.2;
}

vec3 cqfSceneIllumination(vec3 position, vec3 normal, vec3 geometryNormal)
{
    float sky=max(0.0,dot(normal,uMaterial._CQFSunDirection.xyz));
    vec3 illumination=mix(vec3(.31,.34,.38),vec3(.43,.45,.47),normal.y*.5+.5)+vec3(.54,.52,.47)*sky;
    for(int index=0;index<int(uMaterial._CQFLightCount);index++)
    {
        vec4 positionRange=index==0 ? uMaterial._CQFLightPositionRange0 : index==1 ? uMaterial._CQFLightPositionRange1 : index==2 ? uMaterial._CQFLightPositionRange2 : uMaterial._CQFLightPositionRange3;
        vec4 colorIntensity=index==0 ? uMaterial._CQFLightColorIntensity0 : index==1 ? uMaterial._CQFLightColorIntensity1 : index==2 ? uMaterial._CQFLightColorIntensity2 : uMaterial._CQFLightColorIntensity3;
        vec4 endKind=index==0 ? uMaterial._CQFLightEndKind0 : index==1 ? uMaterial._CQFLightEndKind1 : index==2 ? uMaterial._CQFLightEndKind2 : uMaterial._CQFLightEndKind3;
        vec4 directionCone=index==0 ? uMaterial._CQFLightDirectionCone0 : index==1 ? uMaterial._CQFLightDirectionCone1 : index==2 ? uMaterial._CQFLightDirectionCone2 : uMaterial._CQFLightDirectionCone3;
        vec4 parameters=index==0 ? uMaterial._CQFLightParameters0 : index==1 ? uMaterial._CQFLightParameters1 : index==2 ? uMaterial._CQFLightParameters2 : uMaterial._CQFLightParameters3;
        vec4 shadowOrigin=positionRange;
        if(endKind.w>1.5) shadowOrigin.xyz=(positionRange.xyz+endKind.xyz)*.5;
        vec3 source=positionRange.xyz;
        if(endKind.w>1.5)
        {
            vec3 line=endKind.xyz-source;
            source+=line*clamp(dot(position-source,line)/max(dot(line,line),.0001),0.0,1.0);
        }
        vec3 delta=source-position;
        float distance=length(delta),range=positionRange.w;
        if(distance>=range) continue;
        vec3 incoming=delta/max(distance,.001);
        float facing=max(0.0,dot(normal,incoming));
        if(facing<=0.0) continue;
        float attenuation=1.0-distance/range;
        attenuation*=attenuation;
        float kind=endKind.w;
        if(kind>.5 && kind<1.5)
            attenuation*=smoothstep(directionCone.w,parameters.x,dot(-incoming,directionCone.xyz));
        if(attenuation<.0001) continue;
        float shadow=cqfSceneVisibility(position,geometryNormal,shadowOrigin,parameters);
        illumination+=colorIntensity.rgb*(colorIntensity.a*attenuation*facing*shadow);
    }
    return illumination;
}

float cqfSceneBeamMask(vec3 position)
{
    if(uMaterial._CQFBeamShape.z<=0.0) return 0.0;
    float distance=abs(dot(vec4(position,1.0),uMaterial._CQFBeamPlane));
    return 1.0-smoothstep(uMaterial._CQFBeamShape.x,uMaterial._CQFBeamShape.y,distance);
}

float cqfSceneBeamVisibility(vec3 position, vec3 normal)
{
    if(length(position-uMaterial._CQFBeamOrigin.xyz)>uMaterial._CQFBeamOrigin.w) return 1.0;
    vec4 parameters=vec4(0.0,uMaterial._CQFBeamParameters.x,uMaterial._CQFBeamShape.w,uMaterial._CQFBeamParameters.y);
    return cqfSceneVisibility(position,normal,uMaterial._CQFBeamOrigin,parameters);
}

float cqfSceneBeamSurface(vec3 position, vec3 normal, bool distant)
{
    float band=cqfSceneBeamMask(position);
    if(band<=0.0) return 0.0;
    float visible=distant ? .35 : cqfSceneBeamVisibility(position,normal);
    return band*visible*uMaterial._CQFBeamShape.z*smoothstep(-18.0,2.0,position.y);
}
