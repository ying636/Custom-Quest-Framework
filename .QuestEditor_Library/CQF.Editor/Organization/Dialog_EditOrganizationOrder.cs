using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class Dialog_EditOrganizationOrder : Window
{
    internal Dialog_EditOrganizationOrder(MetaLordSquadOrder order, MetaLord? organization)
    {
        this.order = order;
        this.picker = new OrganizationSquadPicker(organization);
        this.doCloseX = true;
        this.forcePause = true;
        this.draggable = true;
    }

    public override Vector2 InitialSize => new(Mathf.Min(580, UI.screenWidth - 28), Mathf.Min(540, UI.screenHeight - 28));

    public override void DoWindowContents(Rect rect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0, 0, rect.width - 30, 32), "CQF_Organization_OrderTitle".Translate().Colorize(CQFUIStyle.Accent));
        Text.Font = GameFont.Small;
        Rect viewport = new(0, 38, rect.width, rect.height - 38);
        Rect content = new(0, 0, viewport.width - 18, Mathf.Max(this.height, viewport.height));
        Widgets.BeginScrollView(viewport, ref this.scroll, content);
        using var scope = new CQFUIScope(content.width, content.height);
        float y = 0;
        if (CQFUIStyle.ButtonText(new Rect(0, y, content.width, 28),
            (this.order.destination == null ? "CQF_Organization_OrderDuty" : "CQF_Organization_OrderMove").Translate()))
        {
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new("CQF_Organization_OrderDuty".Translate(), () => this.order.destination = null),
                new("CQF_Organization_OrderMove".Translate(), () => this.order.destination ??= new MetaLordSquadSelector())
            }));
        }
        y += 37;
        this.picker.Draw(ref y, content, this.order.recipient, "CQF_Organization_Recipient".Translate());
        if (this.order.destination != null)
        {
            this.picker.Draw(ref y, content, this.order.destination, "CQF_Organization_Destination".Translate(), allowTag: false);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordTimeout".Translate(), ref this.order.timeoutTicks, ref this.timeoutBuffer, 0f, 82);
            y += 34;
        }
        else
        {
            var dutyMap = this.order.dutyMap;
            string unavailable = "CQF_MetaLord_Unassigned".Translate();
            if (CQFUIStyle.ButtonText(new Rect(0, y, content.width, 28), "CQF_LordData_DutyMap".Translate(dutyMap?.LabelCap.ToString() ?? unavailable)))
                CQFEditorTools.DrawFloatMenu(DefDatabase<DutyMapDef>.AllDefsListForReading, graph =>
                {
                    this.order.dutyMap = graph;
                    this.order.nodeId = null;
                }, graph => graph.LabelCap);
            y += 34;
            if (dutyMap != null && CQFUIStyle.ButtonText(new Rect(0, y, content.width, 28),
                this.order.nodeId.NullOrEmpty() ? "CQF_Organization_DefaultNode".Translate().ToString() : this.order.nodeId))
            {
                var options = new List<FloatMenuOption>
                {
                    new("CQF_Organization_DefaultNode".Translate(), () => this.order.nodeId = null)
                };
                options.AddRange(dutyMap.nodes.Select(node => new FloatMenuOption(node.nodeId, () => this.order.nodeId = node.nodeId)));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            y += 34;
        }
        Widgets.EndScrollView();
        this.height = y + 12;
    }

    private readonly MetaLordSquadOrder order;
    private readonly OrganizationSquadPicker picker;
    private Vector2 scroll;
    private float height;
    private string? timeoutBuffer;
}
