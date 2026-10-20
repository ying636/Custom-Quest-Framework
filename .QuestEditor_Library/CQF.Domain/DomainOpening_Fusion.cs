using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainOpening_Fusion : DomainOpening
{
    public DomainOpening First => first ?? throw new InvalidOperationException("[CQF] Fused opening is missing its first domain.");
    public DomainOpening Second => second ?? throw new InvalidOperationException("[CQF] Fused opening is missing its second domain.");
    public override DomainDef Definition => First.Definition;
    public override Pawn? Caster => first?.Caster;
    public override bool Active => Spawned && first?.Active == true && second?.Active == true && first.Map == Map && second.Map == Map;
    public override float Progress => Mathf.Min(First.Progress, Second.Progress);
    public float ContactAge => contactTick < 0 ? 0f : Math.Max(0, GenTicks.TicksGame - contactTick) / 60f;
    public float VisualBlend => Active && !visualError ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(ContactAge / 0.4f)) : 0f;
    public bool Prepared => CQFDomainRuntime.Instance.HasAvailable(First.Definition) && CQFDomainRuntime.Instance.HasTerrainTemplate(Second.Definition);

    public static DomainOpening_Fusion Create(DomainOpening first, DomainOpening second)
    {
        DomainOpening_Fusion fusion = (DomainOpening_Fusion)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("CQF_DomainOpeningFusion"));
        fusion.first = first;
        fusion.second = second;
        first.Fusion = fusion;
        second.Fusion = fusion;
        try { return (DomainOpening_Fusion)GenSpawn.Spawn(fusion, first.Position, first.Map); }
        catch
        {
            first.Fusion = second.Fusion = null;
            first.ResumeAfterFusion();
            second.ResumeAfterFusion();
            throw;
        }
    }

    public DomainFusionState DomeStateFor(DomainOpening opening)
    {
        if (!Active || VisualBlend <= 0f) return default;
        DomainOpening rival = opening == First ? Second : First;
        return new DomainFusionState(opening.Position.ToVector3Shifted(), GrowingRadius(opening), rival.Position.ToVector3Shifted(), GrowingRadius(rival),
            opening == Second, GenTicks.TicksGame / 60f + 6f, VisualBlend, 0f);
    }

    public override void CompleteOpening()
    {
        if (!Active) { if (!Destroyed) Destroy(); return; }
        CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
#if RIMWORLD_1_7
        if (Find.CurrentMap == Map && WorldRendererUtility.DrawingMap && Screen.width > 0 && Screen.height > 0)
        {
            try
            {
                DomainFusionTransition transition = new(this);
                runtime.SetTransition(transition);
                transition.BindOpening(this);
                return;
            }
            catch (Exception error)
            {
                runtime.SetTransition(null);
                Log.Error("[CQF] Could not start fused domain transition: " + error);
            }
        }
#endif
        CompleteTransfer();
    }

    public override void CompleteTransfer()
    {
        if (Destroyed || !Spawned) return;
        try { if (Active) CQFDomainRuntime.Instance.EnterFusion(this); }
        catch (Exception error)
        {
            CQFDomainRuntime.Instance.SetTransition(null);
            Log.Error("[CQF] Fused domain entry failed: " + error);
        }
        finally
        {
            if (first != null && !first.Destroyed) first.Destroy();
            if (second != null && !second.Destroyed) second.Destroy();
            if (!Destroyed) Destroy();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref first, "first");
        Scribe_References.Look(ref second, "second");
        Scribe_Values.Look(ref contactTick, "contactTick", -1);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (first == null || second == null || first == second))
            throw new InvalidOperationException("[CQF] Invalid saved fused opening pair.");
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        ReleaseVisuals();
        foreach (DomainOpening? opening in new[] { first, second })
            if (opening?.Fusion == this)
            {
                opening.Fusion = null;
                if (!opening.Destroyed) opening.ResumeAfterFusion();
            }
        base.DeSpawn(mode);
    }

    internal void ReleaseVisuals()
    {
#if RIMWORLD_1_7
        visuals?.Dispose();
        visuals = null;
#endif
    }

    protected override void Tick()
    {
        base.Tick();
        if (!Active) { Destroy(); return; }
        float radius = GrowingRadius(First) + GrowingRadius(Second);
        if (contactTick < 0 && First.Position.DistanceToSquared(Second.Position) <= radius * radius) contactTick = GenTicks.TicksGame;
        if (Progress < 1f || ContactAge < 1f || transferQueued) return;
        transferQueued = true;
        CQFDomainRuntime.Instance.QueueOpening(this);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
#if RIMWORLD_1_7
        if (!Active || VisualBlend <= 0f || visualError) return;
        try
        {
            visuals ??= new DomainFusionVisuals(First.Definition, Second.Definition);
            visuals.Draw(First.Position.ToVector3Shifted(), GrowingRadius(First), Second.Position.ToVector3Shifted(), GrowingRadius(Second),
                GenTicks.TicksGame / 60f, ContactAge, VisualBlend, 0.5f);
        }
        catch (Exception error)
        {
            visualError = true;
            visuals?.Dispose();
            visuals = null;
            Log.Error("[CQF] Fused opening world visuals failed: " + error);
        }
#endif
    }

    private float GrowingRadius(DomainOpening opening)
        => opening.Radius * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, opening.Definition.visuals?.expansionEnd ?? 0.55f, opening.Progress));

    private DomainOpening? first;
    private DomainOpening? second;
    private bool transferQueued;
    private int contactTick = -1;
#if RIMWORLD_1_7
    private DomainFusionVisuals? visuals;
#endif
    private bool visualError;
}
