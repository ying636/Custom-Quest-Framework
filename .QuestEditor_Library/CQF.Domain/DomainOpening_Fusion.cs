using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainOpening_Fusion : DomainOpening
{
    public DomainOpening First => first ?? throw new InvalidOperationException("[CQF] Fused opening is missing its first domain.");
    public DomainOpening Second => second ?? throw new InvalidOperationException("[CQF] Fused opening is missing its second domain.");
    public Building_Domain? ExistingAnchor => anchor;
    public DomainDef FirstDefinition => anchor != null ? anchor.Domain!.Definition : First.Definition;
    public IntVec3 FirstPosition => anchor != null ? anchor.Position : First.Position;
    public float FirstRadius => anchor != null ? FirstDefinition.OpeningRadius : GrowingRadius(First);
    public float SecondRadius => GrowingRadius(Second);
    public override DomainDef Definition => FirstDefinition;
    public override Pawn? Caster => anchor != null ? second?.Caster : first?.Caster;
    public override bool Active => Spawned && second?.Active == true && second.Map == Map
        && (anchor != null ? anchor.Spawned && anchor.Map == Map && anchor.Domain?.CanAcceptChallenger == true
            : first?.Active == true && first.Map == Map);
    public override float Progress => anchor != null ? Second.Progress : Mathf.Min(First.Progress, Second.Progress);
    public float ContactAge => contactTick < 0 ? 0f : Math.Max(0, GenTicks.TicksGame - contactTick) / 60f;
    public float VisualBlend => Active && !visualError ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(ContactAge / 0.4f)) : 0f;
    public bool Prepared => (anchor != null || CQFDomainRuntime.Instance.HasAvailable(FirstDefinition))
        && CQFDomainRuntime.Instance.HasTerrainTemplate(Second.Definition);

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
        bool isSecond = opening == Second;
        return new DomainFusionState(opening.Position.ToVector3Shifted(), GrowingRadius(opening),
            (isSecond ? FirstPosition : Second.Position).ToVector3Shifted(), isSecond ? FirstRadius : SecondRadius,
            opening == Second, GenTicks.TicksGame / 60f + 6f, VisualBlend, 0f);
    }

    public DomainFusionState DomeStateFor(Building_Domain existing)
        => existing != anchor || !Active || VisualBlend <= 0f ? default
            : new DomainFusionState(FirstPosition.ToVector3Shifted(), FirstRadius, Second.Position.ToVector3Shifted(), SecondRadius,
                false, GenTicks.TicksGame / 60f + 6f, VisualBlend, 0f);

    public static DomainOpening_Fusion Create(Building_Domain anchor, DomainOpening opening)
    {
        if (anchor.OpeningFusion != null || anchor.Domain?.CanAcceptChallenger != true)
            throw new InvalidOperationException("[CQF] Existing domain is not available for an opening challenge.");
        DomainOpening_Fusion fusion = (DomainOpening_Fusion)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("CQF_DomainOpeningFusion"));
        fusion.anchor = anchor;
        fusion.second = opening;
        anchor.OpeningFusion = opening.Fusion = fusion;
        try { return (DomainOpening_Fusion)GenSpawn.Spawn(fusion, opening.Position, opening.Map); }
        catch
        {
            anchor.OpeningFusion = opening.Fusion = null;
            opening.ResumeAfterFusion();
            throw;
        }
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
        Scribe_References.Look(ref anchor, "anchor");
        Scribe_Values.Look(ref contactTick, "contactTick", -1);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (first == null && anchor == null || second == null || first == second))
            throw new InvalidOperationException("[CQF] Invalid saved fused opening pair.");
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        ReleaseVisuals();
        if (anchor?.OpeningFusion == this) anchor.OpeningFusion = null;
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
        if (Second.ReadyToClose && (anchor != null || First.ReadyToClose)
            && !CQFDomainRuntime.Instance.HasPendingCounter(Second)
            && (anchor != null || !CQFDomainRuntime.Instance.HasPendingCounter(First)))
        {
            if (anchor == null) First.BeginClosing();
            Second.BeginClosing();
        }
        float radius = FirstRadius + SecondRadius;
        if (contactTick < 0 && FirstPosition.DistanceToSquared(Second.Position) <= radius * radius) contactTick = GenTicks.TicksGame;
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
            visuals ??= new DomainFusionVisuals(FirstDefinition, Second.Definition);
            visuals.Draw(FirstPosition.ToVector3Shifted(), FirstRadius, Second.Position.ToVector3Shifted(), SecondRadius,
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
    private Building_Domain? anchor;
    private bool transferQueued;
    private int contactTick = -1;
#if RIMWORLD_1_7
    private DomainFusionVisuals? visuals;
#endif
    private bool visualError;
}
