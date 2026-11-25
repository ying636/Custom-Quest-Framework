using RimWorld;
using System;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library;

public partial class LordJobData
{
    private bool HasVanillaConfiguration =>
        this.NeedsVanillaPoint || this.UsesVanillaFaction || this.lordJob == typeof(LordJob_ExitMapBest);

    private bool NeedsVanillaPoint =>
        this.lordJob == typeof(LordJob_DefendPoint) || this.lordJob == typeof(LordJob_ExitMapNear) ||
        this.lordJob == typeof(LordJob_Travel) || this.lordJob == typeof(LordJob_TravelAndExit) ||
        this.lordJob == typeof(LordJob_AssistColony);

    private bool UsesVanillaFaction =>
        this.lordJob == typeof(LordJob_AssaultColony) || this.lordJob == typeof(LordJob_AssaultThings) || this.lordJob == typeof(LordJob_AssistColony);

    private bool HasVanillaRadius => this.lordJob == typeof(LordJob_DefendPoint) || this.lordJob == typeof(LordJob_ExitMapNear);

    private bool HasVanillaUrgency => this.lordJob == typeof(LordJob_ExitMapBest) || this.lordJob == typeof(LordJob_ExitMapNear);

    internal LordJob CreateJobForGroup(Map map, Quest quest, IntVec3 origin, out Faction faction)
    {
        if (this.GetType() == typeof(LordJobData) && this.HasVanillaConfiguration)
        {
            faction = GameTools.GetFaction(this.lordData?.faction, map);
            return this.CreateVanillaJob(map, quest, faction, origin);
        }

        LordJob job = this.CreateJob(map, quest);
        faction = (job as MultLevelLordJobBase)?.metaLord.faction ?? GameTools.GetFaction(this.lordData?.faction, map);
        return job;
    }

    private LordJob CreateVanillaJob(Map map, Quest quest, Faction faction, IntVec3 origin)
    {
        Type job = this.lordJob;
        string label = LordJobData.JobLabel(job);
        if (this.UsesVanillaFaction && faction == null)
            throw new InvalidOperationException("CQF_LordJobFactionRequired".Translate(label));
        if (this.HasVanillaRadius && (!(this.vanillaRadius > 0f) || float.IsInfinity(this.vanillaRadius)))
            throw new InvalidOperationException("CQF_LordJobInvalidRadius".Translate(label));

        TargetInfo target = TargetInfo.Invalid;
        if (!this.vanillaTargetKey.NullOrEmpty() && (this.NeedsVanillaPoint || job == typeof(LordJob_AssaultThings)))
        {
            target = map.GetComponent<MapComponent_CQFTargets>().GetTarget(this.vanillaTargetKey);
            if (!target.IsValid) target = GameTools.GetTarget(null, quest, this.vanillaTargetKey);
            if (!target.IsValid || target.Map != map || !target.Cell.InBounds(map))
                throw new InvalidOperationException("CQF_LordJobInvalidTarget".Translate(label, this.vanillaTargetKey));
        }

        IntVec3 point = target.IsValid ? target.Cell : origin;
        if (this.NeedsVanillaPoint && !point.InBounds(map))
            throw new InvalidOperationException("CQF_LordJobInvalidTarget".Translate(label, this.vanillaTargetKey ?? string.Empty));

        if (job == typeof(LordJob_DefendPoint)) return new LordJob_DefendPoint(point, defendRadius: this.vanillaRadius);
        if (job == typeof(LordJob_ExitMapBest)) return new LordJob_ExitMapBest(this.vanillaUrgency);
        if (job == typeof(LordJob_ExitMapNear)) return new LordJob_ExitMapNear(point, this.vanillaUrgency, this.vanillaRadius);
        if (job == typeof(LordJob_Travel)) return new LordJob_Travel(point);
        if (job == typeof(LordJob_TravelAndExit)) return new LordJob_TravelAndExit(point);
        if (job == typeof(LordJob_AssaultColony)) return new LordJob_AssaultColony(faction);
        if (job == typeof(LordJob_AssistColony)) return new LordJob_AssistColony(faction, point);
        if (!target.HasThing || target.ThingDestroyed)
            throw new InvalidOperationException("CQF_LordJobThingRequired".Translate(label));
        return new LordJob_AssaultThings(faction, Gen.YieldSingle(target.Thing).ToList());
    }

    private void DrawVanillaConfiguration(ref float y, Rect rect, float x)
    {
        if (!this.JobSelectable || !this.HasVanillaConfiguration) return;
        float width = rect.width - x;
        if (this.NeedsVanillaPoint || this.lordJob == typeof(LordJob_AssaultThings))
        {
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_LordJobTargetField".Translate(), ref this.vanillaTargetKey, x, width);
            TooltipHandler.TipRegion(new Rect(x, y, width, 25f), (this.lordJob == typeof(LordJob_AssaultThings)
                ? "CQF_LordJobThingTargetTip" : "CQF_LordJobPointTargetTip").Translate());
            y += 34f;
        }
        if (this.HasVanillaRadius)
        {
            CQFEditorTools.DrawLabelAndText_Line<float>(y, "CQF_LordJobRadiusField".Translate(), ref this.vanillaRadius, ref this.vanillaRadiusBuffer, x, width);
            y += 34f;
        }
        if (this.HasVanillaUrgency)
        {
            using var scope = new CQFUIScope(rect.width);
            Rect input = CQFEditorTools.DrawFieldLabel(y, "CQF_LordJobUrgencyField".Translate(), x, width);
            if (CQFUIStyle.ButtonText(input, ("CQF_LordJobUrgency_" + this.vanillaUrgency).Translate(), overrideTextAnchor: TextAnchor.MiddleLeft))
            {
                CQFEditorTools.DrawFloatMenu(new[] { LocomotionUrgency.Amble, LocomotionUrgency.Walk, LocomotionUrgency.Jog, LocomotionUrgency.Sprint }.ToList(),
                    value => this.vanillaUrgency = value, value => ("CQF_LordJobUrgency_" + value).Translate());
            }
            y += 34f;
        }
    }

    private void SaveVanillaConfiguration(XElement result)
    {
        if (!this.HasVanillaConfiguration) return;
        if (!this.vanillaTargetKey.NullOrEmpty() && (this.NeedsVanillaPoint || this.lordJob == typeof(LordJob_AssaultThings)))
            result.Add(new XElement("vanillaTargetKey", this.vanillaTargetKey));
        if (this.HasVanillaRadius && this.vanillaRadius != 12f)
            result.Add(new XElement("vanillaRadius", this.vanillaRadius));
        if (this.HasVanillaUrgency && this.vanillaUrgency != LocomotionUrgency.Jog)
            result.Add(new XElement("vanillaUrgency", this.vanillaUrgency));
    }

    private static string JobLabel(Type type) => type.Name.CanTranslate() ? type.Name.Translate().ToString() : type.Name;

    private string? UnavailableJobReason(Type type)
    {
        if (type == typeof(LordJob_BegForItems) || type == typeof(LordJob_BestowingCeremony) ||
            type == typeof(LordJob_BossgroupAssaultColony) || type == typeof(LordJob_CreepJoiner) ||
            type == typeof(LordJob_DefendAttackedTraderCaravan))
            return "CQF_LordJobTaskContext".Translate(LordJobData.JobLabel(type));
        return type.GetConstructor(Type.EmptyTypes) == null
            ? "CQF_LordJobNoConstructor".Translate(LordJobData.JobLabel(type)).ToString() : null;
    }

    [NoTranslate] public string vanillaTargetKey;
    public float vanillaRadius = 12f;
    public LocomotionUrgency vanillaUrgency = LocomotionUrgency.Jog;
    private string vanillaRadiusBuffer;
}
