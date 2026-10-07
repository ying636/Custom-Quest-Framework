using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal sealed class AIFakeLiveMap : ICQFAILiveMap
{
    public AIFakeLiveMap(CQFAIModel model) { this.model = model; }
    public object Identity => this;
    public bool IsValid { get; set; } = true;
    public CQFAILiveMapInfo Info => new CQFAILiveMapInfo { mapId = 1, size = new IntVec3(64, 1, 64), center = new IntVec3(32, 0, 32), backend = this };
    public Dictionary<string, CQFAILiveThingConfiguration> Things { get; } = new();
    public Dictionary<string, string> Cells { get; } = new();
    public string? FailKey { get; set; }
    public bool FailUndo { get; set; }
    public int ApplyDelayMilliseconds { get; set; }
    public XElement ReadRegion(CellRect region, int offset, int limit) => new("region", new XAttribute("total", region.Area), new XAttribute("offset", offset),
        region.Skip(offset).Take(limit).Select(cell => new XElement("cell", new XAttribute("x", cell.x), new XAttribute("z", cell.z))));
    public XElement ReadThing(string id) => new("thing", new XAttribute("id", id), model.Write(Things.TryGetValue(id, out var value) ? value : throw new InvalidDataException("CQF_AI_LiveThingMissing"), "configuration", true));
    public IReadOnlyList<CQFAILiveMapEdit> Prepare(XElement changes, CQFAIModel serializer, string command, bool generateText)
    {
        List<CQFAILiveMapEdit> result = new();
        foreach (XElement operation in changes.Elements())
        {
            if (operation.Name == "place")
            {
                string id = "CQF_Check_Live_" + ++nextId;
                string defName = operation.Attribute("def")!.Value;
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName) ?? throw new InvalidDataException("CQF_AI_MissingResource");
                var configuration = new CQFAILiveThingConfiguration { hitPoints = 100, stackCount = 1,
                    interaction = def.thingClass == typeof(InteractableThing) ? new CQFAIInteractableConfiguration() : null,
                    entrance = def.thingClass == typeof(CustomMapEntrance) ? new CQFAIEntranceConfiguration() : null,
                    exit = def.thingClass == typeof(CustomMapExit) ? new CQFAIExitConfiguration() : null };
                result.Add(new CQFAILiveMapEdit("thing:" + id, () => { Things.Add(id, configuration); }, () => Things.Remove(id), () => State(id),
                    () => new XElement("placed", new XAttribute("thingId", id))));
            }
            else if (operation.Name == "editThing")
            {
                string id = operation.Attribute("id")!.Value;
                var before = (CQFAILiveThingConfiguration)model.Copy(Things[id]);
                var after = (CQFAILiveThingConfiguration)new CQFAIChanges(model).Build(before, operation.Element("changes")!, command, generateText);
                result.Add(new CQFAILiveMapEdit("thing:" + id, () => Things[id] = after, () => Things[id] = before, () => State(id), () => ReadThing(id)));
            }
            else if (operation.Name == "terrain" || operation.Name == "roof")
            {
                string key = operation.Name + ":" + operation.Attribute("x")!.Value + ":" + operation.Attribute("z")!.Value;
                string value = operation.Attribute("def")!.Value;
                string? before = null;
                result.Add(new CQFAILiveMapEdit(key, () =>
                {
                    if (ApplyDelayMilliseconds > 0) Thread.Sleep(ApplyDelayMilliseconds);
                    before = Cells.GetValueOrDefault(key); Cells[key] = value;
                    if (FailKey == key) throw new InvalidDataException("CQF_AI_ApplyMismatch");
                }, () =>
                {
                    if (FailUndo) throw new InvalidOperationException("CQF_AI_RollbackMismatch");
                    if (before == null) Cells.Remove(key); else Cells[key] = before;
                }, () => Cells.GetValueOrDefault(key, ""), () => new XElement(operation.Name, new XAttribute("def", value))));
            }
            else throw new InvalidDataException("CQF_AI_LiveMapOperationRequired");
        }
        return result;
    }
    public XElement Validate() => new("validation", new XAttribute("passed", IsValid), new XAttribute("scope", "live_map"));
    private string State(string id) => Things.TryGetValue(id, out var value) ? model.Write(value).ToString(SaveOptions.DisableFormatting) : "";
    private readonly CQFAIModel model;
    private int nextId;
}
