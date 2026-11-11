using System.Runtime.CompilerServices;
using HarmonyLib;
using QuestEditor_Library;
using Verse;

internal static class DomainOpeningReasonChecks
{
    public static void Run()
    {
        Game previous = Current.Game;
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        Map source = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        Map other = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        source.components = new() { new MapComponent_Domain(source) };
        other.components = new() { new MapComponent_Domain(other) };
        AccessTools.Field(typeof(Game), "maps").SetValue(game, new List<Map> { source, other });
        Current.Game = game;
        try
        {
            Pawn pawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
            AccessTools.Field(typeof(Thing), "mapIndexOrState").SetValue(pawn, (sbyte)-1);
            pawn.Position = new IntVec3(24, 0, 0);
            AccessTools.Field(typeof(Thing), "mapIndexOrState").SetValue(pawn, (sbyte)0);
            CQFDomainRuntime runtime = new(new GameComponent_MapManager(game));
            Check(runtime, pawn, null);
            DomainMaterialTransition transition = (DomainMaterialTransition)RuntimeHelpers.GetUninitializedObject(typeof(DomainMaterialTransition));
            AccessTools.Field(typeof(CQFDomainRuntime), "transition").SetValue(runtime, transition);
            Check(runtime, pawn, "CQF_DomainTransitionInProgress");
            AccessTools.Field(typeof(DomainTransition), "<IsReturning>k__BackingField").SetValue(transition, true);
            Check(runtime, pawn, "CQF_DomainReturnInProgress");
            AccessTools.Field(typeof(DomainTransition), "<IsReturning>k__BackingField").SetValue(transition, false);
            ReasonProbeOpening opening = new() { Position = IntVec3.Zero };
            AccessTools.Field(typeof(Thing), "mapIndexOrState").SetValue(opening, (sbyte)0);
            AccessTools.Field(typeof(DomainTransition), "opening").SetValue(transition, opening);
            Check(runtime, pawn, null);
            AccessTools.Field(typeof(Thing), "mapIndexOrState").SetValue(pawn, (sbyte)-1);
            pawn.Position = new IntVec3(25, 0, 0);
            AccessTools.Field(typeof(Thing), "mapIndexOrState").SetValue(pawn, (sbyte)0);
            Check(runtime, pawn, "CQF_DomainOpeningOutOfRange");
            AccessTools.Field(typeof(Thing), "mapIndexOrState").SetValue(pawn, (sbyte)1);
            Check(runtime, pawn, "CQF_DomainTransitionOtherMap");
            AccessTools.Field(typeof(Thing), "mapIndexOrState").SetValue(pawn, (sbyte)0);
            opening.caster = pawn;
            Check(runtime, pawn, "CQF_DomainAlreadyOpening");
            opening.caster = null;
            opening.supportsFusion = false;
            Check(runtime, pawn, "CQF_DomainTransitionInProgress");
            opening.supportsFusion = true;
            opening.active = false;
            Check(runtime, pawn, "CQF_DomainTransitionInProgress");
            opening.active = true;
            AccessTools.Property(typeof(DomainOpening), "Fusion").SetValue(opening, RuntimeHelpers.GetUninitializedObject(typeof(DomainOpening_Fusion)));
            Check(runtime, pawn, "CQF_DomainTransitionInProgress");
            AccessTools.Property(typeof(DomainOpening), "Fusion").SetValue(opening, null);
            AccessTools.Field(typeof(DomainTransition), "opening").SetValue(transition, RuntimeHelpers.GetUninitializedObject(typeof(DomainOpening_Fusion)));
            Check(runtime, pawn, "CQF_DomainTransitionInProgress");
            AccessTools.Field(typeof(DomainTransition), "opening").SetValue(transition, opening);
            AccessTools.Field(typeof(MapComponent_Domain), "active").SetValue(source.components[0], true);
            Check(runtime, pawn, "CQF_DomainTransitionInProgress");
        }
        finally { Current.Game = previous; }
        Console.WriteLine("Domain opening reason checks passed: " + checks);
    }

    private static void Check(CQFDomainRuntime runtime, Pawn pawn, string? expected)
    {
        bool accepted = runtime.CanBeginOpening(pawn, 12f, out string key);
        if (accepted != (expected == null) || key != (expected ?? string.Empty)
            || runtime.CanBeginOpening(pawn, 12f) != accepted || runtime.CanBeginOpening(pawn) != accepted)
            throw new InvalidOperationException("Unexpected opening reason: " + key + ", expected=" + expected);
        checks++;
    }

    private static int checks;
}
