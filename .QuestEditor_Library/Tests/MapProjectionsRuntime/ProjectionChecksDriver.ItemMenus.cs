using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CQF.MapProjectionChecks;

public sealed partial class ProjectionChecksDriver
{
    private IEnumerable<object?> CheckItemMenus()
    {
        Pawn worker = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        for (int i = 0; i < 20 && worker.WorkTagIsDisabled(WorkTags.Violent); i++)
            worker = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        Check(!worker.WorkTagIsDisabled(WorkTags.Violent), "item-menu pawn can equip weapons");
        GenSpawn.Spawn(worker, new IntVec3(35, 0, 30), source!);
        worker.drafter.Drafted = true;
        Thing weapon = GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Revolver")), new IntVec3(37, 0, 30), source!);
        guiAction = () =>
        {
            Bootstrap.TraceItemMenus = true;
            control!.ResetView();
            ClickPlaying(worker.DrawPos);
            Check(Find.Selector.IsSelected(worker), "native item-menu pawn is selected");
            FirePlaying(weapon.DrawPos, EventType.MouseDown, 1);
            FirePlaying(weapon.DrawPos, EventType.MouseUp, 1);
            FloatMenuMap menu = Find.WindowStack.WindowOfType<FloatMenuMap>() ?? throw new InvalidOperationException("Native equipment menu did not open.");
            ChooseItemMenu(menu, "Equip".Translate(weapon.LabelShort));
            Check(worker.CurJobDef == JobDefOf.Equip && worker.CurJob.targetA.Thing == weapon,
                "clicking a native equipment menu option orders the source pawn");
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        int began = Find.TickManager.TicksGame;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
        while (worker.equipment.Primary != weapon && Find.TickManager.TicksGame - began < 900) yield return null;
        Find.TickManager.Pause();
        Check(worker.equipment.Primary == weapon && worker.Map == source, "source pawn actually equips the weapon after menu selection");
        Thing meal = GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MealSimple")), new IntVec3(36, 0, 29), source!);
        guiAction = () =>
        {
            worker.needs.food.CurLevelPercentage = 0.15f;
            if (!Find.Selector.IsSelected(worker)) CameraJumper.TryJumpAndSelect(worker);
            FirePlaying(meal.DrawPos, EventType.MouseDown, 1);
            FirePlaying(meal.DrawPos, EventType.MouseUp, 1);
            FloatMenuMap menu = Find.WindowStack.WindowOfType<FloatMenuMap>() ?? throw new InvalidOperationException("Native consume menu did not open.");
            ChooseItemMenu(menu, "ConsumeThing".Translate(meal.LabelShort, meal));
            Check(worker.CurJobDef == JobDefOf.Ingest && worker.CurJob.targetA.Thing == meal,
                "clicking a native consume menu option orders the source pawn");
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        began = Find.TickManager.TicksGame;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
        while (!meal.Destroyed && Find.TickManager.TicksGame - began < 1800) yield return null;
        Find.TickManager.Pause();
        Check(meal.Destroyed && worker.Map == source, "source pawn actually consumes the item after menu selection");
        Thing usable = GenSpawn.Spawn(ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("PsychicAmplifier")), new IntVec3(36, 0, 31), source!);
        guiAction = () =>
        {
            if (!Find.Selector.IsSelected(worker)) CameraJumper.TryJumpAndSelect(worker);
            Check(Find.Selector.IsSelected(worker), "usable-item command retains the same source pawn selection");
            FirePlaying(usable.DrawPos, EventType.MouseDown, 1);
            FirePlaying(usable.DrawPos, EventType.MouseUp, 1);
            FloatMenuMap menu = Find.WindowStack.WindowOfType<FloatMenuMap>() ?? throw new InvalidOperationException("Native usable-item menu did not open.");
            CompUsable comp = usable.TryGetComp<CompUsable>();
            ChooseItemMenu(menu, comp.Props.useLabel.Formatted(usable));
            Check(worker.CurJobDef == comp.Props.useJob && worker.CurJob.targetA.Thing == usable,
                "clicking the native usable-item option starts a source-map use job");
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        began = Find.TickManager.TicksGame;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
        while (!usable.Destroyed && Find.TickManager.TicksGame - began < 1800) yield return null;
        Find.TickManager.Pause();
        Check(usable.Destroyed && worker.health.hediffSet.HasHediff(HediffDefOf.PsychicAmplifier),
            "native usable item is consumed and applies its effect to the source pawn");
        Check(CQFMapProjectionContext.Current == null && Find.CurrentMap == host,
            "deferred window callbacks restore the real host map after item actions");
        Bootstrap.TraceItemMenus = false;
        savedViewportZoom = control!.ViewZoom;
        savedViewportRect = control.ViewRect;
    }

    private void ChooseItemMenu(FloatMenuMap menu, string prefix)
    {
        List<FloatMenuOption> options = (List<FloatMenuOption>)AccessTools.Field(typeof(FloatMenu), "options").GetValue(menu)!;
        FloatMenuOption option = options.FirstOrDefault(option => option.Label.StartsWith(prefix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Native menu option missing: " + prefix + " / " + string.Join(" | ", options.Select(option => option.Label)));
        Check(!option.Disabled, "native item menu option is enabled before the click: " + option.Label);
        float top = options.TakeWhile(item => item != option).Sum(item => item.RequiredHeight - 1f);
        Vector2 point = new(menu.windowRect.x + Mathf.Min(120f, menu.windowRect.width / 2f), menu.windowRect.y + top + option.RequiredHeight / 2f);
        FireItemMenu(menu, point, EventType.Repaint);
        File.AppendAllText(Path.Combine(Bootstrap.Output, "ItemMenus.txt"), prefix + " disabled=" + option.Disabled
            + " focused=" + CQFMapProjectionContext.Focused?.id + " point=" + point + " window=" + Find.WindowStack.GetWindowAt(point)?.GetType().Name + "\n");
        FireItemMenu(menu, point, EventType.MouseDown);
        FireItemMenu(menu, point, EventType.MouseUp);
        File.AppendAllText(Path.Combine(Bootstrap.Output, "ItemMenus.txt"), "after click disabled=" + option.Disabled
            + " focused=" + CQFMapProjectionContext.Focused?.id + " selected=" + Find.Selector.SingleSelectedThing + "\n");
    }

    private void FireItemMenu(FloatMenuMap menu, Vector2 point, EventType type)
    {
        Event before = Event.current;
        try
        {
            SetPointer(point, type);
            GUI.BeginGroup(menu.windowRect);
            try { AccessTools.Method(typeof(Window), "InnerWindowOnGUI").Invoke(menu, new object[] { 0 }); }
            finally { GUI.EndGroup(); }
        }
        finally { Bootstrap.MousePosition = null; Event.current = before; }
    }
}
