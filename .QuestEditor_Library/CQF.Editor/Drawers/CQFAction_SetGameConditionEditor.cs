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
    public static class CQFAction_SetGameConditionEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_SetGameCondition cqfReceiver, ref float y, Rect inRect, float x)
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
                CQFEditorTools.DrawFloatMenu(DefDatabase<GameConditionDef>.AllDefsListForReading, f => cqfReceiver.condition = f, f => f.label);
            }

            y += 30f;
        }
    }
}
