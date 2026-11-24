using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed partial class CQFAIResourceCatalog
    {
        public XElement SceneAssets(string package, string kind, string search, int offset, int limit, bool placement)
        {
            if (package.Length is < 1 or > 160 || search.Length > 100 || offset is < 0 or > 100000 || limit is < 1 or > 20)
                throw new InvalidDataException("CQF_AI_InvalidTool: scene asset page");
            if (!LoadedModManager.RunningModsListForReading.Any(mod => package.Equals(mod.PackageId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("CQF_AI_MissingResource: " + package);

            IEnumerable<Def> candidates = kind == "terrain"
                ? DefDatabase<TerrainDef>.AllDefsListForReading.Where(terrain => !terrain.isFoundation && !terrain.temporary)
                : ThingDefs(kind);
            Def[] found = candidates.Where(def =>
                    package.Equals(def.modContentPack?.PackageId, StringComparison.OrdinalIgnoreCase) &&
                    (def.defName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     (def.label?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0))
                .OrderByDescending(def => def.defName.Equals(search, StringComparison.OrdinalIgnoreCase))
                .ThenBy(def => def.defName, StringComparer.Ordinal)
                .ToArray();
            return new XElement("sceneAssets", new XAttribute("mod", package), new XAttribute("kind", kind),
                new XAttribute("offset", offset), new XAttribute("total", found.Length),
                found.Skip(offset).Take(limit).Select(def => new XElement("asset",
                    new XAttribute("def", def.defName), new XAttribute("label", def.label ?? def.defName),
                    def is ThingDef thing && !string.IsNullOrEmpty(thing.graphicData?.texPath) ? new XAttribute("texture", thing.graphicData.texPath) : null,
                    placement ? ResourceDetails(def) : null)));
        }

        private static IEnumerable<ThingDef> ThingDefs(string kind)
        {
            ThingCategory category = kind switch
            {
                "building" => ThingCategory.Building,
                "item" => ThingCategory.Item,
                "plant" => ThingCategory.Plant,
                _ => throw new InvalidDataException("CQF_AI_InvalidTool: scene asset kind")
            };
            return DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.category == category &&
                !def.IsBlueprint && !def.IsFrame && !def.IsCorpse);
        }
    }
}
