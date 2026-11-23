using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
internal static class Patch_CompositeMapColonistBar
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = instructions.ToList();
        int mapsIndex = code.FindIndex(instruction => instruction.Calls(findMaps));
        if (mapsIndex < 0 || code.FindLastIndex(instruction => instruction.Calls(findMaps)) != mapsIndex)
            throw new InvalidOperationException("[CQF] Expected one native colonist bar map lookup.");
        code[mapsIndex].operand = mapsForColonistBar;

        int index = code.FindIndex(instruction => instruction.Calls(calculateDrawLocs));
        if (index < 0 || code.FindLastIndex(instruction => instruction.Calls(calculateDrawLocs)) != index)
            throw new InvalidOperationException("[CQF] Expected one native colonist bar layout call.");

        CodeInstruction call = code[index];
        CodeInstruction loadEntries = new(OpCodes.Ldarg_0);
        loadEntries.labels.AddRange(call.labels);
        loadEntries.blocks.AddRange(call.blocks);
        call.labels.Clear();
        call.blocks.Clear();
        code.InsertRange(index, new[]
        {
            loadEntries,
            new CodeInstruction(OpCodes.Ldfld, cachedEntries),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, cachedReorderableGroups)
        });
        call.opcode = OpCodes.Call;
        call.operand = calculateWithCompositeEntries;
        return code;
    }

    private static List<Map> MapsForColonistBar()
    {
        List<Map> maps = Find.Maps;
        if (GameComponent_MapManager.Instance is not GameComponent_MapManager manager || manager.CompositeMaps.Count == 0)
            return maps;

        List<Map> visible = new(maps.Count);
        foreach (Map map in maps)
            if (manager.GetCompositeMap(map) is not CQFCompositeMapInstance instance || instance.EntryMap == map)
                visible.Add(map);
        return visible;
    }

    private static void CalculateWithCompositeEntries(ColonistBarDrawLocsFinder finder, List<Vector2> drawLocs,
        out float scale, int groupCount, List<ColonistBar.Entry> entries, List<int> reorderableGroups)
    {
        if (Find.PlaySettings.showColonistBar && GameComponent_MapManager.Instance is GameComponent_MapManager manager)
        {
            foreach (CQFCompositeMapInstance instance in manager.CompositeMaps)
            {
                if (instance.EntryMap is not { Disposed: false } map || !manager.IsHibernating(map) ||
                    entries.Exists(entry => entry.map == map))
                    continue;
                entries.Add(new ColonistBar.Entry(null, map, groupCount++));
                reorderableGroups.Add(-1);
            }
        }

        finder.CalculateDrawLocs(drawLocs, out scale, groupCount);
    }

    private static readonly MethodInfo findMaps = AccessTools.PropertyGetter(typeof(Find), nameof(Find.Maps));
    private static readonly MethodInfo mapsForColonistBar = AccessTools.Method(typeof(Patch_CompositeMapColonistBar),
        nameof(MapsForColonistBar));
    private static readonly MethodInfo calculateDrawLocs = AccessTools.Method(typeof(ColonistBarDrawLocsFinder),
        nameof(ColonistBarDrawLocsFinder.CalculateDrawLocs), new[] { typeof(List<Vector2>), typeof(float).MakeByRefType(), typeof(int) });
    private static readonly MethodInfo calculateWithCompositeEntries = AccessTools.Method(typeof(Patch_CompositeMapColonistBar),
        nameof(CalculateWithCompositeEntries));
    private static readonly FieldInfo cachedEntries = AccessTools.Field(typeof(ColonistBar), "cachedEntries");
    private static readonly FieldInfo cachedReorderableGroups = AccessTools.Field(typeof(ColonistBar), "cachedReorderableGroups");
}
