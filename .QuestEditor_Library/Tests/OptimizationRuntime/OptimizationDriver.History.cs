using System.Text.Json;
using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.OptimizationChecks;

public sealed partial class OptimizationDriver
{
    private void CheckHistory(bool reloaded)
    {
        string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "HistoryExpected.json");
        string Snapshot() => JsonSerializer.Serialize(new
        {
            Things = Designator_SpawnThing.RecentSelections.Select(item => item.Thing.defName + "/" + item.Stuff?.defName).ToArray(),
            Tools = Designator_CQFTools.RecentSelections.Select(item => item.Thing.defName + "/" + item.Stuff?.defName).ToArray(),
            Terrains = Designator_Terrain.RecentSelections.Select(item => item.defName).ToArray()
        });
        if (reloaded)
        {
            Check(File.ReadAllText(path) == Snapshot(), "all three floating palettes restore the same order and materials in a new game process");
            return;
        }
        Designator_SpawnThing spawn = new();
        ThingDef[] choices = Designator_SpawnThing.Bespawnable.Where(def => !def.MadeFromStuff).Take(7).ToArray();
        foreach (ThingDef def in choices) spawn.SelectThing(def, null!);
        spawn.SelectThing(choices[3], null!);
        Check(Designator_SpawnThing.RecentSelections.Count == 5 && Designator_SpawnThing.RecentSelections[0].Thing == choices[3], "recent things keep five entries and move duplicate selections to the front");
        ThingDef made = Designator_SpawnThing.Bespawnable.First(def => def.MadeFromStuff && GenStuff.AllowedStuffsFor(def).Take(2).Count() == 2);
        foreach (ThingDef stuff in GenStuff.AllowedStuffsFor(made).Take(2)) spawn.SelectThing(made, stuff);
        Check(Designator_SpawnThing.RecentSelections.Count(item => item.Thing == made) == 2, "different materials remain distinct recent thing selections");
        Designator_CQFTools tools = new();
        foreach (ThingDef def in Designator_CQFTools.Basespawnable.Take(3)) tools.SelectThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
        Designator_Terrain terrain = new();
        foreach (TerrainDef def in DefDatabase<TerrainDef>.AllDefsListForReading.Take(7)) terrain.SelectTerrain(def);
        Check(Designator_Terrain.RecentSelections.Count == 5 && Designator_CQFTools.RecentSelections.Count > 0, "terrain and CQF tool palettes keep their own histories");
        Find.DesignatorManager.Deselect();
        File.WriteAllText(path, Snapshot());
        Check(Directory.GetFiles(Path.Combine(GenFilePaths.SaveDataFolderPath, "Config"), "Mod_*CustomQuestFramework_Mod.xml").Length == 1, "palette selections are saved to the mod settings file");
    }
}
