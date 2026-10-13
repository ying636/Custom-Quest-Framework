using System.Reflection;
using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CQF.RuntimeChecks;

public sealed class Version17RuntimeChecksDriver : MonoBehaviour
{
    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting) return;
        try
        {
            if (Time.realtimeSinceStartup > 180f) throw new TimeoutException("CQF 1.7 map checks timed out: " + stage);
            if (stage == 0)
            {
                if (Current.Game?.CurrentMap == null) return;
                bool disabled = File.Exists(Path.Combine(GenFilePaths.SaveDataFolderPath, "disabled"));
                Check(CQFEditorBridge.IsLoaded == !disabled, "editor setting controls native startup");
                Check(typeof(CustomMapDataDef).Assembly.GetType("QuestEditor_Library.CustomSitePartParams") == null, "old site parameters absent in game");
                if (disabled)
                {
                    string[] errors = LanguageDatabase.activeLanguage.loadErrors
                        .Concat(LanguageDatabase.activeLanguage.defInjections.SelectMany(package => package.loadErrors)).ToArray();
                    if (errors.Length > 0) File.WriteAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath, "TranslationErrors.txt"), errors);
                    Check(errors.Length == 0, "language files load without translation errors");
                    Finish(true);
                    return;
                }
                sourceMap = Current.Game.CurrentMap;
                definition = new CustomMapDataDef { defName = "CQF_Runtime17_Map", size = new IntVec3(64, 1, 64) };
                definition.terrainsRect.Add("Concrete", new List<CellRect> { new CellRect(0, 0, 64, 64) });
                definition.thingDatas.Add(new ThingData
                {
                    def = ThingDefOf.Wall, stuff = ThingDefOf.BlocksGranite, position = new IntVec3(12, 0, 12)
                });
                DefDatabase<CustomMapDataDef>.Add(definition);
                Type editor = GenTypes.GetTypeInAnyAssembly("QuestEditor_Library.QuestEditor_CustomQuestMap");
                editor.GetMethod("GenerateMap", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { definition.size, definition, false });
                stage = 1;
            }
            else if (stage == 1)
            {
                Map editorMap = Current.Game.CurrentMap;
                if (editorMap == sourceMap) return;
                CheckMap(editorMap, "editor");
                Check(editorMap.Parent is EditorMapObject parent && parent.mapDef == definition, "editor parent owns map definition");
                definition!.GenerateAsSubmap(sourceMap!, new IntVec3(32, 0, 32), null!, null!);
                Map subMap = sourceMap!.GetComponent<MapComponent_CustomMapData>().subMaps.Last().Map;
                CheckMap(subMap, "submap");
                Check(subMap.Parent is MapParent_Custom pocket && pocket.mapDataDef == definition, "submap parent owns map definition");
                CustomSite site = QuestNode_Root_CustomMap.MakeCustomSite(
                    Gen.YieldSingle(new SitePartDefWithParams(QEDefOf.QE_CustomSite, new SitePartParams())),
                    TileFinder.RandomSettlementTileFor(Faction.OfPlayer), Faction.OfPlayer);
                site.mapDef = definition;
                site.replaceMapGeneration = true;
                Find.WorldObjects.Add(site);
                LongEventHandler.QueueLongEvent(() => siteMap = MapGenerator.GenerateMap(definition.size, site, site.MapGeneratorDef, site.ExtraGenStepDefs),
                    "GeneratingMap", true, error => { failure = error; });
                stage = 2;
            }
            else if (stage == 2)
            {
                if (failure != null) throw failure;
                if (siteMap == null) return;
                CheckMap(siteMap, "site");
                Check(siteMap.Parent is CustomSite site && site.mapDef == definition, "site parent owns map definition");
                Finish(true);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "Version17Checks.txt"), "FAIL " + error + "\n");
            Log.Error("CQF 1.7 runtime checks: " + error);
            Finish(false);
        }
    }

    private void CheckMap(Map map, string kind)
    {
        Check(map.Size == new IntVec3(64, 1, 64), kind + " map size");
        Check(map.terrainGrid.TerrainAt(new IntVec3(10, 0, 10)).defName == "Concrete", kind + " terrain generated");
        Check(map.thingGrid.ThingsListAt(new IntVec3(12, 0, 12)).Any(thing => thing.def == ThingDefOf.Wall && thing.Stuff == ThingDefOf.BlocksGranite), kind + " building generated");
    }

    private void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "Version17Checks.txt"), "PASS " + name + "\n");
    }

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "Version17Checks.txt"), success ? "COMPLETE\n" : "FAILED\n");
        Application.Quit(success ? 0 : 1);
    }

    private Map? sourceMap;
    private Map? siteMap;
    private CustomMapDataDef? definition;
    private Exception? failure;
    private int stage;
    private bool finished;
}
