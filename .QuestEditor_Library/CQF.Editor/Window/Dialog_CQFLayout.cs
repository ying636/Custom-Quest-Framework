using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Dialog_CQFLayout : Window
{
    public Dialog_CQFLayout(Map map, CustomMapStep_Layout? settings = null, CQFMapRegion? reference = null)
    {
        this.map = map;
        this.settings = settings ?? new CustomMapStep_Layout { referenceKey = reference?.id ?? "" };
        doCloseX = true; draggable = true; resizeable = true; forcePause = true;
    }

    public override Vector2 InitialSize => new Vector2(620f, 760f);

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new CQFUIScope(inRect.width, inRect.height);
        Widgets.Label(new Rect(0f, 0f, inRect.width - 32f, 30f), "CustomMapStep_Layout".Translate());
        Rect viewport = new Rect(0f, 38f, inRect.width, inRect.height - 88f);
        Rect content = new Rect(0f, 0f, viewport.width - 20f, height);
        Widgets.BeginScrollView(viewport, ref scroll, content);
        float y = 0f; settings.Draw(ref y, content, 0f);
        if (!string.IsNullOrEmpty(error)) { Widgets.Label(new Rect(0f, y, content.width, 100f), error); y += 104f; }
        height = y + 10f;
        Widgets.EndScrollView();
        if (CQFUIStyle.ButtonText(new Rect(0f, inRect.height - 40f, inRect.width, 34f), "CQF_Layout_Preview".Translate()))
        {
            try
            {
                CQFLayoutPlan plan = settings.Preview(map);
                Close(); Find.DesignatorManager.Select(new Designator_CQFLayoutPreview(map, settings, plan));
            }
            catch (Exception exception) { error = exception.Message; Log.Error(exception.ToString()); }
        }
    }

    private readonly Map map;
    private readonly CustomMapStep_Layout settings;
    private Vector2 scroll;
    private float height = 1200f;
    private string error = "";
}
