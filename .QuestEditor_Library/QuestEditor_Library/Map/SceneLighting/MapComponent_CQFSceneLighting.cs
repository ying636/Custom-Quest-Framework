#if RIMWORLD_1_7
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class MapComponent_CQFSceneLighting : MapComponent, IDisposable
{
    public MapComponent_CQFSceneLighting(Map map) : base(map)
    {
    }

    public IReadOnlyList<CQFSceneLight> Lights => this.lights ?? (IReadOnlyList<CQFSceneLight>)Array.Empty<CQFSceneLight>();
    public int ShadowRevision => this.shadows?.Revision ?? 0;
    public int NativeLightCount => this.glowHandles?.Count ?? 0;
    public Texture2D? ShadowTexture => this.shadows?.Texture;
    public CQFSceneBeam? Beam => this.beam;
    public long PhaseTicks => this.phaseTicks;
    public Vector4 BeamPlane => this.beamPlane;
    public float BeamGain => this.beamGain;
    public int NativeRevision { get; private set; }
    public CQFSceneScattering? Scattering => this.scattering;

    public void Configure(IEnumerable<CQFSceneLight> sources) => this.Configure(sources, null);

    public void Configure(IEnumerable<CQFSceneLight> sources, CQFSceneBeam? mainBeam)
    {
        List<CQFSceneLight> copy = sources.Select(source => source.Copy()).ToList();
        if (copy.Count > 16) throw new ArgumentOutOfRangeException(nameof(sources), "CQF_SceneLightLimit");
        foreach (CQFSceneLight source in copy) this.Validate(source);
        this.lights = copy;
        this.runtimeLights = copy.Select(source => source.Copy()).ToList();
        this.beam = mainBeam?.Copy();
        if (this.beam != null && (this.beam.lightIndex < 0 || this.beam.lightIndex >= copy.Count)) throw new ArgumentOutOfRangeException(nameof(mainBeam), "CQF_SceneBeamLightIndex");
        if (this.beam != null && (!float.IsFinite(this.beam.period) || this.beam.period <= 0f)) throw new ArgumentException("CQF_SceneBeamPeriod");
        if (this.beam != null && (this.beam.innerWidth < 0f || this.beam.outerWidth <= this.beam.innerWidth)) throw new ArgumentException("CQF_SceneBeamWidth");
        this.phaseTicks = 0;
        this.lastFingerprint = 0;
        this.shadowDirty = this.nativeDirty = true;
        this.immediateShadow = this.immediateNative = true;
        this.Subscribe();
    }

    public void SetOccluders(IReadOnlyList<Bounds> boxes)
    {
        bool same = this.sceneOccluders != null && this.sceneOccluders.Count == boxes.Count;
        if (same)
            for (int i = 0; i < boxes.Count; i++)
                if (!this.sceneOccluders![i].Equals(boxes[i])) { same = false; break; }
        if (same) return;
        this.sceneOccluders ??= new List<Bounds>();
        this.sceneOccluders.Clear();
        for (int i = 0; i < boxes.Count; i++) this.sceneOccluders.Add(boxes[i]);
        this.occludersDirty = true;
        this.shadowDirty = true;
        this.immediateShadow = true;
    }

    public Material MaterialFor(Material template)
    {
        this.materials ??= new Dictionary<Material, Material>();
        if (!this.materials.TryGetValue(template, out Material? material))
        {
            material = new Material(template);
            this.materials.Add(template, material);
        }
        return material;
    }

    public void Bind(Material material, Bounds receiver, bool receiveLocal = true)
    {
        this.UpdateLighting();
        this.positions ??= new Vector4[4];
        this.colors ??= new Vector4[4];
        this.ends ??= new Vector4[4];
        this.directions ??= new Vector4[4];
        this.parameters ??= new Vector4[4];
        Span<int> chosen = stackalloc int[4];
        Span<float> scores = stackalloc float[4];
        chosen.Fill(-1);
        scores.Clear();
        if (receiveLocal && this.runtimeLights != null)
        {
            for (int index = 0; index < this.runtimeLights.Count; index++)
            {
                if (this.beam != null && index == this.beam.lightIndex) continue;
                CQFSceneLight light = this.runtimeLights[index];
                if (!light.enabled || light.intensity <= 0f) continue;
                Vector3 point = light.position;
                if (light.kind == CQFSceneLightKind.Strip)
                {
                    Vector3 line = light.end - light.position;
                    point += line * Mathf.Clamp01(Vector3.Dot(receiver.center - point, line) / Mathf.Max(.0001f, line.sqrMagnitude));
                }
                float distance = Mathf.Sqrt(receiver.SqrDistance(point));
                if (distance >= light.range) continue;
                float score = light.intensity * (1f - distance / light.range);
                for (int i = 0; i < 4; i++)
                {
                    if (score <= scores[i]) continue;
                    for (int j = 3; j > i; j--) { scores[j] = scores[j - 1]; chosen[j] = chosen[j - 1]; }
                    scores[i] = score;
                    chosen[i] = index;
                    break;
                }
            }
        }
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            if (chosen[i] < 0) { this.colors[i] = Vector4.zero; continue; }
            count++;
            CQFSceneLight light = this.runtimeLights![chosen[i]];
            Vector3 direction = light.direction.normalized;
            this.positions[i] = new Vector4(light.position.x, light.position.y, light.position.z, light.range);
            this.colors[i] = new Vector4(light.color.r, light.color.g, light.color.b, light.intensity);
            this.ends[i] = new Vector4(light.end.x, light.end.y, light.end.z, (float)light.kind);
            this.directions[i] = new Vector4(direction.x, direction.y, direction.z, Mathf.Cos(light.outerAngle * .5f * Mathf.Deg2Rad));
            this.parameters[i] = new Vector4(Mathf.Cos(light.innerAngle * .5f * Mathf.Deg2Rad), light.softness, chosen[i], light.castsShadows ? 1f : 0f);
        }
        float phase = GenLocalDate.DayTick(this.map) / 60000f * Mathf.PI * 2f;
        Vector3 sun = new Vector3(Mathf.Sin(phase) * .7f, .7f, Mathf.Cos(phase) * .35f).normalized;
        material.SetVector("_CQFSunDirection", new Vector4(sun.x, sun.y, sun.z, 0f));
        material.SetFloat("_CQFLightCount", count);
        for (int i = 0; i < 4; i++)
        {
            material.SetVector(PositionNames[i], this.positions[i]);
            material.SetVector(ColorNames[i], this.colors[i]);
            material.SetVector(EndNames[i], this.ends[i]);
            material.SetVector(DirectionNames[i], this.directions[i]);
            material.SetVector(ParameterNames[i], this.parameters[i]);
        }
        this.BindBeam(material);
    }

    public void BindBeam(Material material)
    {
        this.UpdateLighting();
        Texture2D texture = this.shadows?.Texture ?? BaseContent.BlackTex;
        material.SetTexture("_CQFShadowAtlas", texture);
        material.SetVector("_CQFShadowInfo", new Vector4(1f / texture.width, 1f / texture.height, CQFSceneShadowAtlas.Resolution, this.lights?.Count ?? 0));
        material.SetVector("_CQFBeamPlane", this.beamPlane);
        material.SetVector("_CQFBeamShape", new Vector4(this.beam?.innerWidth ?? 0f, this.beam?.outerWidth ?? 1f, this.beamGain, this.beam?.lightIndex ?? 0));
        Color color = this.beam?.color ?? Color.white;
        material.SetVector("_CQFBeamColor", new Vector4(color.r, color.g, color.b, this.beam?.density ?? 0f));
        material.SetVector("_CQFBeamTime", new Vector4(this.phaseTicks / 60f, this.beam?.scatterHeight ?? 0f, .85f, 0f));
        if (this.beam != null && this.runtimeLights != null)
        {
            CQFSceneLight light = this.runtimeLights[this.beam.lightIndex];
            Vector4 origin = this.shadows!.Origins[this.beam.lightIndex];
            material.SetVector("_CQFBeamOrigin", origin);
            material.SetVector("_CQFBeamParameters", new Vector4(light.softness, light.castsShadows ? 1f : 0f, this.beam.dust, 0f));
        }
    }

    public Texture? RenderScattering()
    {
        this.UpdateLighting();
        if (this.beam == null || this.beamGain <= 0f || this.beam.density <= 0f) return null;
        this.scattering ??= new CQFSceneScattering(this.map);
        return this.scattering.Render(this);
    }

    public override void MapComponentUpdate()
    {
        if (this.lights != null && Find.CurrentMap == this.map) this.UpdateLighting();
    }

    public override void MapComponentTick()
    {
        if (this.beam is { enabled: true }) this.phaseTicks++;
    }

    public override void FinalizeInit()
    {
        if (this.lights is { Count: > 0 }) this.Subscribe();
        this.occludersDirty = true;
        this.shadowDirty = this.nativeDirty = true;
        this.immediateShadow = this.immediateNative = true;
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref this.lights, "CQF_sceneLights", LookMode.Deep);
        Scribe_Deep.Look(ref this.beam, "CQF_sceneBeam");
        Scribe_Values.Look(ref this.phaseTicks, "CQF_sceneLightPhaseTicks");
    }

    public override void MapRemoved() => this.Dispose();

    public void Dispose()
    {
        this.ClearNativeLights();
        this.shadows?.Dispose();
        this.shadows = null;
        this.scattering?.Dispose();
        this.scattering = null;
        if (this.materials != null)
        {
            foreach (Material material in this.materials.Values) UnityEngine.Object.Destroy(material);
            this.materials.Clear();
        }
        if (this.subscribed)
        {
            this.map.events.BuildingSpawned -= this.BuildingChanged;
            this.map.events.BuildingDespawned -= this.BuildingChanged;
            this.map.events.DoorOpened -= this.DoorChanged;
            this.map.events.DoorClosed -= this.DoorChanged;
            this.subscribed = false;
        }
        this.lastFrame = -1;
        this.occluders?.Clear();
        this.occludersDirty = true;
        this.shadowDirty = this.nativeDirty = true;
        this.immediateShadow = this.immediateNative = true;
    }

    private void UpdateLighting()
    {
        if (this.lastFrame == Time.frameCount) return;
        this.lastFrame = Time.frameCount;
        if (this.lights == null) return;
        this.runtimeLights ??= this.lights.Select(source => source.Copy()).ToList();
        this.UpdateBeamState();
        HashCode fingerprint = new HashCode();
        HashCode configurationShadow = new HashCode();
        HashCode shadowFingerprint = new HashCode();
        HashCode nativeFingerprint = new HashCode();
        foreach (CQFSceneLight light in this.lights)
        {
            fingerprint.Add(light.position); fingerprint.Add(light.end); fingerprint.Add(light.direction); fingerprint.Add(light.color);
            fingerprint.Add(light.kind); fingerprint.Add(light.range); fingerprint.Add(light.intensity); fingerprint.Add(light.enabled);
            fingerprint.Add(light.castsShadows); fingerprint.Add(light.softness); fingerprint.Add(light.innerAngle); fingerprint.Add(light.outerAngle); fingerprint.Add(light.glowRadius);
            configurationShadow.Add(light.position); configurationShadow.Add(light.end); configurationShadow.Add(light.kind); configurationShadow.Add(light.range); configurationShadow.Add(light.castsShadows);
        }
        for (int index = 0; index < this.runtimeLights.Count; index++)
        {
            CQFSceneLight light = this.runtimeLights[index];
            shadowFingerprint.Add(light.position); shadowFingerprint.Add(light.end); shadowFingerprint.Add(light.kind); shadowFingerprint.Add(light.range); shadowFingerprint.Add(light.castsShadows);
            Vector3 nativePosition = light.position, nativeEnd = light.end;
            if (this.beam != null && index == this.beam.lightIndex) { nativePosition.x -= this.beam.heightSlope * nativePosition.y; nativeEnd.x -= this.beam.heightSlope * nativeEnd.y; }
            nativeFingerprint.Add(new IntVec3(Mathf.FloorToInt(nativePosition.x), 0, Mathf.FloorToInt(nativePosition.z)));
            nativeFingerprint.Add(new IntVec3(Mathf.FloorToInt(nativeEnd.x), 0, Mathf.FloorToInt(nativeEnd.z)));
            nativeFingerprint.Add(light.kind); nativeFingerprint.Add((Color32)(light.color * Mathf.Clamp01(light.intensity))); nativeFingerprint.Add(light.enabled); nativeFingerprint.Add(light.glowRadius);
        }
        int current = fingerprint.ToHashCode();
        if (current != this.lastFingerprint)
        {
            foreach (CQFSceneLight light in this.lights) this.Validate(light);
            this.lastFingerprint = current;
            this.nativeDirty = this.immediateNative = true;
        }
        int configurationState = configurationShadow.ToHashCode();
        if (configurationState != this.lastConfigurationShadow) { this.immediateShadow = true; this.lastConfigurationShadow = configurationState; }
        int shadowState = shadowFingerprint.ToHashCode(), nativeState = nativeFingerprint.ToHashCode();
        if (shadowState != this.lastShadowFingerprint) { this.shadowDirty = true; this.lastShadowFingerprint = shadowState; }
        if (nativeState != this.lastNativeFingerprint) { this.nativeDirty = true; this.lastNativeFingerprint = nativeState; }
        if (this.nativeDirty && (this.immediateNative || Time.realtimeSinceStartup - this.lastNativeTime >= .2f))
        {
            this.ClearNativeLights();
            this.glowHandles ??= new List<int>();
            for (int index = 0; index < this.runtimeLights.Count; index++)
            {
                CQFSceneLight light = this.runtimeLights[index];
                if (!light.enabled || light.intensity <= 0f || light.glowRadius <= 0f) continue;
                int samples = light.kind == CQFSceneLightKind.Strip ? Mathf.Clamp(Mathf.CeilToInt((light.end - light.position).magnitude / 8f), 2, 8) : 1;
                for (int i = 0; i < samples; i++)
                {
                    Vector3 point = samples == 1 ? light.position : Vector3.Lerp(light.position, light.end, (i + .5f) / samples);
                    if (this.beam != null && index == this.beam.lightIndex) point.x -= this.beam.heightSlope * point.y;
                    IntVec3 cell = new IntVec3(Mathf.FloorToInt(point.x), 0, Mathf.FloorToInt(point.z));
                    if (!cell.InBounds(this.map)) continue;
                    Color color = light.color * Mathf.Clamp01(light.intensity / Mathf.Sqrt(samples));
                    color.a = 1f;
                    this.glowHandles.Add(this.map.glowGrid.RegisterCustomGlower(cell, Mathf.Min(40f, light.glowRadius), new ColorInt(color)));
                }
            }
            this.nativeDirty = false;
            this.immediateNative = false;
            this.lastNativeTime = Time.realtimeSinceStartup;
            this.NativeRevision++;
        }
        if (this.shadowDirty && this.runtimeLights.Count > 0 && (this.immediateShadow || Time.realtimeSinceStartup - this.lastShadowTime >= .1f))
        {
            this.UpdateOccluders();
            this.shadows ??= new CQFSceneShadowAtlas();
            this.shadows.Rebuild(this.runtimeLights, this.occluders!);
            this.shadowDirty = false;
            this.immediateShadow = false;
            this.lastShadowTime = Time.realtimeSinceStartup;
        }
    }

    private void UpdateOccluders()
    {
        if (!this.occludersDirty) return;
        this.occluders ??= new List<Bounds>();
        this.occluders.Clear();
        if (this.sceneOccluders != null) this.occluders.AddRange(this.sceneOccluders);
        foreach (Building building in this.map.listerThings.AllThings.OfType<Building>())
        {
            if (!building.def.blockLight || building is Building_Door { Open: true }) continue;
            CellRect rect = building.OccupiedRect();
            Bounds box = new Bounds(new Vector3(rect.minX + rect.Width * .5f, 1.6f, rect.minZ + rect.Height * .5f), new Vector3(rect.Width, 3.2f, rect.Height));
            bool included = false;
            if (this.sceneOccluders != null)
                foreach (Bounds scene in this.sceneOccluders)
                    if (scene.Contains(box.center)) { included = true; break; }
            if (!included) this.occluders.Add(box);
        }
        this.occludersDirty = false;
    }

    private void UpdateBeamState()
    {
        for (int index = 0; index < this.lights!.Count; index++) this.lights[index].CopyTo(this.runtimeLights![index]);
        this.beamGain = 0f;
        if (this.beam == null) return;
        float angle = this.phaseTicks / 60f * Mathf.PI * 2f / this.beam.period;
        float offsetChange = Mathf.Sin(angle) * this.beam.drift;
        float slopeChange = Mathf.Sin(angle * .67f) * this.beam.sway;
        this.beamPlane = new Vector4(1f, -this.beam.heightSlope, -this.beam.slope - slopeChange, -this.beam.offset - offsetChange);
        CQFSceneLight light = this.runtimeLights![this.beam.lightIndex];
        light.position.x += offsetChange + slopeChange * light.position.z;
        light.end.x += offsetChange + slopeChange * light.end.z;
        float sky = Mathf.Lerp(1f, Mathf.Clamp01(this.map.skyManager.CurSkyGlow), Mathf.Clamp01(this.beam.skyResponse));
        float modulation = sky * (.96f + .04f * Mathf.Cos(angle * 1.3f));
        light.intensity *= modulation * this.beam.intensity;
        light.color *= this.beam.color;
        light.enabled &= this.beam.enabled;
        if (light.enabled && light.intensity > 0f) this.beamGain = this.beam.intensity * modulation;
    }

    private void Validate(CQFSceneLight light)
    {
        if (!float.IsFinite(light.range) || light.range <= 0f) throw new ArgumentException("CQF_SceneLightRange");
        if (!float.IsFinite(light.intensity) || light.intensity < 0f) throw new ArgumentException("CQF_SceneLightIntensity");
        if (!float.IsFinite(light.softness) || light.softness < 0f) throw new ArgumentException("CQF_SceneLightSoftness");
        if (light.kind == CQFSceneLightKind.Spot && (light.direction.sqrMagnitude < .0001f || light.innerAngle < 0f || light.outerAngle <= light.innerAngle || light.outerAngle >= 179f)) throw new ArgumentException("CQF_SceneLightCone");
    }

    private void Subscribe()
    {
        if (this.subscribed || this.map.events == null) return;
        this.map.events.BuildingSpawned += this.BuildingChanged;
        this.map.events.BuildingDespawned += this.BuildingChanged;
        this.map.events.DoorOpened += this.DoorChanged;
        this.map.events.DoorClosed += this.DoorChanged;
        this.subscribed = true;
    }

    private void BuildingChanged(Building building) { if (building.def.blockLight) this.occludersDirty = this.shadowDirty = this.immediateShadow = true; }
    private void DoorChanged(Building_Door door) => this.occludersDirty = this.shadowDirty = this.immediateShadow = true;

    private void ClearNativeLights()
    {
        if (this.glowHandles == null) return;
        foreach (int handle in this.glowHandles) this.map.glowGrid.DeregisterCustomGlower(handle);
        this.glowHandles.Clear();
    }

    private static readonly string[] PositionNames = { "_CQFLightPositionRange0", "_CQFLightPositionRange1", "_CQFLightPositionRange2", "_CQFLightPositionRange3" };
    private static readonly string[] ColorNames = { "_CQFLightColorIntensity0", "_CQFLightColorIntensity1", "_CQFLightColorIntensity2", "_CQFLightColorIntensity3" };
    private static readonly string[] EndNames = { "_CQFLightEndKind0", "_CQFLightEndKind1", "_CQFLightEndKind2", "_CQFLightEndKind3" };
    private static readonly string[] DirectionNames = { "_CQFLightDirectionCone0", "_CQFLightDirectionCone1", "_CQFLightDirectionCone2", "_CQFLightDirectionCone3" };
    private static readonly string[] ParameterNames = { "_CQFLightParameters0", "_CQFLightParameters1", "_CQFLightParameters2", "_CQFLightParameters3" };
    private List<CQFSceneLight>? lights;
    private List<CQFSceneLight>? runtimeLights;
    private CQFSceneBeam? beam;
    private CQFSceneScattering? scattering;
    private List<Bounds>? sceneOccluders;
    private List<Bounds>? occluders;
    private List<int>? glowHandles;
    private Dictionary<Material, Material>? materials;
    private CQFSceneShadowAtlas? shadows;
    private Vector4[]? positions;
    private Vector4[]? colors;
    private Vector4[]? ends;
    private Vector4[]? directions;
    private Vector4[]? parameters;
    private Vector4 beamPlane;
    private float beamGain;
    private long phaseTicks;
    private int lastFrame = -1;
    private int lastFingerprint;
    private int lastShadowFingerprint;
    private int lastNativeFingerprint;
    private int lastConfigurationShadow;
    private float lastShadowTime = -1f;
    private float lastNativeTime = -1f;
    private bool immediateShadow = true;
    private bool immediateNative = true;
    private bool shadowDirty = true;
    private bool nativeDirty = true;
    private bool subscribed;
    private bool occludersDirty = true;
}
#endif
