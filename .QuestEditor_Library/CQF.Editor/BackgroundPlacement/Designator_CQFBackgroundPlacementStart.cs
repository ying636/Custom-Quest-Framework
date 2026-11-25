#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Designator_CQFBackgroundPlacementStart : Designator
{
    public Designator_CQFBackgroundPlacementStart()
    {
        this.defaultLabel = "CQF_BackgroundPlacementStart".Translate();
        this.defaultDesc = "CQF_BackgroundPlacementStartTip".Translate();
        this.icon = TexButton.OpenDebugActionsMenu;
    }

    public override bool Visible => CQFEditorBridge.IsLoaded;
    public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
    {
        get
        {
            yield return new FloatMenuOption("CQF_OpenFloatingPalette".Translate(), () =>
            {
                if (this.Visible && this.CheckCanInteract() && Find.CurrentMap is { } map)
                    this.Begin(map, false);
            });
        }
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc) => false;

    public override void ProcessInput(Event ev)
    {
        if (!this.Visible || !this.CheckCanInteract() || Find.CurrentMap == null) return;
        this.Begin(Find.CurrentMap, true);
    }

    public void Begin(Map map, bool activateSelection = false)
    {
        if (Find.WindowStack.TryGetWindow<Window_CQFBackgroundPlacement>(out var open))
        {
            Find.WindowStack.Notify_ManuallySetFocus(open);
            if (activateSelection) open.ActivateSelection();
            return;
        }
        var session = new CQFBackgroundPlacementSession(map);
        var window = new Window_CQFBackgroundPlacement(session, new Designator_CQFBackgroundPlacement(session));
        Find.WindowStack.Add(window);
        if (activateSelection) window.ActivateSelection();
    }
}
#endif
