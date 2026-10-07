using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Xml;
using System.Xml.Linq;
using RimWorld.QuestGen;
using Verse.Grammar;
using System.Reflection;
using UnityEngine;
using System.Collections;
using Verse.AI;
using Verse.AI.Group;
using System.IO;
using Unity.Collections;
using RimWorld.Planet;
using System.Net.NetworkInformation;
using System.Text;

namespace QuestEditor_Library
{
    public static class CQFAction_ChangeGoodwillOfFactionEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_ChangeGoodwillOfFaction cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            Widgets.Label(rect, "Faction".Translate() + ":" + cqfReceiver.fixedFaction?.label);
            rect.x = 160f;
            if (CQFUIStyle.ButtonText(rect, "Select".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<FactionDef>.AllDefsListForReading, f => cqfReceiver.fixedFaction = f, f => f.label, new List<FloatMenuOption>() { new FloatMenuOption("Null".Translate(), () => cqfReceiver.fixedFaction = null) });
            }

            rect.y += 30f;
            rect.x = x;
            y += 30f;
            Widgets.CheckboxLabeled(rect, "IsIncrease".Translate(), ref cqfReceiver.isIncrease);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "GoodwillValue".Translate(), ref cqfReceiver.value, ref cqfReceiver.buffer, x);
            y += 30f;
            rect.y += 60f;
            Widgets.CheckboxLabeled(rect, "SendLetter".Translate(), ref cqfReceiver.sendLetter);
        }
    }
}
