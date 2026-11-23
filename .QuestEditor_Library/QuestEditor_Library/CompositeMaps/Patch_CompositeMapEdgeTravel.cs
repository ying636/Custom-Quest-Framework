using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
internal static class Patch_CompositeMapEdgeTravel
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = instructions.ToList();
        int move = code.FindIndex(instruction => instruction.Calls(setPosition));
        if (move < 0 || code.FindLastIndex(instruction => instruction.Calls(setPosition)) != move)
            throw new InvalidOperationException("[CQF] Expected one pawn position assignment in the native path step.");
        code.InsertRange(move + 1, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, pathPawn),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, lastCell),
            new CodeInstruction(OpCodes.Call, onMoved)
        });
        return code;
    }

    private static void OnMoved(Pawn pawn, IntVec3 previous)
    {
        if (pawn.Map is not { Parent: CQFCompositeMapParent } map) return;
        IntVec3 cell = pawn.Position;
        if (cell == previous) return;
        if (cell.x != 0 && cell.z != 0 && cell.x != map.Size.x - 1 && cell.z != map.Size.z - 1) return;
        if (!pawn.IsColonistPlayerControlled) return;
        GameComponent_MapManager.Instance?.QueueCompositeEdgeTravel(pawn, map, cell);
    }

    private static readonly MethodInfo setPosition = AccessTools.PropertySetter(typeof(Thing), nameof(Thing.Position));
    private static readonly FieldInfo pathPawn = AccessTools.Field(typeof(Pawn_PathFollower), "pawn");
    private static readonly FieldInfo lastCell = AccessTools.Field(typeof(Pawn_PathFollower), "lastCell");
    private static readonly MethodInfo onMoved = AccessTools.Method(typeof(Patch_CompositeMapEdgeTravel), nameof(OnMoved));
}
