using System.Xml.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public sealed class CQFAIDefinitionRuntime
    {
        public CQFAIDefinitionRuntime(CQFAIModel model, CQFAIRuntimeJournal journal, bool text, string command = "") { this.model = model; this.journal = journal; this.text = text; this.command = command; }
        public XElement Help() => new XElement("definitions",
            new XElement("request", new XAttribute("kind", "create"), "type=exact CQF editable Def type, name=CQF_ prefixed XML/file-safe name, optional changes_xml (generic changes, schema first),save=false,overwrite=false. Registers validated new data so it becomes discoverable/editable; no automatic map/entity generation."),
            new XElement("request", new XAttribute("kind", "copy"), "type,name=new CQF_ name,source_name=existing definition,optional changes_xml,save=false,overwrite=false. Deep-copies editable configuration and keeps referenced Defs."),
            new XElement("request", new XAttribute("kind", "save"), "type,name=existing loaded definition,overwrite=false. Saves source XML to its CQF category and hot-loads it. Read it first; file overwrite requires true. Uses atomic file replacement."),
            new XElement("request", new XAttribute("kind", "capture_map"), "map_id,name=new CQF_ name, optional x,z,width,height for rectangular part; omit all four for full map. save=false,overwrite=false. Captures native CQF map data, ThingData, customThings, terrain, roofs, routes and Lords through existing LoadData. Does not replace the current map. NPCs/configuration are captured according to the existing CQF map saving rules."),
            new XElement("request", new XAttribute("kind", "refresh_bindings"), "type,name=existing loaded definition. Rebinds stale matching Def references in supported CQF loaded configurations and runtime components, updates QuestBook instances and native Duty application where applicable. Native QuestBook refresh may activate the first step if no active step remains. Entity modules are not reapplied and maps are not regenerated; use explicit operations for materialized contents."),
            new XElement("undo", "Definition registration, source writes and runtime binding refresh are not fully undoable. Each returns actual name/path/state and must be read back. Export tool returns XML text pages with total characters."));
        public XElement Export(XElement arguments)
        {
            Def def = Resolve(CQFAIRuntimeRequest.Text(arguments, "type"), CQFAIRuntimeRequest.Text(arguments, "name"));
            int offset = CQFAIRuntimeRequest.Int(arguments, "offset", 0), limit = CQFAIRuntimeRequest.Int(arguments, "limit", 8000);
            if (offset < 0 || limit < 1 || limit > 16000) throw new InvalidDataException("CQF_AI_InvalidTool: export page");
            string xml = new XDocument(new XElement("Defs", CQFAIDefDocument.Export(def))).ToString();
            return new XElement("definitionXml", new XAttribute("totalCharacters", xml.Length), new XAttribute("offset", offset), new XAttribute("nextOffset", offset < xml.Length ? Math.Min(xml.Length, offset + limit) : xml.Length),
                offset >= xml.Length ? "" : xml.Substring(offset, Math.Min(limit, xml.Length - offset)));
        }
        public void AddInstruction(string text) { command += "\n" + text; }
        public XElement Manage(XElement request)
        {
            if (!journal.IsTargetValid) throw new InvalidDataException("CQF_AI_StaleTarget");
            string kind = CQFAIRuntimeRequest.Text(request, "kind"), name = CQFAIRuntimeRequest.Text(request, "name"), typeName = CQFAIRuntimeRequest.Text(request, "type");
            CQFAIRuntimeRequest.Fields(request, kind switch
            {
                "create" => new[] { "kind", "type", "name", "changes_xml", "save", "overwrite" },
                "copy" => new[] { "kind", "type", "name", "source_name", "changes_xml", "save", "overwrite" },
                "save" => new[] { "kind", "type", "name", "overwrite" },
                "refresh_bindings" => new[] { "kind", "type", "name" },
                "capture_map" => new[] { "kind", "name", "map_id", "x", "z", "width", "height", "save", "overwrite" },
                _ => throw new InvalidDataException("CQF_AI_InvalidTool: definition request")
            });
            bool save = CQFAIRuntimeRequest.Bool(request, "save"), overwrite = CQFAIRuntimeRequest.Bool(request, "overwrite");
            if (kind == "save")
            {
                Def existing = Resolve(typeName, name);
                string path = CQFAIDefDocument.PathFor(existing);
                if (File.Exists(path) && !overwrite) throw new InvalidDataException("CQF_AI_InvalidValue: source file exists; overwrite required");
                Validate(existing); CQFAIDefDocument.Export(existing);
                journal.MarkIrreversible(); path = CQFAIDefDocument.Save(existing, overwrite);
                return Receipt(existing, path);
            }
            if (kind == "refresh_bindings")
            {
                Def existing = Resolve(typeName, name); Validate(existing);
                journal.MarkIrreversible(); int updated = new CQFAIDefinitionBindings().Refresh(existing);
                return new XElement("bindingsRefreshed", new XAttribute("undoSupported", false), new XAttribute("references", updated), new XAttribute("name", name), new XAttribute("materializedContentsRegenerated", false));
            }
            if (kind is not ("create" or "copy" or "capture_map") || !name.StartsWith("CQF_", StringComparison.Ordinal)) throw new InvalidDataException("CQF_AI_InvalidName");
            System.Xml.XmlConvert.VerifyNCName(name);
            Type type = kind == "capture_map" ? typeof(CustomMapDataDef) : SupportedType(typeName);
            if (GenDefDatabase.GetAllDefsInDatabaseForDef(type).Any(def => def.defName == name)) throw new InvalidDataException("CQF_AI_DuplicateKey: definition exists");
            Def definition = kind == "copy" ? (Def)model.Copy(Resolve(typeName, CQFAIRuntimeRequest.Text(request, "source_name"))) : (Def)Activator.CreateInstance(type)!;
            definition.defName = name; definition.label ??= name;
            if (kind == "capture_map") Capture((CustomMapDataDef)definition, request);
            if (request.Element("changes_xml") != null) definition = (Def)new CQFAIChanges(model).Build(definition, CQFAIChanges.Parse(CQFAIRuntimeRequest.Text(request, "changes_xml")), command, text);
            if (definition.defName != name) throw new InvalidDataException("CQF_AI_DefinitionNameFixed");
            Validate(definition); CQFAIDefDocument.Export(definition);
            string? targetPath = save ? CQFAIDefDocument.PathFor(definition) : null;
            if (targetPath != null && File.Exists(targetPath) && !overwrite) throw new InvalidDataException("CQF_AI_InvalidValue: source file exists; overwrite required");
            journal.MarkIrreversible();
            if (save) targetPath = CQFAIDefDocument.Save(definition, overwrite); else CQFQuestDefBootstrap.HotLoadDefinition(definition);
            if (!GenDefDatabase.GetAllDefsInDatabaseForDef(type).Any(def => ReferenceEquals(def, definition))) throw new InvalidDataException("CQF_AI_ApplyMismatch: definition registration");
            return Receipt(definition, targetPath);
        }
        private Def Resolve(string typeName, string name)
        {
            Type type = SupportedType(typeName);
            return GenDefDatabase.GetAllDefsInDatabaseForDef(type).FirstOrDefault(def => def.defName == name) ?? throw new InvalidDataException("CQF_AI_MissingResource: definition");
        }
        private Type SupportedType(string name)
        {
            Type type = model.Resolve(name);
            if (type.IsAbstract || !typeof(Def).IsAssignableFrom(type) || type.Assembly != typeof(DialogTreeDef).Assembly && type != typeof(DutyDef) && type != typeof(QuestScriptDef)) throw new InvalidDataException("CQF_AI_UnknownType: " + name);
            return type;
        }
        private void Validate(Def definition)
        {
            CQFAIChanges.Validate(definition);
            string[] errors = definition.ConfigErrors().Take(16).ToArray();
            if (errors.Length > 0) throw new InvalidDataException("CQF_AI_InvalidValue: " + string.Join("; ", errors));
            if (!text && new CQFAIChanges(model).Build(definition, new XElement("changes"), "", false) is not Def) throw new InvalidDataException("CQF_AI_TextNotAllowed");
        }
        private static void Capture(CustomMapDataDef definition, XElement request)
        {
            Map map = CQFAIRuntimeRequest.Map(request);
            bool part = new[] { "x", "z", "width", "height" }.Any(name => request.Element(name) != null);
            definition.isPart = part;
            if (!part)
            {
                definition.LoadData(map);
                if (map.GetComponent<MapComponent_CustomMapData>() is MapComponent_CustomMapData component) definition.lordDatas = component.Lords.Select(lord => lord.data).ToList();
                return;
            }
            int x = CQFAIRuntimeRequest.Int(request, "x"), z = CQFAIRuntimeRequest.Int(request, "z"), width = CQFAIRuntimeRequest.Int(request, "width"), height = CQFAIRuntimeRequest.Int(request, "height");
            if (x < 0 || z < 0 || width < 1 || height < 1 || (long)x + width > map.Size.x || (long)z + height > map.Size.z) throw new InvalidDataException("CQF_AI_MapBounds");
            List<IntVec3> cells = new CellRect(x, z, width, height).Cells.ToList();
            definition.LoadData(map, cells, new IntVec3(width, 1, height));
        }
        private XElement Receipt(Def def, string? path) => new XElement("definitionManaged", new XAttribute("undoSupported", false), new XAttribute("type", def.GetType().FullName!), new XAttribute("name", def.defName), new XAttribute("registered", true), new XAttribute("saved", path != null), path == null ? null : new XElement("path", path), new CQFAITargetReader(model).Summary(def));
        private readonly CQFAIModel model;
        private readonly CQFAIRuntimeJournal journal;
        private readonly bool text;
        private string command;
    }
}
