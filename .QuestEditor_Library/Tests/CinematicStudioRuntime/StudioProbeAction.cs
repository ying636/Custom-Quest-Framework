using System.Xml.Linq;
using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.CinematicStudioChecks;

public sealed class StudioProbeAction : CQFAction
{
    public static int Executions { get; private set; }
    public static string ActorID { get; private set; } = "";

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        result.Add(new XElement("pawnKey", pawnKey));
        result.Add(new XElement("replacementName", replacementName));
        return result;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref pawnKey, "pawnKey", "CQF_StudioActor");
        Scribe_Values.Look(ref replacementName, "replacementName", "CQF_StudioMutation");
    }

    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        Pawn actor = targets[pawnKey].Thing as Pawn ?? throw new InvalidOperationException("Studio probe requires its real bound pawn.");
        actor.Rotation = Rot4.North;
        actor.Name = new NameSingle(replacementName);
        ActorID = actor.GetUniqueLoadID();
        Executions++;
    }

    public string pawnKey = "CQF_StudioActor";
    public string replacementName = "CQF_StudioMutation";
}
