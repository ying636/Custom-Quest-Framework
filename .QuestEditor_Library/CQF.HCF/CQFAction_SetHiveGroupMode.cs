using System.Xml.Linq;
using HiveCreatureFramework;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace HCFWithCQF.Actions;

public sealed class CQFAction_SetHiveGroupMode : CQFAction_Target
{
    public CQFAction_SetHiveGroupMode()
    {
        this.targetsText = new List<string> { "Hive" };
    }

    public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

    public override void Draw(ref float y, Rect inRect, float x)
    {
        using CQFUIScope scope = new CQFUIScope(inRect.width);
        base.Draw(ref y, inRect, x);
        if (CQFUIStyle.ButtonText(new Rect(x, y, Mathf.Max(40f, inRect.width - x - 12f), 30f),
            "CQF_HCF_Mode".Translate(this.mode?.LabelCap.ToString() ?? "CQF_HCF_None".Translate().ToString()), false))
        {
            List<GroupWorkModeDef> modes = DefDatabase<GroupWorkModeDef>.AllDefsListForReading.OrderBy(def => def.order).ThenBy(def => def.defName).ToList();
            Find.WindowStack.Add(new Dialog_Select<GroupWorkModeDef>(new TextSelectDrawer<GroupWorkModeDef>(modes,
                def => def.LabelCap, def => this.mode = def, getTip: def => def.description, getRawText: def => def.defName), "CQF_HCF_SelectMode".Translate()));
        }
        y += 36f;
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_HCF_GroupIndex".Translate(), ref this.groupIndex, ref this.groupIndexBuffer, x, 150f);
        TooltipHandler.TipRegion(new Rect(x, y, inRect.width - x - 12f, 25f), "CQF_HCF_ModeGroupIndex_Tip".Translate());
        y += 30f;
    }

    public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        if (this.mode == null || this.groupIndex < -1)
            throw new InvalidOperationException("CQF_HCF_InvalidModeSettings".Translate());
        if (targets.Count == 0)
            throw new InvalidOperationException("CQF_HCF_NoTargets".Translate());

        HashSet<UnitGroup> processed = new HashSet<UnitGroup>();
        foreach (Thing thing in targets.Values.Select(target => target.Thing).Distinct())
        {
            if (thing == null || thing.Destroyed)
            {
                Log.Error("CQF_HCF_InvalidGroupTarget".Translate(thing?.ToString() ?? "null"));
                continue;
            }

            IEnumerable<UnitGroup> ownedGroups = thing is Pawn node
                ? HCFGameUtility.GetNodeGroups(node)
                : thing.TryGetComp<CompHiveGroup>()?.groups ?? Enumerable.Empty<UnitGroup>();
            List<UnitGroup> groups = ownedGroups.Where(group => group != null).Distinct().OrderBy(group => group.id).ToList();
            if (groups.Count == 0 && HCFGameUtility.GetUnitComp(thing)?.group is UnitGroup memberGroup)
                groups.Add(memberGroup);
            if (groups.Count == 0 || this.groupIndex >= groups.Count)
            {
                Log.Error("CQF_HCF_GroupMissing".Translate(thing.LabelShort, this.groupIndex));
                continue;
            }

            foreach (UnitGroup group in this.groupIndex < 0 ? groups : new[] { groups[this.groupIndex] }.AsEnumerable())
            {
                if (!processed.Add(group))
                    continue;
                GroupWorkModeDef resolvedMode = this.mode.Worker.CanGroupSupport(group) ? this.mode.Worker.ResolveMode(group) : null;
                if (resolvedMode == null || !resolvedMode.Worker.CanGroupSupport(group))
                {
                    Log.Error("CQF_HCF_ModeUnsupported".Translate(thing.LabelShort, group.id, this.mode.LabelCap));
                    continue;
                }
                group.SetMode(this.mode, false);
            }
        }
    }

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        if (this.mode != null)
            result.Add(new XElement("mode", this.mode.defName));
        result.Add(new XElement("groupIndex", this.groupIndex));
        return result;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref this.mode, "mode");
        Scribe_Values.Look(ref this.groupIndex, "groupIndex", -1);
    }

    public GroupWorkModeDef mode;
    public int groupIndex = -1;
    private string groupIndexBuffer;
}
