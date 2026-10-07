using System.Reflection;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIFacilityRuntime
    {
        public CQFAIFacilityRuntime(CQFAIModel model, CQFAIRuntimeJournal journal, bool text, string command = "") { this.model = model; this.journal = journal; this.text = text; this.command = command; }
        public XElement Help() => new XElement("facilities",
            new XElement("queries", "facilities: map_id,offset,limit. facility: thing_id,component_index,optional path=/ and offset/limit. Returns CQFAIFacilityConfiguration for that physical component. Query schema before editing."),
            new XElement("operation", new XAttribute("kind", "facility_configure"), "thing_id,component_index,changes_xml=generic changes. Editable fields depend on actual component: CompTransmit receiverId (empty disconnect); CompPower_Level linkedPowerId/outputMode/targetPowerOutput (bidirectional, complementary modes); CompLandFillable startLandfill/tickToFill/filled/iconPath/landfillText; CompTriggerDialog triggerSignal/dialog; CompHackOutcome outcomes; CompSetMapAndGenerate exitKey/mapChoice (weighted Def/tag configuration). Props are cloned per instance so other buildings and ThingDef props are preserved. Configuration changes do not transmit objects, fill land, start dialogue, execute hacking outcomes or replay PostPostMake. Use entrance configuration tools to explicitly set an existing entrance's active MapDef."));
        public XElement Read(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "map_id", "thing_id", "component_index", "path", "offset", "limit");
            if (CQFAIRuntimeRequest.Text(request, "kind") == "facilities") return CQFAIRuntimeRequest.Page("facilities", CQFAIRuntimeRequest.Map(request).listerThings.AllThings.OfType<ThingWithComps>()
                .SelectMany(thing => thing.AllComps.Select((comp, index) => new { thing, comp, index }).Where(entry => Supported(entry.comp)))
                .Select(entry => new XElement("facility", new XAttribute("thingId", entry.thing.ThingID), new XAttribute("def", entry.thing.def.defName), new XAttribute("componentIndex", entry.index), new XAttribute("type", entry.comp.GetType().FullName!))), request);
            ThingComp target = Component(request);
            return new XElement("facility", new XAttribute("type", target.GetType().FullName!), new CQFAITargetReader(model).Read(Configuration(target), CQFAIRuntimeRequest.Text(request, "path", "/"), CQFAIRuntimeRequest.Offset(request), CQFAIRuntimeRequest.Limit(request)));
        }
        public void AddInstruction(string text) { command += "\n" + text; }
        public XElement Operate(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "thing_id", "component_index", "changes_xml");
            if (CQFAIRuntimeRequest.Text(request, "kind") != "facility_configure") throw new InvalidDataException("CQF_AI_InvalidTool: facility operation");
            ThingComp comp = Component(request);
            CQFAIFacilityConfiguration before = Configuration(comp);
            CQFAIFacilityConfiguration after = (CQFAIFacilityConfiguration)new CQFAIChanges(model).Build(before, CQFAIChanges.Parse(CQFAIRuntimeRequest.Text(request, "changes_xml")), command, text);
            string[] allowed = comp switch
            {
                CompTransmit => new[] { "receiverId" }, CompPower_Level => new[] { "linkedPowerId", "outputMode", "targetPowerOutput" },
                CompLandFillable => new[] { "startLandfill", "tickToFill", "filled", "iconPath", "landfillText" }, CompTriggerDialog => new[] { "triggerSignal", "dialog" },
                CompHackOutcome => new[] { "outcomes" }, CompSetMapAndGenerate => new[] { "exitKey", "mapChoice" }, _ => throw new InvalidDataException("CQF_AI_InvalidValue: unsupported component")
            };
            XElement original = model.Write(before, root: true), updated = model.Write(after, root: true);
            if (original.Elements().Any(field => !allowed.Contains(field.Name.LocalName) && !XNode.DeepEquals(field, updated.Element(field.Name)))) throw new InvalidDataException("CQF_AI_InvalidValue: field does not belong to component");
            CompProperties oldProps = comp.props;
            Action apply, undo;
            Func<XElement> read = () => new XElement("facilityState", new XAttribute("spawned", comp.parent.Spawned), new XAttribute("destroyed", comp.parent.Destroyed), model.Write(Configuration(comp), root: true));
            if (comp is CompTransmit)
            {
                Building_TransmitReceiver? receiver = after.receiverId.Length == 0 ? null : CQFAIRuntimeRequest.Thing(after.receiverId) as Building_TransmitReceiver ?? throw new InvalidDataException("CQF_AI_InvalidValue: transmit receiver required");
                FieldInfo field = typeof(CompTransmit).GetField("receiver", BindingFlags.NonPublic | BindingFlags.Instance)!; object? old = field.GetValue(comp);
                apply = () => field.SetValue(comp, receiver); undo = () => field.SetValue(comp, old);
            }
            else if (comp is CompPower_Level power)
            {
                if (float.IsNaN(after.targetPowerOutput) || float.IsInfinity(after.targetPowerOutput) || after.targetPowerOutput < 0 || after.targetPowerOutput > 10000000) throw new InvalidDataException("CQF_AI_InvalidValue: power output");
                CompPower_Level? linked = after.linkedPowerId.Length == 0 ? null : CQFAIRuntimeRequest.Thing(after.linkedPowerId).TryGetComp<CompPower_Level>() ?? throw new InvalidDataException("CQF_AI_InvalidValue: power receiver required");
                if (linked == power || linked != null && linked.outputMode == after.outputMode || linked?.linked != null && linked.linked != power.parent) throw new InvalidDataException("CQF_AI_InvalidValue: power link conflict/modes");
                CompPower_Level? oldLinked = power.linked?.TryGetComp<CompPower_Level>();
                CompPower_Level[] touched = new[] { power, oldLinked, linked }.Where(value => value != null).Cast<CompPower_Level>().Distinct().ToArray();
                var old = touched.Select(value => new { value, value.linked, value.comp, value.outputMode, value.targetPowerOutput }).ToArray();
                apply = () => { if (oldLinked?.linked == power.parent) { oldLinked.linked = null!; oldLinked.comp = null!; } power.linked = (linked?.parent)!; power.comp = linked!; power.outputMode = after.outputMode; power.targetPowerOutput = after.targetPowerOutput; if (linked != null) { linked.linked = power.parent; linked.comp = power; } foreach (CompPower_Level item in touched) item.UpdateDesiredPowerOutput(); };
                undo = () => { foreach (var entry in old) { entry.value.linked = entry.linked; entry.value.comp = entry.comp; entry.value.outputMode = entry.outputMode; entry.value.targetPowerOutput = entry.targetPowerOutput; } foreach (CompPower_Level item in touched) item.UpdateDesiredPowerOutput(); };
                read = () => new XElement("powerLinks", touched.Select(value => new XElement("power", new XAttribute("thingId", value.parent.ThingID), new XAttribute("spawned", value.parent.Spawned), new XAttribute("destroyed", value.parent.Destroyed), model.Write(Configuration(value), root: true))));
            }
            else
            {
                CompProperties clone = (CompProperties)typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(oldProps, null)!;
                if (comp is CompLandFillable fill)
                {
                    if (after.tickToFill < 1 || after.tickToFill > 10000000 || after.filled == null || after.filled.category != ThingCategory.Building || after.landfillText.Length > 200 || after.iconPath.Length > 500) throw new InvalidDataException("CQF_AI_InvalidValue: landfill configuration");
                    CompPropertiesLandFillable props = (CompPropertiesLandFillable)clone;
                    props.tickToFill = after.tickToFill; props.filled = after.filled; props.iconPath = after.iconPath; props.landfillText = after.landfillText;
                    typeof(CompPropertiesLandFillable).GetField("icon", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(props, null);
                    bool oldStart = fill.startLandfill; apply = () => { comp.props = props; fill.startLandfill = after.startLandfill; }; undo = () => { comp.props = oldProps; fill.startLandfill = oldStart; };
                }
                else if (comp is CompTriggerDialog)
                {
                    if (after.triggerSignal.Length > 200 || after.triggerSignal.Length > 0 && after.dialog == null) throw new InvalidDataException("CQF_AI_InvalidValue: dialogue trigger");
                    CompPropertiesTriggerDialog props = (CompPropertiesTriggerDialog)clone; props.triggerSignal = after.triggerSignal.Length == 0 ? null! : after.triggerSignal; props.dialog = after.dialog!;
                    apply = () => comp.props = props; undo = () => comp.props = oldProps;
                }
                else if (comp is CompSetMapAndGenerate)
                {
                    if (after.exitKey.Length > 200 || after.mapChoice == null || after.mapChoice.datas == null || after.mapChoice.tags == null
                        || after.mapChoice.datas.Count > 256 || after.mapChoice.tags.Count > 256
                        || after.mapChoice.datas.Any(value => value == null || value.data == null || value.weight < 0 || float.IsNaN(value.weight) || float.IsInfinity(value.weight))
                        || after.mapChoice.tags.Any(value => value == null || string.IsNullOrWhiteSpace(value.tag) || value.weight < 0 || float.IsNaN(value.weight) || float.IsInfinity(value.weight))
                        || after.mapChoice.datas.Select(value => value.data).Distinct().Count() != after.mapChoice.datas.Count || after.mapChoice.tags.Select(value => value.tag).Distinct().Count() != after.mapChoice.tags.Count)
                        throw new InvalidDataException("CQF_AI_InvalidValue: map selection configuration");
                    CompPropertiesSetMapAndGenerate props = (CompPropertiesSetMapAndGenerate)clone; props.key = after.exitKey; props.map = after.mapChoice;
                    apply = () => comp.props = props; undo = () => comp.props = oldProps;
                }
                else
                {
                    CQFAIChanges.Validate(after);
                    if (after.outcomes == null || after.outcomes.Count > 256 || after.outcomes.Any(action => action == null)) throw new InvalidDataException("CQF_AI_InvalidValue: hacking outcomes");
                    ((CompPropertiesHackOutcome)clone).outcoomes = after.outcomes; apply = () => comp.props = clone; undo = () => comp.props = oldProps;
                }
            }
            int index = CQFAIRuntimeRequest.Int(request, "component_index");
            if (CQFComponentOverride.Supported(comp))
            {
                MapComponent_CQFComponentOverrides persistence = comp.parent.Map.GetComponent<MapComponent_CQFComponentOverrides>() ?? throw new InvalidDataException("CQF_AI_MissingResource: component persistence");
                CQFComponentOverride? prior = persistence.Get(comp.parent, index);
                Action applyConfiguration = apply, undoConfiguration = undo;
                apply = () => { applyConfiguration(); persistence.Set(comp.parent, index, new CQFComponentOverride(comp, index)); };
                undo = () => { undoConfiguration(); persistence.Set(comp.parent, index, prior); };
            }
            return journal.Edit("facility:" + comp.parent.ThingID + ":" + index, apply, undo, read);
        }
        public static bool Supported(ThingComp comp) => comp is CompTransmit or CompPower_Level or CompLandFillable or CompTriggerDialog or CompHackOutcome or CompSetMapAndGenerate;
        private static CQFAIFacilityConfiguration Configuration(ThingComp comp)
        {
            CQFAIFacilityConfiguration value = new CQFAIFacilityConfiguration();
            if (comp is CompTransmit) value.receiverId = (typeof(CompTransmit).GetField("receiver", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(comp) as Thing)?.ThingID ?? "";
            if (comp is CompPower_Level power) { value.linkedPowerId = power.linked?.ThingID ?? ""; value.outputMode = power.outputMode; value.targetPowerOutput = power.targetPowerOutput; }
            if (comp is CompLandFillable fill) { value.startLandfill = fill.startLandfill; value.tickToFill = fill.Props.tickToFill; value.filled = fill.Props.filled; value.iconPath = fill.Props.iconPath ?? ""; value.landfillText = fill.Props.landfillText ?? ""; }
            if (comp is CompTriggerDialog trigger) { value.triggerSignal = trigger.Props.triggerSignal ?? ""; value.dialog = trigger.Props.dialog; }
            if (comp is CompHackOutcome hack) value.outcomes = hack.Props.outcoomes;
            if (comp is CompSetMapAndGenerate generate) { value.exitKey = generate.Props.key ?? ""; value.mapChoice = generate.Props.map; }
            return value;
        }
        private static ThingComp Component(XElement request)
        {
            ThingWithComps thing = CQFAIRuntimeRequest.Thing(CQFAIRuntimeRequest.Text(request, "thing_id")) as ThingWithComps ?? throw new InvalidDataException("CQF_AI_InvalidValue: ThingWithComps required");
            int index = CQFAIRuntimeRequest.Int(request, "component_index");
            return index >= 0 && index < thing.AllComps.Count && Supported(thing.AllComps[index]) ? thing.AllComps[index] : throw new InvalidDataException("CQF_AI_MissingResource: component index");
        }
        private readonly CQFAIModel model;
        private readonly CQFAIRuntimeJournal journal;
        private readonly bool text;
        private string command;
    }
}
