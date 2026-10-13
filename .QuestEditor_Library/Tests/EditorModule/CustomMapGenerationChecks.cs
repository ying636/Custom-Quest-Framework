using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;
using Verse;

internal static class CustomMapGenerationChecks
{
    public static void Run()
    {
        Harmony harmony = new Harmony("CQF.Checks.MapGeneration");
        var method = typeof(GenStep_CustomMap).GetMethod(nameof(GenStep_CustomMap.SpawnCustomMap));
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(CustomMapGenerationChecks).GetMethod(nameof(Capture), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)));
        try
        {
            CustomMapDataDef definition = new CustomMapDataDef { defName = "CQF_Check_Map", destroyAllThing = true };
            Quest quest = new Quest();
            foreach (MapParent parent in new MapParent[]
            {
                new CustomSite { mapDef = definition, quest = quest, replaceMapGeneration = true },
                new MapParent_Custom { mapDataDef = definition, quest = quest },
                new EditorMapObject { mapDef = definition }
            })
            {
                captured = null;
                new GenStep_CustomMap().Generate(new Map { info = new MapInfo { parent = parent } }, new GenStepParams());
                if (!captured.HasValue || captured.Value.definition != definition || !captured.Value.destroy)
                    throw new InvalidOperationException("Map generation lost parent definition: " + parent.GetType().Name);
                bool editor = parent is EditorMapObject;
                if (captured.Value.quest != (editor ? null : quest) || captured.Value.load != editor
                    || captured.Value.subMap != (parent is MapParent_Custom) || captured.Value.replace != (parent is CustomSite))
                    throw new InvalidOperationException("Map generation lost parent options: " + parent.GetType().Name);
                Console.WriteLine("PASS parent map generation with empty native parameters: " + parent.GetType().Name);
            }
            QEDefOf.QE_CustomSite_GenStep = new GenStepDef { defName = "CQF_Check_GenStep" };
            GenStepDef geysers = new GenStepDef { defName = "SteamGeysers" };
            IEnumerable<GenStepWithParams> customSteps = new[]
            {
                new GenStepWithParams(QEDefOf.QE_CustomSite_GenStep, new GenStepParams()),
                new GenStepWithParams(geysers, new GenStepParams())
            };
            Patch_ExtraGenStepDefs.postfix(ref customSteps);
            if (customSteps.Any(step => step.def == geysers)) throw new InvalidOperationException("Custom map kept native geysers.");
            IEnumerable<GenStepWithParams> ordinarySteps = new[] { new GenStepWithParams(geysers, new GenStepParams()) };
            Patch_ExtraGenStepDefs.postfix(ref ordinarySteps);
            if (!ordinarySteps.Any(step => step.def == geysers)) throw new InvalidOperationException("Ordinary map lost native geysers.");
            Console.WriteLine("PASS custom generator filtering preserves ordinary maps");
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }
    }

    private static bool Capture(CustomMapDataDef def, Quest quest, bool load, bool isSubMap, bool destroyThings, bool replaceMap, ref List<Thing> __result)
    {
        captured = (def, quest, load, isSubMap, destroyThings, replaceMap);
        __result = new List<Thing>();
        return false;
    }

    private static (CustomMapDataDef definition, Quest quest, bool load, bool subMap, bool destroy, bool replace)? captured;
}
