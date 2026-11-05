using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using Verse;
using Verse.AI;

internal static class PawnSpawnDefaultDutyChecks
{
    public static void Run()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        ModContentPack content = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        content.assemblies = new ModAssemblyHandler(content);
        content.assemblies.loadedAssemblies.Add(typeof(PawnSpawnData).Assembly);
        LoadedModManager.RunningModsListForReading.Add(content);
        GenTypes.ClearCache();
        Harmony harmony = new("CQF.Checks.PawnSpawnDefaultDuty");
        MethodInfo initialization = AccessTools.Method(typeof(DefOfHelper), nameof(DefOfHelper.EnsureInitializedInCtor));
        harmony.Patch(initialization, prefix: new HarmonyMethod(typeof(PawnSpawnDefaultDutyChecks), nameof(RecordDefOfInitialization)));
        try
        {
            PawnSpawnData[] constructed = { new(), new PawnSpawnData_Faction(), new PawnSpawnData_Group(), new PawnSpawnData_Random(), new PawnSpawnData_ComplexPawn() };
            Check(dutyDefOfInitializations == 0, "spawn-data constructors do not initialize DutyDefOf before defs load");
            XmlDocument document = new();
            document.LoadXml("<QuestEditor_Library.CustomMapDataDef><defName>CQF_Check_EarlyPawnSpawn</defName><pawns><li><key>(2, 0, 3)</key><value><li /><li Class=\"QuestEditor_Library.PawnSpawnData_Faction\"><dataName>CQF_Check_FactionSpawn</dataName></li></value></li></pawns></QuestEditor_Library.CustomMapDataDef>");
            CustomMapDataDef map = DirectXmlToObject.ObjectFromXml<CustomMapDataDef>(document.DocumentElement!, false);
            List<PawnSpawnData> nested = map.pawns[new IntVec3(2, 0, 3)];
            Check(nested.Count == 2 && nested[1] is PawnSpawnData_Faction, "native XML creates nested spawn-data dictionary and list entries: " + string.Join(", ", nested.Select(data => data.GetType().Name)));
            Check(dutyDefOfInitializations == 0, "native nested XML loading leaves DutyDefOf uninitialized");
            Check(constructed.Concat(nested).All(data => data.duty == null), "omitted duties remain unresolved during early loading");

            DutyDef explicitDuty = new() { defName = "CQF_Check_ExplicitSpawnDuty" };
            DefDatabase<DutyDef>.Add(explicitDuty);
            document.LoadXml("<li><enableLord>true</enableLord><duty>CQF_Check_ExplicitSpawnDuty</duty></li>");
            PawnSpawnData configured = DirectXmlToObject.ObjectFromXml<PawnSpawnData>(document.DocumentElement!, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            Check(configured.duty == explicitDuty && dutyDefOfInitializations == 0, "explicit XML duties resolve without consulting DutyDefOf");

            DutyDef defend = new() { defName = "Defend" };
            DefDatabase<DutyDef>.Add(defend);
            FieldInfo binding = AccessTools.Field(typeof(DefOfHelper), "bindingNow");
            bool previousBinding = (bool)binding.GetValue(null)!;
            binding.SetValue(null, true);
            try
            {
                typeof(DutyDefOf).GetField(nameof(DutyDefOf.Defend))!.SetValue(null, defend);
            }
            finally
            {
                binding.SetValue(null, previousBinding);
            }

            Check(constructed.Concat(nested).All(data => data.Duty == defend), "all spawn-data variants obtain Defend after binding");
            Check(configured.Duty == explicitDuty, "configured duties take precedence over the runtime default");
            PawnSpawnData defaults = nested[0];
            defaults.enableLord = true;
            Check(defaults.SaveToXElement("li").Element("duty")?.Value == "Defend", "export writes the effective default duty");
            Check(defaults.Copy().duty == defend, "default duty survives native XML copy and cross-reference resolution");
            Check(configured.Copy().duty == explicitDuty, "explicit duty survives native XML copy and cross-reference resolution");
            configured.duty = null;
            Check(configured.Duty == defend, "clearing a duty restores the default");
            Console.WriteLine("Pawn spawn default-duty checks passed.");
        }
        finally
        {
            harmony.Unpatch(initialization, HarmonyPatchType.Prefix, harmony.Id);
            LoadedModManager.RunningModsListForReading.Remove(content);
            GenTypes.ClearCache();
        }
    }

    private static void RecordDefOfInitialization(Type defOf)
    {
        if (defOf == typeof(DutyDefOf)) dutyDefOfInitializations++;
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }

    private static int dutyDefOfInitializations;
}
