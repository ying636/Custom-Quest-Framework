using System.Xml.Linq;
using RimWorld;
using System.Globalization;
using Verse;
using UnityEngine;

namespace QuestEditor_Library
{
    public sealed class CQFAIResourceCatalog
    {
        public CQFAIResourceCatalog(CQFAIModel model) { this.model = model; }
        public XElement Summary() => new XElement("resources", new XAttribute("mods", LoadedModManager.RunningModsListForReading.Count),
            new XAttribute("defTypes", GenDefDatabase.AllDefTypesWithDatabases().Count()));
        public XElement Discover(bool mods, string search, int offset)
        {
            if (search.Length > 100 || offset < 0 || offset > 100000) throw new InvalidDataException("CQF_AI_InvalidTool: pagination/search");
            IEnumerable<XElement> entries = mods
                ? LoadedModManager.RunningModsListForReading.OrderBy(mod => mod.PackageId).Select(mod => new XElement("mod", new XAttribute("id", mod.PackageId), new XAttribute("name", mod.Name)))
                : GenDefDatabase.AllDefTypesWithDatabases().OrderBy(type => type.FullName).Select(type => new XElement("type", new XAttribute("name", type.FullName!)));
            XElement[] matches = entries.Where(entry => entry.Attributes().Any(attribute => attribute.Value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
            return new XElement(mods ? "mods" : "defTypes", new XAttribute("total", matches.Length), new XAttribute("offset", offset), matches.Skip(offset).Take(40));
        }
        public XElement Overview()
        {
            return new XElement("resources",
                new XElement("mods", LoadedModManager.RunningModsListForReading.Select(mod => new XElement("mod", new XAttribute("id", mod.PackageId), new XAttribute("name", mod.Name)))),
                new XElement("defTypes", GenDefDatabase.AllDefTypesWithDatabases().OrderBy(type => type.FullName).Select(type => new XElement("type", new XAttribute("name", type.FullName!), new XAttribute("count", GenDefDatabase.GetAllDefsInDatabaseForDef(type).Count())))));
        }
        public XElement Query(XElement queries)
        {
            if (queries.Name != "queries" || queries.HasAttributes || queries.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
                throw InvalidQuery("CQF_AI_QueryRoot");
            if (queries.Elements().Count() < 1 || queries.Elements().Count() > 8) throw InvalidQuery("CQF_AI_QueryCount");
            XElement results = new XElement("results");
            foreach (XElement query in queries.Elements())
            {
                string[] attributes = query.Name.ToString() switch
                {
                    "defs" => new[] { "type", "search", "mod", "offset" },
                    "images" => new[] { "search", "mod", "offset" },
                    "schema" => new[] { "type" },
                    "object" => new[] { "type", "name" },
                    _ => throw InvalidQuery("CQF_AI_QueryUnknown", query.Name.ToString())
                };
                if (query.HasElements || !string.IsNullOrWhiteSpace(query.Value)) throw InvalidQuery("CQF_AI_QueryBody", query.Name.ToString());
                XAttribute? invalidAttribute = query.Attributes().FirstOrDefault(attribute => !attributes.Contains(attribute.Name.ToString()));
                if (invalidAttribute != null) throw InvalidQuery("CQF_AI_QueryAttributes", query.Name.ToString(), invalidAttribute.Name.ToString(), string.Join(", ", attributes));
                string typeName = query.Attribute("type")?.Value.Trim() ?? string.Empty;
                if (query.Name != "images" && typeName.Length == 0) throw InvalidQuery("CQF_AI_QueryType", query.Name.ToString());
                if (query.Name == "schema")
                {
                    results.Add(model.Schema(model.Resolve(typeName)));
                    continue;
                }
                string search = query.Attribute("search")?.Value ?? string.Empty;
                if (search.Length > 100) throw InvalidQuery("CQF_AI_QuerySearch");
                string modFilter = query.Attribute("mod")?.Value ?? string.Empty;
                if (!int.TryParse(query.Attribute("offset")?.Value ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out int offset)
                    || offset < 0 || offset > 100000) throw InvalidQuery("CQF_AI_QueryOffset");
                bool Match(string value) => value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                if (query.Name == "images")
                {
                    var images = LoadedModManager.RunningModsListForReading.Where(mod => modFilter.Length == 0 || mod.PackageId.Equals(modFilter, StringComparison.OrdinalIgnoreCase))
                        .SelectMany(mod => mod.GetContentHolder<Texture2D>().contentList.Keys.Where(Match).Select(path => new { path, mod = mod.PackageId })).OrderBy(entry => entry.path).ToList();
                    results.Add(new XElement("images", new XAttribute("total", images.Count), new XAttribute("offset", offset),
                        images.Skip(offset).Take(40).Select(entry => new XElement("image", new XAttribute("path", entry.path), new XAttribute("mod", entry.mod)))));
                    continue;
                }
                Type type = GenDefDatabase.AllDefTypesWithDatabases().FirstOrDefault(type => type.FullName == typeName)
                    ?? throw new InvalidDataException("CQF_AI_UnknownType: " + typeName);
                if (query.Name == "object")
                {
                    if (!model.Types.Contains(type)) throw new InvalidDataException("CQF_AI_UnknownType: " + typeName);
                    string name = query.Attribute("name")?.Value ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(name)) throw InvalidQuery("CQF_AI_QueryName");
                    Def definition = GenDefDatabase.GetAllDefsInDatabaseForDef(type).FirstOrDefault(def => def.defName == name)
                        ?? throw new InvalidDataException("CQF_AI_MissingResource: " + name);
                    results.Add(model.Write(definition, "object", true));
                    continue;
                }
                var matches = GenDefDatabase.GetAllDefsInDatabaseForDef(type)
                    .Where(def => (modFilter.Length == 0 || def.modContentPack?.PackageId.Equals(modFilter, StringComparison.OrdinalIgnoreCase) == true) && (Match(def.defName) || Match(def.label ?? string.Empty)))
                    .OrderByDescending(def => def.defName.Equals(search, StringComparison.OrdinalIgnoreCase)).ThenBy(def => def.defName).ToList();
                results.Add(new XElement("defs", new XAttribute("type", type.FullName!), new XAttribute("total", matches.Count), new XAttribute("offset", offset),
                    matches.Skip(offset).Take(40).Select(def => new XElement("def", new XAttribute("name", def.defName), new XAttribute("label", def.label ?? def.defName),
                        new XAttribute("mod", def.modContentPack?.PackageId ?? string.Empty), new XElement("description", def.description ?? string.Empty), ResourceDetails(def)))));
            }
            return results;
        }
        public XElement QueryForAssistant(XElement queries)
        {
            try { return Query(queries); }
            catch (InvalidDataException error) when (error.Message.StartsWith("CQF_AI_InvalidQuery", StringComparison.Ordinal)
                || error.Message.StartsWith("CQF_AI_UnknownType", StringComparison.Ordinal) || error.Message.StartsWith("CQF_AI_MissingResource", StringComparison.Ordinal))
            {
                string code = error.Message.Split(new[] { ':', ' ' }, 2)[0];
                return new XElement("results", new XElement("error", new XAttribute("code", code), error.Message));
            }
        }
        private static InvalidDataException InvalidQuery(string key, params NamedArgument[] arguments) => new InvalidDataException("CQF_AI_InvalidQuery: " + key.Translate(arguments));
        private static XElement? ResourceDetails(Def def)
        {
            if (def is ThingDef thing) return new XElement("placement", new XAttribute("category", thing.category), new XAttribute("sizeX", thing.size.x),
                new XAttribute("sizeZ", thing.size.z), new XAttribute("madeFromStuff", thing.MadeFromStuff), new XAttribute("stackLimit", thing.stackLimit),
                new XAttribute("rotatable", thing.rotatable), thing.MadeFromStuff ? GenStuff.AllowedStuffsFor(thing).Take(40).Select(stuff => new XElement("stuff", stuff.defName)) : null);
            if (def is TerrainDef terrain) return new XElement("terrain", new XAttribute("temporary", terrain.temporary), new XAttribute("foundation", terrain.isFoundation));
            return null;
        }
        private readonly CQFAIModel model;
    }
}
