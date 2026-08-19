using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class BlueprintRepository
    {
        public static int Version
        {
            get
            {
                EnsureCurrentGame();
                return version;
            }
        }

        public static IReadOnlyList<CustomMapDataDef> AllBlueprints
        {
            get
            {
                EnsureCurrentGame();
                List<CustomMapDataDef> result = new List<CustomMapDataDef>();
                HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (CustomMapDataDef def in temporaryBlueprints)
                {
                    if (def != null && names.Add(def.defName ?? string.Empty))
                    {
                        result.Add(def);
                    }
                }
                foreach (CustomMapDataDef def in importedBlueprints)
                {
                    if (def != null && names.Add(def.defName ?? string.Empty))
                    {
                        result.Add(def);
                    }
                }
                return result;
            }
        }

        public static bool IsTemporary(CustomMapDataDef def)
        {
            EnsureCurrentGame();
            return def != null && temporaryBlueprints.Contains(def);
        }

        public static bool IsImported(CustomMapDataDef def)
        {
            EnsureCurrentGame();
            return def != null && importedBlueprints.Contains(def);
        }

        public static void AddTemporary(CustomMapDataDef def)
        {
            EnsureCurrentGame();
            if (def == null)
            {
                Log.Error("Cannot add a null custom map blueprint.");
                return;
            }
            if (def.defName.NullOrEmpty())
            {
                def.defName = "CQF_Blueprint_" + Find.TickManager.TicksGame;
            }
            if (def.label.NullOrEmpty())
            {
                def.label = def.defName;
            }
            if (!temporaryBlueprints.Contains(def))
            {
                temporaryBlueprints.Add(def);
                version++;
            }
        }

        public static void RemoveTemporary(CustomMapDataDef def)
        {
            EnsureCurrentGame();
            if (def != null && temporaryBlueprints.Remove(def))
            {
                version++;
            }
        }

        public static bool ContainsDefName(string defName)
        {
            EnsureCurrentGame();
            if (defName.NullOrEmpty())
            {
                return false;
            }
            return temporaryBlueprints.Any(def => string.Equals(def.defName, defName, StringComparison.OrdinalIgnoreCase))
                || importedBlueprints.Any(def => string.Equals(def.defName, defName, StringComparison.OrdinalIgnoreCase))
                || DefDatabase<CustomMapDataDef>.AllDefsListForReading.Any(def =>
                    string.Equals(def.defName, defName, StringComparison.OrdinalIgnoreCase));
        }

        public static void CreateFromMap(Map map, List<IntVec3> cells, Action<CustomMapDataDef> savedAction = null)
        {
            EnsureCurrentGame();
            if (map == null)
            {
                Messages.Message("CQF_NoCurrentMap".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            cells.RemoveAll(cell => !cell.InBounds(map));
            if (cells.Count == 0)
            {
                Messages.Message("CQF_BlueprintEmptySelection".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            int minX = cells.Min(cell => cell.x);
            int maxX = cells.Max(cell => cell.x);
            int minZ = cells.Min(cell => cell.z);
            int maxZ = cells.Max(cell => cell.z);
            int number = GetNextBlueprintNumber();
            CustomMapDataDef blueprint = new CustomMapDataDef
            {
                defName = "CQF_Blueprint_" + number,
                label = "CQF_BlueprintAutoName".Translate(number),
                isPart = true,
                destroyAllThing = false,
                rot = Rot4.North
            };
            IntVec3 size = new IntVec3(maxX - minX + 1, 1, maxZ - minZ + 1);
            List<IntVec3> savedCells = cells.ListFullCopy();
            LongEventHandler.QueueLongEvent(() =>
            {
                try
                {
                    blueprint.LoadData(map, savedCells, size);
                    AddTemporary(blueprint);
                    if (savedAction != null)
                    {
                        LongEventHandler.ExecuteWhenFinished(() => savedAction(blueprint));
                    }
                    Messages.Message("CQF_BlueprintSavedInMemory".Translate(blueprint.label),
                        MessageTypeDefOf.PositiveEvent);
                }
                catch (Exception exception)
                {
                    Log.Error("Save blueprint in memory failed: " + exception);
                    Messages.Message("CQF_BlueprintSaveFailed".Translate(exception.Message),
                        MessageTypeDefOf.RejectInput);
                }
            }, "CQF_SaveBlueprint".Translate(), true, exception =>
            {
                Log.Error("Queue blueprint save failed: " + exception);
                Messages.Message("CQF_BlueprintSaveFailed".Translate(exception.Message),
                    MessageTypeDefOf.RejectInput);
            });
        }

        public static void ConfirmImportLoadedBlueprints()
        {
            EnsureCurrentGame();
            List<CustomMapDataDef> candidates = DefDatabase<CustomMapDataDef>.AllDefsListForReading
                .Where(def => def != null && !temporaryBlueprints.Contains(def) && !importedBlueprints.Contains(def))
                .OrderBy(def => def.label ?? def.defName)
                .ToList();
            if (candidates.Count == 0)
            {
                Messages.Message("CQF_NoLoadedMapsToImport".Translate(), MessageTypeDefOf.NeutralEvent);
                return;
            }
            Find.WindowStack.Add(new Window_SelectBlueprintImports(candidates));
        }

        public static void ImportLoadedBlueprints(IEnumerable<CustomMapDataDef> selectedBlueprints)
        {
            EnsureCurrentGame();
            if (selectedBlueprints == null)
            {
                Log.Error("Cannot import blueprints from a null selection.");
                Messages.Message("CQF_NoBlueprintsSelectedForImport".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            int importedCount = 0;
            foreach (CustomMapDataDef def in selectedBlueprints.Distinct())
            {
                if (def == null || temporaryBlueprints.Contains(def) || importedBlueprints.Contains(def))
                {
                    continue;
                }
                importedBlueprints.Add(def);
                importedCount++;
            }
            if (importedCount == 0)
            {
                Messages.Message("CQF_NoBlueprintsSelectedForImport".Translate(), MessageTypeDefOf.NeutralEvent);
                return;
            }
            version++;
            Messages.Message("CQF_ImportedLoadedMaps".Translate(importedCount), MessageTypeDefOf.PositiveEvent);
        }

        public static void Delete(CustomMapDataDef def)
        {
            EnsureCurrentGame();
            if (def == null)
            {
                return;
            }
            temporaryBlueprints.Remove(def);
            importedBlueprints.Remove(def);
            BlueprintPreviewCache.Remove(def);
            version++;
        }

        public static Texture2D GetIcon(CustomMapDataDef def)
        {
            ThingData thing = def?.thingDatas.FirstOrDefault();
            if (thing?.def != null)
            {
                return thing.def.GetUIIconForStuff(thing.stuff) ?? TexButton.Copy;
            }
            string terrainName = def?.terrains.Keys.FirstOrDefault();
            TerrainDef terrain = terrainName.NullOrEmpty() ? null : DefDatabase<TerrainDef>.GetNamedSilentFail(terrainName);
            return terrain?.uiIcon ?? TexButton.Copy;
        }

        public static void ExportToXml(CustomMapDataDef def)
        {
            if (def == null || def.defName.NullOrEmpty())
            {
                Messages.Message("CQF_BlueprintInvalidName".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            string directory = Path.Combine(Page_QuestEditor.Path, "Map");
            string path = Path.Combine(directory, def.defName + ".xml");
            if (File.Exists(path))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "CQF_BlueprintOverwriteXml".Translate(path), () => QueueExport(def, directory, path)));
                return;
            }
            QueueExport(def, directory, path);
        }

        private static void QueueExport(CustomMapDataDef def, string directory, string path)
        {
            LongEventHandler.QueueLongEvent(() =>
            {
                Directory.CreateDirectory(directory);
                XDocument document = new XDocument(new XElement("Defs",
                    def.SaveToXElement("QuestEditor_Library.CustomMapDataDef")));
                document.Save(path);
                Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
            }, "SaveToFile".Translate(), true, exception =>
            {
                Log.Error("Export blueprint XML failed: " + exception);
                Messages.Message("CQF_BlueprintExportFailed".Translate(exception.Message), MessageTypeDefOf.RejectInput);
            });
        }

        private static int GetNextBlueprintNumber()
        {
            do
            {
                blueprintCounter++;
            }
            while (ContainsDefName("CQF_Blueprint_" + blueprintCounter));
            return blueprintCounter;
        }

        private static void EnsureCurrentGame()
        {
            Game game = Current.Game;
            if (ReferenceEquals(activeGame, game))
            {
                return;
            }
            temporaryBlueprints.Clear();
            importedBlueprints.Clear();
            BlueprintPreviewCache.Clear();
            blueprintCounter = 0;
            activeGame = game;
            version++;
        }

        private static readonly List<CustomMapDataDef> temporaryBlueprints = new List<CustomMapDataDef>();
        private static readonly List<CustomMapDataDef> importedBlueprints = new List<CustomMapDataDef>();
        private static Game activeGame;
        private static int blueprintCounter;
        private static int version;
    }
}
