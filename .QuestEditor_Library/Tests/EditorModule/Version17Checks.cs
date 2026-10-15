using System.Reflection;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;

internal static class Version17Checks
{
    public static void Run(string game)
    {
        Assembly framework = typeof(CustomMapDataDef).Assembly;
        Assembly editor = Assembly.LoadFrom(Path.Combine(game, "Mods", "CQF", "1.7", "Optional", "Editor", "CQF.Editor.dll"));
        foreach (Assembly assembly in new[] { framework, editor })
        {
            Type[] types = assembly.GetTypes();
            Console.WriteLine("PASS types load: " + assembly.GetName().Name + " (" + types.Length + ")");
            Harmony harmony = new Harmony("CQF.Checks.Version17." + assembly.GetName().Name);
            int patched = 0;
            foreach (Type type in types.Where(type => type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0))
            {
                try
                {
                    var methods = harmony.CreateClassProcessor(type).Patch();
                    if (methods == null || methods.Count == 0) throw new InvalidOperationException("No patched methods: " + type.FullName);
                    patched += methods.Count;
                    Console.WriteLine("PASS patch: " + type.Name + " (" + methods.Count + ")");
                }
                catch (Exception error)
                {
                    throw new InvalidOperationException("1.7 patch failed: " + type.FullName, error);
                }
            }
            Console.WriteLine("PASS Harmony registration: " + assembly.GetName().Name + " (" + patched + ")");
        }
        if (framework.GetType("QuestEditor_Library.CustomSitePartParams") != null
            || framework.GetType("QuestEditor_Library.Patch_SitePartParams") != null
            || typeof(ComplexPawnDef).GetMethod("LoadModData") != null
            || typeof(PawnModWorker).GetMethod("LoadData") != null)
            throw new InvalidOperationException("Old compatibility API remains.");
        if (typeof(Site).GetField("allEnemiesDefeatedSignalSent", BindingFlags.NonPublic | BindingFlags.Instance) == null)
            throw new MissingFieldException(typeof(Site).FullName, "allEnemiesDefeatedSignalSent");
        Console.WriteLine("PASS old compatibility APIs removed and site field verified");
        if (framework.GetType("QuestEditor_Library.Patch_MapHibernationExecution") != null)
            throw new InvalidOperationException("Global hibernation execution patch remains.");
        foreach (MethodBase method in new[]
        {
            AccessTools.Method(typeof(Verse.Game), nameof(Verse.Game.UpdatePlay)),
            AccessTools.Method(typeof(Verse.TickManager), nameof(Verse.TickManager.DoSingleTick))
        })
        {
            if (Harmony.GetPatchInfo(method)?.Owners.Contains("CQF.Checks.Version17.QuestEditor_Library") == true)
                throw new InvalidOperationException("CQF still patches " + method.Name);
        }
        Console.WriteLine("PASS framework registers no UpdatePlay or DoSingleTick patches");
        AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
        {
            string? name = new AssemblyName(request.Name).Name;
            if (name?.StartsWith("UnityEngine", StringComparison.Ordinal) == true) return typeof(UnityEngine.Rect).Assembly;
            string? path = name switch
            {
                "AllsparkCrisis" => Path.Combine(game, "Mods", "火种危机", "1.7", "Assemblies", "AllsparkCrisis.dll"),
                "SimManagementLib" => Path.GetFullPath(Path.Combine(game, "..", "..", "workshop", "content", "294100", "3736621496", "1.6", "Assemblies", "SimManagementLib.dll")),
                _ => null
            };
            return path != null && File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        Assembly allspark = Assembly.LoadFrom(Path.Combine(game, "Mods", "CQF", "1.7", "Mod", "AllsparkCrisis", "Assemblies", "CQFAndRS.dll"));
        allspark.GetTypes();
        CQFAction_Target infection = (CQFAction_Target)Activator.CreateInstance(allspark.GetType("CQFAndRS.CQFAction_InfectTarget", true)!)!;
        infection.RealWork(new Dictionary<string, Verse.TargetInfo>(), null!);
        Console.WriteLine("PASS Allspark adapter loads and resolves infection API");
        Assembly rimsim = Assembly.LoadFrom(Path.Combine(game, "Mods", "CQF", "1.7", "Mod", "RimSimManagementFramework", "Assemblies", "CQF_RSMF.dll"));
        rimsim.GetTypes();
        var adapterPatches = new Harmony("CQF.Checks.Version17.RimSim").CreateClassProcessor(rimsim.GetType("CQF_RSMF.PatchCustomerArrivalManager", true)!).Patch();
        if (adapterPatches == null || adapterPatches.Count != 2) throw new InvalidOperationException("RimSim customer hooks did not register.");
        Console.WriteLine("PASS RimSim adapter registers both private customer generation hooks");
    }
}
