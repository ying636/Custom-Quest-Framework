using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class Designator_CQFLayoutPreview : Designator
{
    public Designator_CQFLayoutPreview(Map map, CustomMapStep_Layout settings, CQFLayoutPlan plan)
    {
        this.map = map; this.settings = settings; this.plan = plan;
        defaultLabel = "CQF_Layout_Preview".Translate(); defaultDesc = "CQF_Layout_PreviewTip".Translate(); icon = TexButton.OpenDebugActionsMenu;
    }

    public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
    {
        get
        {
            yield return new FloatMenuOption("CQF_Layout_Generate".Translate(), () =>
            {
                try
                {
                    settings.Generate(map, new CustomMapGenerationContext(GameTools.GetQuestFromMap(map)), plan);
                    Find.DesignatorManager.Deselect();
                }
                catch (Exception exception) { Log.Error(exception.ToString()); Messages.Message(exception.Message, MessageTypeDefOf.RejectInput); }
            });
            yield return new FloatMenuOption("CQF_Layout_Settings".Translate(), () => Find.WindowStack.Add(new Dialog_CQFLayout(map, settings)));
        }
    }

    public override bool CanRemainSelected() => Find.CurrentMap == map;
    public override AcceptanceReport CanDesignateCell(IntVec3 loc) => false;

    public override void SelectedUpdate()
    {
        foreach (CQFLayoutPlacement part in plan.Parts) GenDraw.DrawFieldEdges(part.Bounds.Cells.ToList(), CQFUIStyle.Accent);
        GenDraw.DrawFieldEdges(plan.Roads.ToList(), CQFUIStyle.Link);
        GenDraw.DrawFieldEdges(plan.Protected.ToList(), CQFUIStyle.Muted);
    }

    private readonly Map map;
    private readonly CustomMapStep_Layout settings;
    private readonly CQFLayoutPlan plan;
}
