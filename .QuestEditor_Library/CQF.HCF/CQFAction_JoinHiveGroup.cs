using System.Xml.Linq;
using HiveCreatureFramework;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace HCFWithCQF.Actions;

public sealed class CQFAction_JoinHiveGroup : CQFAction_Target
{
    public CQFAction_JoinHiveGroup()
    {
        this.targetsText = new List<string> { "Pawn" };
    }

    public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

    public override void Draw(ref float y, Rect inRect, float x)
    {
        using CQFUIScope scope = new CQFUIScope(inRect.width);
        base.Draw(ref y, inRect, x);
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_HCF_HiveKey".Translate(), ref this.hiveKey, x, 150f);
        TooltipHandler.TipRegion(new Rect(x, y, inRect.width - x - 12f, 25f), "CQF_HCF_HiveKey_Tip".Translate());
        y += 30f;
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_HCF_GroupIndex".Translate(), ref this.groupIndex, ref this.groupIndexBuffer, x, 150f);
        TooltipHandler.TipRegion(new Rect(x, y, inRect.width - x - 12f, 25f), "CQF_HCF_JoinGroupIndex_Tip".Translate());
        y += 30f;
    }

    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        this.RealWork(targets, quest);
    }

    public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        if (this.hiveKey.NullOrEmpty() || this.groupIndex < -1)
            throw new InvalidOperationException("CQF_HCF_InvalidJoinSettings".Translate());

        Thing hive = GameTools.GetTarget(targets, quest, this.hiveKey).Thing;
        if (hive == null || hive.Destroyed)
            throw new InvalidOperationException("CQF_HCF_HiveMissing".Translate(this.hiveKey));

        IEnumerable<UnitGroup> ownedGroups = hive is Pawn node
            ? HCFGameUtility.GetNodeGroups(node)
            : hive.TryGetComp<CompHiveGroup>()?.groups ?? Enumerable.Empty<UnitGroup>();
        List<UnitGroup> groups = ownedGroups.Where(group => group != null).Distinct().OrderBy(group => group.id).ToList();
        if (groups.Count == 0 || this.groupIndex >= groups.Count)
            throw new InvalidOperationException("CQF_HCF_GroupMissing".Translate(hive.LabelShort, this.groupIndex));

        Thing[] selected = GameTools.GetTargets(targets, quest, this.targetsText).Values.Select(target => target.Thing).Distinct().ToArray();
        if (selected.Length == 0)
            throw new InvalidOperationException("CQF_HCF_NoTargets".Translate());

        foreach (Thing thing in selected)
        {
            if (thing is not Pawn pawn || pawn.Destroyed)
            {
                Log.Error("CQF_HCF_InvalidUnit".Translate(thing?.ToString() ?? "null"));
                continue;
            }

            if (!pawn.Spawned || !hive.Spawned || pawn.Map != hive.Map)
            {
                Log.Error("CQF_HCF_DifferentMap".Translate(pawn.LabelShort, hive.LabelShort));
                continue;
            }

            IEnumerable<UnitGroup> candidates = this.groupIndex < 0 ? groups : new[] { groups[this.groupIndex] };
            UnitGroup currentGroup = HCFGameUtility.GetUnitComp(pawn)?.group;
            if (candidates.Any(group => group == currentGroup && group.units.Contains(pawn)))
                continue;

            UnitGroup destination = null;
            List<string> reasons = new List<string>();
            foreach (UnitGroup group in candidates)
            {
                var acceptance = group.CanAccept(pawn);
                if (acceptance.Accepted)
                {
                    destination = group;
                    break;
                }
                reasons.Add(acceptance.Reason ?? "CQF_HCF_UnknownRejection".Translate().ToString());
            }

            if (destination == null)
            {
                Log.Error("CQF_HCF_JoinRejected".Translate(pawn.LabelShort, hive.LabelShort, string.Join("; ", reasons)));
                continue;
            }

            destination.AcceptUnit(pawn);
        }
    }

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        result.Add(new XElement("hiveKey", this.hiveKey ?? string.Empty));
        result.Add(new XElement("groupIndex", this.groupIndex));
        return result;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref this.hiveKey, "hiveKey", "Hive");
        Scribe_Values.Look(ref this.groupIndex, "groupIndex", -1);
    }

    [NoTranslate]
    public string hiveKey = "Hive";
    public int groupIndex = -1;
    private string groupIndexBuffer;
}
