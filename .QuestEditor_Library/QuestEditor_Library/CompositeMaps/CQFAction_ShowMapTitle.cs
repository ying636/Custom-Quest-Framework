using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFAction_ShowMapTitle : CQFAction_Target
{
    public CQFAction_ShowMapTitle() => targetsText = new List<string> { "Position" };

    public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

    public override void Draw(ref float y, Rect inRect, float x)
    {
        using CQFUIScope scope = new(inRect.width);
        base.Draw(ref y, inRect, x);
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_EntranceTitleText".Translate(), ref text, x, 240f);
        y += 30f;
    }

    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        if (string.IsNullOrWhiteSpace(text)) base.Work(targets, quest);
        else RealWork(targets, quest);
    }

    public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        GameComponent_MapManager manager = GameComponent_MapManager.Instance
            ?? throw new InvalidOperationException("[CQF] Map manager is unavailable.");
        if (!string.IsNullOrWhiteSpace(text))
        {
            manager.ShowMapEntranceText(text.CanTranslate() ? text.Translate().ToString() : text);
            return;
        }
        if (targets.Count != 1 || targets.Values.Single().Map is not Map map)
            throw new InvalidOperationException("[CQF] ShowMapTitle requires text or one map target.");
        manager.ShowMapEntranceTitle(map);
    }

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        if (!string.IsNullOrWhiteSpace(text)) result.Add(new XElement("text", text));
        return result;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref text, "text", string.Empty);
    }

    [CQFLocalizableText]
    public string text = string.Empty;
}
