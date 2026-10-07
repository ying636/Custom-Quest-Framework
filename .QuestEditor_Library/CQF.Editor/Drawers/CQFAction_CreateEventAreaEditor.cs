using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_CreateEventAreaEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_CreateEventArea cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "EventAreaKey".Translate(), ref cqfReceiver.key, x, 150f);
            y += 30f;
            Rect rect = new Rect(x, y, 350f, 25f);
            CQFEditorTools.DrawFactionSelectableText(y, "EventAreaFaction".Translate(), ref cqfReceiver.faction, f => cqfReceiver.faction = f, 20f + x, 120f);
            TooltipHandler.TipRegion(rect, "EventAreaFactionTip".Translate());
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 350f, 25f), "EventAreaOnlyHumanlike".Translate(), ref cqfReceiver.onlyHumanlike);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 350f, 25f), "EventAreaReplaceExisting".Translate(), ref cqfReceiver.replaceExisting);
            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, cqfReceiver.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }
    }
}
