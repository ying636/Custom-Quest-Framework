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
    public static class CQFAction_SetGameConditionWithActionsEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_SetGameConditionWithActions cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Widgets.CheckboxLabeled(new Rect(x, y, 150f, 25f), "IsPermanent".Translate(), ref cqfReceiver.permanent);
            y += 30f;
            if (!cqfReceiver.permanent)
            {
                CQFEditorTools.DrawIntRange(ref y, "Duration".Translate(), ref cqfReceiver.duration, ref cqfReceiver.buffer, ref cqfReceiver.maxBuffer, x, 100f);
            }

            if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 25f), "CQFGameConditionDef".Translate(cqfReceiver.condition?.label), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<GameConditionDef>.AllDefsListForReading.FindAll(c => typeof(GameCondition_Actions).IsAssignableFrom(c.conditionClass)), f => cqfReceiver.condition = f, f => f.label);
            }

            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, cqfReceiver.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 360f, 30f), "UseTick".Translate(), ref cqfReceiver.useTick);
            y += 35f;
            if (cqfReceiver.useTick)
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "TickToTrigger".Translate(), ref cqfReceiver.tick, ref cqfReceiver.tickBuffer);
                y += 35f;
            }
        }
    }
}
