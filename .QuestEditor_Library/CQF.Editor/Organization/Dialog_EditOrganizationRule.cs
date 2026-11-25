using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class Dialog_EditOrganizationRule : Window
{
    internal Dialog_EditOrganizationRule(MetaLordEvent entry, MetaLord? organization)
    {
        this.entry = entry;
        this.organization = organization;
        this.picker = new OrganizationSquadPicker(organization);
        this.doCloseX = true;
        this.forcePause = true;
        this.draggable = true;
        this.entry.trigger ??= new MetaLordEventTrigger { type = LordEventSignalType.External };
    }

    public override Vector2 InitialSize => new(Mathf.Min(620, UI.screenWidth - 28), Mathf.Min(650, UI.screenHeight - 28));

    public override void DoWindowContents(Rect rect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0, 0, rect.width - 30, 32), "CQF_Organization_RuleTitle".Translate().Colorize(CQFUIStyle.Accent));
        Text.Font = GameFont.Small;
        Rect viewport = new(0, 38, rect.width, rect.height - 38);
        Rect content = new(0, 0, viewport.width - 18, Mathf.Max(this.height, viewport.height));
        Widgets.BeginScrollView(viewport, ref this.scroll, content);
        using var scope = new CQFUIScope(content.width, content.height);
        float y = 0;
        this.DrawTrigger(ref y, content);
        this.DrawOrders(ref y, content);
        CQFEditorTools.DrawActionList_UseWindow(ref y, 0, this.entry.actions, content,
            "CQF_Organization_MoreActions".Translate(), action => action.GetType().Name.Translate());
        Widgets.EndScrollView();
        this.height = y + 14;
    }

    private void DrawTrigger(ref float y, Rect rect)
    {
        MetaLordEventTrigger trigger = this.entry.trigger;
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_Organization_EventKey".Translate(), ref this.entry.key, 0f, rect.width - 160);
        y += 32;
        Rect button = new(0, y, rect.width, 28);
        if (CQFUIStyle.ButtonText(button, ("CQF_Organization_Trigger_" + trigger.type).Translate()))
            CQFEditorTools.DrawFloatMenu(Enum.GetValues(typeof(LordEventSignalType)).Cast<LordEventSignalType>().ToList(), type =>
            {
                trigger.type = type;
                if (type is LordEventSignalType.Tick or LordEventSignalType.MapRemoved)
                {
                    trigger.sourceSquad = null;
                    trigger.groupTag = null;
                }
                if (type != LordEventSignalType.External) trigger.signalTag = null;
            }, type => ("CQF_Organization_Trigger_" + type).Translate());
        y += 34;
        if (trigger.type == LordEventSignalType.External)
        {
            CQFSignalEditor.DrawBookField(y, "CQF_MetaLordSignal".Translate(), trigger.signalTag, tag => trigger.signalTag = tag, 0f, rect.width);
            y += 32;
        }
        if (trigger.type is not (LordEventSignalType.Tick or LordEventSignalType.MapRemoved))
        {
            if (!trigger.groupTag.NullOrEmpty() && trigger.sourceSquad == null)
            {
                trigger.sourceSquad = new MetaLordSquadSelector { tag = trigger.groupTag };
                trigger.groupTag = null;
            }
            bool filter = trigger.sourceSquad != null;
            Widgets.CheckboxLabeled(new Rect(0, y, rect.width, 26), "CQF_Organization_FilterSender".Translate(), ref filter);
            if (filter) trigger.sourceSquad ??= new MetaLordSquadSelector();
            else trigger.sourceSquad = null;
            y += 31;
            if (trigger.sourceSquad != null)
                this.picker.Draw(ref y, rect, trigger.sourceSquad, "CQF_Organization_Sender".Translate());
        }
        Widgets.CheckboxLabeled(new Rect(0, y, rect.width, 26), "CQF_Organization_Once".Translate(), ref this.entry.once);
        y += 31;
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_Organization_Cooldown".Translate(), ref this.entry.cooldownTicks, ref this.cooldownBuffer, 0f, 82);
        y += 30;
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_Organization_Minimum".Translate(), ref trigger.minimumMembers, ref this.minBuffer, 0f, 82);
        y += 30;
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_Organization_Maximum".Translate(), ref trigger.maximumMembers, ref this.maxBuffer, 0f, 82);
        y += 38;
    }

    private void DrawOrders(ref float y, Rect rect)
    {
        Widgets.Label(new Rect(0, y, rect.width - 112, 28), "CQF_Organization_Orders".Translate().Colorize(CQFUIStyle.Accent));
        if (CQFUIStyle.ButtonText(new Rect(rect.width - 108, y, 108, 27), "CQF_Organization_AddOrder".Translate()))
        {
            var order = new MetaLordSquadOrder();
            this.entry.orders.Add(order);
            Find.WindowStack.Add(new Dialog_EditOrganizationOrder(order, this.organization));
        }
        y += 34;
        foreach (var order in this.entry.orders.ToArray())
        {
            string name = order.recipient.name.NullOrEmpty() ? order.recipient.tag : order.recipient.name;
            string type = (order.destination == null ? "CQF_Organization_OrderDuty" : "CQF_Organization_OrderMove").Translate();
            string rowLabel = "CQF_Organization_OrderRow".Translate(name.NullOrEmpty() ? "CQF_MetaLord_Unassigned".Translate().ToString() : name, type);
            if (CQFUIStyle.ButtonText(new Rect(0, y, rect.width - 35, 28), rowLabel, overrideTextAnchor: TextAnchor.MiddleLeft))
                Find.WindowStack.Add(new Dialog_EditOrganizationOrder(order, this.organization));
            if (CQFUIStyle.ButtonImage(new Rect(rect.width - 27, y + 2, 23, 23), TexButton.Delete, tooltip: "Delete".Translate()))
                this.entry.orders.Remove(order);
            y += 34;
        }
        y += 12;
    }

    private readonly MetaLordEvent entry;
    private readonly MetaLord? organization;
    private readonly OrganizationSquadPicker picker;
    private Vector2 scroll;
    private float height;
    private string? cooldownBuffer;
    private string? minBuffer;
    private string? maxBuffer;
}
