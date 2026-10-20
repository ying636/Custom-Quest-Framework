using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class DomainOpening_Basic : DomainOpening
{
    public override DomainDef Definition => definition ?? throw new InvalidOperationException("[CQF] Basic domain opening has no definition.");
    public override Pawn? Caster => caster;
    public override bool Active => Spawned && caster != null && !caster.Dead && !caster.Downed && caster.MapHeld == Map;
    public override float Progress => Mathf.Clamp01((Find.TickManager.TicksGame - startTick) / (float)Visuals.openingTicks);
    public override IReadOnlyList<Pawn> Entrants => entrants;
#if RIMWORLD_1_7
    public override bool SupportsFusion => true;
#endif
    public DomainVisualDef Visuals => Definition.visuals ?? throw new InvalidOperationException("[CQF] Basic domain opening requires DomainDef.visuals.");

    public static DomainOpening_Basic Start(DomainDef definition, Pawn caster, IEnumerable<Pawn> targets)
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
        opening.startTick = Find.TickManager.TicksGame;
        opening.entrants.Add(caster);
        opening.entrants.AddRange(targets.Where(pawn => pawn != caster && pawn.Spawned && !pawn.Dead && pawn.Map == caster.Map).Distinct());
        foreach (Pawn pawn in opening.entrants)
            opening.originalStunEnds.Add(Find.TickManager.TicksGame + pawn.stances.stunner.StunTicksLeft);
        try
        {
            GenSpawn.Spawn(opening, caster.Position, caster.Map);
            foreach (Pawn pawn in opening.entrants)
            {
                pawn.pather?.StopDead();
                pawn.stances.stunner.StunFor(definition.visuals.openingTicks, caster, false, false, true);
            }
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
            ReleaseLocks();
            MapComponent_Domain domain = CQFDomainRuntime.Instance.EnterOrContest(Definition, caster!, entrants.Where(pawn => pawn != caster && pawn != null
                && pawn.Spawned && !pawn.Dead && pawn.Map == Map));
            CQFDomainRuntime.Instance.Transition?.Bind(domain);
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
        Scribe_Values.Look(ref startTick, "startTick");
        Scribe_Collections.Look(ref entrants, "entrants", LookMode.Reference);
        Scribe_Collections.Look(ref originalStunEnds, "originalStunEnds", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (definition?.visuals == null || entrants.Count != originalStunEnds.Count))
            throw new InvalidOperationException("[CQF] Basic domain opening has invalid saved configuration or lock records.");
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        ReleaseLocks();
        ReleaseVisuals();
        base.DeSpawn(mode);
    }

    public override void ReleaseForFusion() => ReleaseLocks();

    public override void ResumeAfterFusion() => transferQueued = false;

    internal void ReleaseVisuals()
    {
        visuals?.Dispose();
        visuals = null;
    }

    protected override void Tick()
    {
        base.Tick();
        if (!Active) { Destroy(); return; }
        if (Progress < 1f || transferQueued || Fusion?.Active == true) return;
        transferQueued = true;
        CQFDomainRuntime.Instance.QueueOpening(this);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (!Active || visualError) return;
        try
        {
            visuals ??= new DomainOpeningVisuals(Visuals);
            visuals.Draw(drawLoc, Progress, (Find.TickManager.TicksGame - startTick) / 60f, fusion: Fusion?.DomeStateFor(this) ?? default);
        }
        catch (Exception error)
        {
            visualError = true;
            visuals?.Dispose();
            visuals = null;
            Log.Error("[CQF] Basic domain opening visuals failed: " + error);
        }
    }

    private void ReleaseLocks()
    {
        if (locksReleased) return;
        locksReleased = true;
        for (int index = 0; index < entrants.Count; index++)
        {
            Pawn pawn = entrants[index];
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map != Map) continue;
            int remaining = Math.Max(0, originalStunEnds[index] - Find.TickManager.TicksGame);
            int openingRemaining = Math.Max(0, startTick + Visuals.openingTicks - Find.TickManager.TicksGame);
            if (pawn.stances.stunner.StunTicksLeft > Math.Max(remaining, openingRemaining)) continue;
            pawn.stances.stunner.StopStun();
            if (remaining > 0) pawn.stances.stunner.StunFor(remaining, null, false, false, true);
        }
    }

    private DomainDef? definition;
    private Pawn? caster;
    private int startTick;
    private List<Pawn> entrants = new();
    private List<int> originalStunEnds = new();
    private bool transferQueued;
    private bool locksReleased;
    private bool visualError;
    private DomainOpeningVisuals? visuals;
}
