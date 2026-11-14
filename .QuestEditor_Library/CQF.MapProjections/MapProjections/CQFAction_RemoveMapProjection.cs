#if RIMWORLD_1_7
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFAction_RemoveMapProjection : CQFAction
{
    public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        Map[] hosts = GameTools.GetTargets(targets, quest, new List<string> { hostKey }).Values.Select(target => target.Map).Distinct().ToArray();
        if (hosts.Length != 1 || hosts[0] == null || !hosts[0].GetComponent<MapComponent_CQFMapProjections>().Remove(id))
            throw new InvalidOperationException("[CQF] Map projection removal target was not found: " + id);
    }

    public override void Draw(ref float y, Rect inRect, float x)
    {
        base.Draw(ref y, inRect, x);
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MP_ID".Translate(), ref id, x); y += 34f;
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MP_Host".Translate(), ref hostKey, x); y += 34f;
    }

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        result.Add(new XElement("id", id), new XElement("hostKey", hostKey));
        return result;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref id, "id", "CQF_Projection");
        Scribe_Values.Look(ref hostKey, "hostKey", "Position");
    }

    public string id = "CQF_Projection";
    public string hostKey = "Position";
}
#endif
