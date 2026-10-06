using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookTextureEntry
    {
        public QuestBookTextureEntry(string label, string path, string category)
        {
            this.Label = label;
            this.TexturePath = path;
            this.Category = category;
            this.Texture = path.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(path, false);
        }

        public static void OpenSelect(Action<string> selectAction, string titleKey)
        {
            List<QuestBookTextureEntry> entries = MakeEntries();
            Dictionary<string, Func<QuestBookTextureEntry, bool>> typeFilters = new Dictionary<string, Func<QuestBookTextureEntry, bool>>
            {
                { "CQF_QuestBook_TextureCategoryEntity".Translate().ToString(), entry => entry.Category == EntityCategory },
                { "CQF_QuestBook_TextureCategoryBuilding".Translate().ToString(), entry => entry.Category == BuildingCategory },
                { "CQF_QuestBook_TextureCategoryItem".Translate().ToString(), entry => entry.Category == ItemCategory },
                { "CQF_QuestBook_TextureCategoryFloor".Translate().ToString(), entry => entry.Category == FloorCategory },
                { "CQF_QuestBook_TextureCategoryOther".Translate().ToString(), entry => entry.Category == OtherCategory }
            };
            Find.WindowStack.Add(new Dialog_Select<QuestBookTextureEntry>(
                new LabeledTextureSelectDrawer<QuestBookTextureEntry>(
                    entries,
                    entry => entry.Texture,
                    entry => entry.Label,
                    entry => selectAction(entry.TexturePath),
                    null,
                    null,
                    entry => entry.TexturePath,
                    null,
                    entry => entry.Label,
                    null,
                    typeFilters,
                    null),
                titleKey.Translate().ToString()));
        }

        public static string GetThingTexturePath(ThingDef def)
        {
            if (def == null)
            {
                return null;
            }
            if (!def.uiIconPath.NullOrEmpty() && ContentFinder<Texture2D>.Get(def.uiIconPath, false) != null)
            {
                return def.uiIconPath;
            }
            if (def.graphicData == null || def.graphicData.texPath.NullOrEmpty())
            {
                return null;
            }
            if (def.graphicData.graphicClass != null && typeof(Graphic_Multi).IsAssignableFrom(def.graphicData.graphicClass))
            {
                string southPath = def.graphicData.texPath + "_south";
                if (ContentFinder<Texture2D>.Get(southPath, false) != null)
                {
                    return southPath;
                }
            }
            return ContentFinder<Texture2D>.Get(def.graphicData.texPath, false) != null ? def.graphicData.texPath : null;
        }

        private static List<QuestBookTextureEntry> MakeEntries()
        {
            return MakeThingEntries()
                .Concat(MakePawnEntries())
                .Concat(MakeTerrainEntries())
                .Where(entry => entry.Texture != null)
                .GroupBy(entry => entry.TexturePath, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(entry => entry.Category)
                .ThenBy(entry => entry.Label)
                .ToList();
        }

        private static List<QuestBookTextureEntry> MakeThingEntries()
        {
            return DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.category != ThingCategory.Pawn && def.category != ThingCategory.Mote && def.mote == null
                    && def.projectile == null && def.skyfaller == null && def.pawnFlyer == null && def.gas == null
                    && def.filth == null)
                .Select(def => new { def, path = GetThingTexturePath(def) })
                .Where(item => !item.path.NullOrEmpty())
                .Select(item => new QuestBookTextureEntry(item.def.LabelCap, item.path,
                    item.def.building != null ? BuildingCategory : item.def.category == ThingCategory.Item ? ItemCategory : OtherCategory))
                .ToList();
        }

        private static List<QuestBookTextureEntry> MakePawnEntries()
        {
            List<QuestBookTextureEntry> result = new List<QuestBookTextureEntry>();
            Dictionary<string, string> pawnLabels = DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(pawnKind => pawnKind.lifeStages != null)
                .SelectMany(pawnKind => pawnKind.lifeStages
                    .Where(stage => stage?.bodyGraphicData != null && !stage.bodyGraphicData.texPath.NullOrEmpty())
                    .Select(stage => new { path = stage.bodyGraphicData.texPath, label = pawnKind.race.LabelCap.ToString() }))
                .GroupBy(item => NormalizeTexturePath(item.path), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().label, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> pawnLabelsByFileName = DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(pawnKind => pawnKind.lifeStages != null)
                .SelectMany(pawnKind => pawnKind.lifeStages
                    .Where(stage => stage?.bodyGraphicData != null && !stage.bodyGraphicData.texPath.NullOrEmpty())
                    .Select(stage => new { path = stage.bodyGraphicData.texPath, label = pawnKind.race.LabelCap.ToString() }))
                .GroupBy(item => Path.GetFileName(NormalizeTexturePath(item.path)), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().label, StringComparer.OrdinalIgnoreCase);
            foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
            {
                string texturesDir = Path.Combine(mod.RootDir, "Textures");
                if (!Directory.Exists(texturesDir))
                {
                    continue;
                }
                foreach (string file in Directory.GetFiles(texturesDir, "*.*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(file).ToLowerInvariant();
                    if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                    {
                        continue;
                    }
                    string relativePath = file.Substring(texturesDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (relativePath.IndexOf("pawn", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    string fileName = Path.GetFileNameWithoutExtension(relativePath);
                    string directionKey = GetDirectionKey(fileName);
                    string directionSuffix = GetDirectionSuffix(fileName);
                    if (directionKey.NullOrEmpty() || directionSuffix.NullOrEmpty())
                    {
                        continue;
                    }
                    string texturePath = Path.Combine(Path.GetDirectoryName(relativePath) ?? string.Empty, fileName).Replace("\\", "/");
                    string basePath = NormalizeTexturePath(texturePath.Substring(0, texturePath.Length - directionSuffix.Length));
                    if (!pawnLabels.TryGetValue(basePath, out string pawnLabel)
                        && !pawnLabelsByFileName.TryGetValue(Path.GetFileName(basePath), out pawnLabel))
                    {
                        continue;
                    }
                    result.Add(new QuestBookTextureEntry(pawnLabel + " - " + directionKey.Translate(), texturePath, EntityCategory));
                }
            }
            return result;
        }

        private static List<QuestBookTextureEntry> MakeTerrainEntries()
        {
            return DefDatabase<TerrainDef>.AllDefsListForReading
                .Where(def => !def.texturePath.NullOrEmpty() && ContentFinder<Texture2D>.Get(def.texturePath, false) != null)
                .Select(def => new QuestBookTextureEntry(def.LabelCap, def.texturePath, FloorCategory))
                .ToList();
        }

        private static string GetDirectionKey(string fileName)
        {
            if (fileName.EndsWith("_south", StringComparison.OrdinalIgnoreCase)) return "CQF_QuestBook_DirectionSouth";
            if (fileName.EndsWith("_east", StringComparison.OrdinalIgnoreCase)) return "CQF_QuestBook_DirectionEast";
            if (fileName.EndsWith("_north", StringComparison.OrdinalIgnoreCase)) return "CQF_QuestBook_DirectionNorth";
            if (fileName.EndsWith("_west", StringComparison.OrdinalIgnoreCase)) return "CQF_QuestBook_DirectionWest";
            return null;
        }

        private static string GetDirectionSuffix(string fileName)
        {
            if (fileName.EndsWith("_south", StringComparison.OrdinalIgnoreCase)) return "_south";
            if (fileName.EndsWith("_east", StringComparison.OrdinalIgnoreCase)) return "_east";
            if (fileName.EndsWith("_north", StringComparison.OrdinalIgnoreCase)) return "_north";
            if (fileName.EndsWith("_west", StringComparison.OrdinalIgnoreCase)) return "_west";
            return string.Empty;
        }

        private static string NormalizeTexturePath(string path)
        {
            return (path ?? string.Empty).Replace("\\", "/").TrimStart('/');
        }

        public const string EntityCategory = "Entity";
        public const string BuildingCategory = "Building";
        public const string ItemCategory = "Item";
        public const string FloorCategory = "Floor";
        public const string OtherCategory = "Other";

        public readonly string Label;
        public readonly string TexturePath;
        public readonly string Category;
        public readonly Texture2D Texture;
    }
}
