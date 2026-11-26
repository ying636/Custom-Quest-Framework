using System;
using System.Linq;
using System.Xml.Linq;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library;

public sealed class MetaLordSquadOrder
{
    public void Execute(MetaLord owner)
    {
        Lord[] squads = owner.Levels.SelectMany(level => level.Lords).ToArray();
        Pawn[] pawns = squads.Where(this.recipient.Matches).SelectMany(squad => squad.ownedPawns)
            .Where(pawn => this.CanReceive(owner, pawn)).Distinct().ToArray();
        if (pawns.Length == 0)
        {
            Log.Warning("[CQF] No available squad members for order in " + owner.name + ": " + (this.recipient.name ?? this.recipient.tag));
            return;
        }

        if (this.destination != null)
        {
            Lord[] matches = squads.Where(this.destination.Matches).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("[CQF] Squad order requires one destination squad: " + this.destination.name);
            Lord target = matches[0];
            foreach (Pawn pawn in pawns)
                owner.ValidateMove(pawn, target.Map, this.timeoutTicks);
            foreach (Pawn pawn in pawns)
            {
                if (this.CanReceive(owner, pawn) && pawn.GetLord() != target)
                    owner.RequestMove(pawn, target.Map, ((MultLevelLordJobBase)target.LordJob).groupName, this.timeoutTicks);
            }
            return;
        }

        foreach (Pawn pawn in pawns)
        {
            if (!this.CanReceive(owner, pawn)) continue;
            var component = GameComponent_ComplexDuty.Instance;
            if (component.TryGetRuntime(pawn)?.dutyMap != this.dutyMap)
                component.SetDutyMap(pawn, this.dutyMap, owner.quest);
            string node = this.nodeId.NullOrEmpty() ? this.dutyMap.StartNode?.nodeId : this.nodeId;
            if (!node.NullOrEmpty() && component.TryGetRuntime(pawn)?.currentNodeId != node && this.CanReceive(owner, pawn))
                component.SetNode(pawn, node, owner.quest);
        }
    }

    public XElement SaveToXElement()
    {
        var xml = new XElement("li", this.recipient.SaveToXElement("recipient"));
        if (this.destination != null)
            xml.Add(this.destination.SaveToXElement("destination"), new XElement("timeoutTicks", this.timeoutTicks));
        else
            xml.Add(new XElement("dutyMap", this.dutyMap?.defName), new XElement("nodeId", this.nodeId));
        return xml;
    }

    private bool CanReceive(MetaLord owner, Pawn pawn) =>
        !owner.Disposed && (pawn.Spawned || this.destination != null) && !pawn.Dead && !pawn.Downed && !pawn.IsPrisoner &&
        pawn.GetLord() is { } squad && squad.LordJob is MultLevelLordJobBase job &&
        job.metaLord == owner && this.recipient.Matches(squad);

    public MetaLordSquadSelector recipient = new();
    public MetaLordSquadSelector destination;
    public DutyMapDef dutyMap;
    [NoTranslate] public string nodeId;
    public int timeoutTicks = 60000;
}
