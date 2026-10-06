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
    public static class CQFAction_StartDialogEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_StartDialog cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x, y, 320f, 25f), "DialogManagerForSpawner".Translate(cqfReceiver.dialog?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, m => cqfReceiver.dialog = m, m => m.defName);
            }

            y += 30f;
            CQFTargetKeyEditor.DrawBookField(y, "Interviewer".Translate(), cqfReceiver.interviewerText, value => cqfReceiver.interviewerText = value, x, 150f, inRect.width - x - 20f);
            y += 30f;
            CQFTargetKeyEditor.DrawBookField(y, "Interviewee".Translate(), cqfReceiver.intervieeText, value => cqfReceiver.intervieeText = value, x, 150f, inRect.width - x - 20f);
            y += 30f;
        }
    }
}
