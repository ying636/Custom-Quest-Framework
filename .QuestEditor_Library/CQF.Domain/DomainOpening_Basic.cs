using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class DomainOpening_Basic : DomainOpening
{
    public override DomainDef Definition => definition ?? throw new InvalidOperationException("[CQF] Basic domain opening has no definition.");
    public override Pawn? Caster => caster;
    public override bool Active => Spawned && caster != null && !caster.Dead && !caster.Downed && caster.MapHeld == Map && CounterOpeningValid;
#if RIMWORLD_1_7
    public override bool SupportsFusion => !IsCounterOpening;
#endif
    public DomainVisualDef Visuals => Definition.visuals ?? throw new InvalidOperationException("[CQF] Basic domain opening requires DomainDef.visuals.");

    public static DomainOpening_Basic Start(DomainDef definition, Pawn caster, IEnumerable<Pawn>? targets = null)
    {
        if (!caster.Spawned || caster.Dead || caster.Downed)
            throw new InvalidOperationException("[CQF] Basic domain opening requires a living, standing, spawned caster.");
        if (definition.visuals == null) throw new InvalidOperationException("[CQF] Basic domain opening requires DomainDef.visuals.");
        if (caster.Map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("CQF_DomainOpening"))
            .OfType<DomainOpening_Basic>().Any(opening => opening.Active && opening.Caster == caster))
            throw new InvalidOperationException("[CQF] This caster already has a basic domain opening.");
        DomainOpening_Basic opening = (DomainOpening_Basic)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("CQF_DomainOpening"));
        opening.definition = definition;
        opening.caster = caster;
        try
        {
            GenSpawn.Spawn(opening, caster.Position, caster.Map);
            opening.InitializeOpening();
            return opening;
        }
        catch
        {
            if (opening.Spawned) opening.Destroy();
            throw;
        }
    }

    public override void CompleteOpening()
    {
        if (!Active) { if (!Destroyed) Destroy(); return; }
        CQFDomainRuntime manager = CQFDomainRuntime.Instance;
        manager.SetTransition(null);
        if (Find.CurrentMap == Map && WorldRendererUtility.DrawingMap && Screen.width > 0 && Screen.height > 0)
        {
            try
            {
                DomainMaterialTransition transition = new(Visuals, Position, false);
                manager.SetTransition(transition);
#if RIMWORLD_1_7
                transition.BindOpening(this);
                return;
#endif
            }
            catch (Exception error)
            {
                manager.SetTransition(null);
                Log.Error("[CQF] Could not start basic domain transition; entering without the effect: " + error);
            }
        }
        CompleteTransfer();
    }

    public override void CompleteTransfer()
    {
        if (Destroyed || !Spawned) return;
        try
        {
            if (!Active) return;
            ReleaseOpeningLocks();
            MapComponent_Domain domain = CQFDomainRuntime.Instance.EnterOrContest(Definition, caster!, CaptureEntrants());
            BindEntryTransition(domain);
        }
        catch (Exception error)
        {
            CQFDomainRuntime.Instance.SetTransition(null);
            Log.Error("[CQF] Basic domain opening failed: " + error);
        }
        finally { if (!Destroyed) Destroy(); }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref definition, "definition");
        Scribe_References.Look(ref caster, "caster");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && definition?.visuals == null)
            throw new InvalidOperationException("[CQF] Basic domain opening has invalid saved configuration or lock records.");
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        ReleaseOpeningLocks();
        ReleaseVisuals();
        base.DeSpawn(mode);
    }

    public override void ResumeAfterFusion() => transferQueued = false;

    internal void ReleaseVisuals()
    {
        visuals?.Dispose();
        visuals = null;
    }

    protected override void Tick()
    {
        base.Tick();
        if (!Active) { if (!Destroyed) Destroy(); return; }
        if (!ReadyToTransfer || transferQueued || Fusion?.Active == true) return;
        transferQueued = true;
        CQFDomainRuntime.Instance.QueueOpening(this);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (!Active || IsCounterOpening || visualError) return;
        try
        {
            visuals ??= new DomainOpeningVisuals(Visuals);
            visuals.Draw(drawLoc, Progress, GenTicks.TicksGame / 60f, fusion: Fusion?.DomeStateFor(this) ?? default);
        }
        catch (Exception error)
        {
            visualError = true;
            visuals?.Dispose();
            visuals = null;
            Log.Error("[CQF] Basic domain opening visuals failed: " + error);
        }
    }

    private DomainDef? definition;
    private Pawn? caster;
    private bool transferQueued;
    private bool visualError;
    private DomainOpeningVisuals? visuals;
}
