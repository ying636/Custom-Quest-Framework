using System.Reflection;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class TargetKeySaveChecks
{
    public static void Run(Map map, Thing thing, string key, string directory)
    {
        string path = Path.Combine(directory, "CQF_Check_TargetKeySave.xml");
        MapComponent_CQFTargets? component = map.GetComponent<MapComponent_CQFTargets>();
        try
        {
            Scribe.saver.InitSaving(path, "savegame");
            Scribe_Deep.Look(ref component, "component", map);
            Scribe.saver.FinalizeSaving();
            XDocument saved = XDocument.Load(path);
            Check(saved.Descendants("key").Any(node => node.Value == key)
                && saved.Descendants("target").Any(node => node.Value.Contains(thing.GetUniqueLoadID(), StringComparison.Ordinal)),
                "native save writes the target key and the actual Thing reference");
            MapComponent_CQFTargets? loaded = null;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref loaded, "component", map);
            LoadedObjectDirectory objects = (LoadedObjectDirectory)typeof(CrossRefHandler)
                .GetField("loadedObjectDirectory", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Scribe.loader.crossRefs)!;
            objects.RegisterLoaded(thing);
            Scribe.loader.FinalizeLoading();
            Check(loaded != null && !ReferenceEquals(component, loaded) && ReferenceEquals(loaded.GetTarget(key).Thing, thing),
                "native load restores the target key to the same Thing rather than a name or cell lookup");
            Check(loaded!.GetKeys(thing).Contains(key), "loaded bindings support target-key lookup for linked-object display");
        }
        finally
        {
            if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }
}
