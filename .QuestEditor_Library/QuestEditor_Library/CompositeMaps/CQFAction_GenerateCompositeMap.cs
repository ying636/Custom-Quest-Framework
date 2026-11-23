using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFAction_GenerateCompositeMap : CQFAction_Target
{
    public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

    public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        if (definition == null || targets.Count != 1 || targets.Values.Single() is not { Map: Map source } target)
            throw new InvalidOperationException("[CQF] GenerateCompositeMap requires a definition and one target cell.");
        GameComponent_MapManager manager = GameComponent_MapManager.Instance
            ?? throw new InvalidOperationException("[CQF] Map manager is unavailable.");
        manager.GenerateCompositeMap(definition, source, target.Cell, quest);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref definition, "definition");
    }

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        if (definition != null) result.Add(new XElement("definition", definition.defName));
        return result;
    }

    public CQFCompositeMapDef definition = null!;
}
