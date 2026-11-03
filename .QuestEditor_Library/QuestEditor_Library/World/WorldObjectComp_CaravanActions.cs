using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library;

public sealed class WorldObjectComp_CaravanActions : WorldObjectComp
{
    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (parent is not Caravan caravan) yield break;
        GameComponent_Editor component = GameComponent_Editor.Instance;
        foreach (CaravanActionDef definition in DefDatabase<CaravanActionDef>.AllDefsListForReading)
        {
            if (definition.conditions.Any(condition => !condition.Satisfied(caravan))) continue;
            Command_Action command = new()
            {
                defaultLabel = definition.label,
                defaultDesc = definition.description,
                icon = definition.Icon,
                Order = float.MaxValue,
                action = () =>
                {
                    component.CACDS.Add(definition, new CD
                    {
                        curTick = Find.TickManager.TicksGame,
                        time = Find.TickManager.TicksGame + definition.CD
                    });
                    foreach (WorldAction action in definition.actions) action.Work(caravan);
                }
            };
            if (!component.IsAvailable(definition)) command.Disable("CQFCooldown".Translate());
            yield return command;
        }
    }
}
