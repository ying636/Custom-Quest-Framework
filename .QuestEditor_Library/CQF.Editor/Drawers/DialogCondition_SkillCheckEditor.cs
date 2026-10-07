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
    public static class DialogCondition_SkillCheckEditor
    {
        public static void Draw_0(QuestEditor_Library.DialogCondition_SkillCheck cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            DialogCondition_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            rect = new Rect(x, y, 150f, 25f);
            if (CQFUIStyle.ButtonText(rect, "RequiredSkill".Translate() + cqfReceiver.skill?.label, false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<SkillDef>.AllDefsListForReading, s => cqfReceiver.skill = s, s => s.label);
            }

            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CheckModifier".Translate(), ref cqfReceiver.checkModifier, ref cqfReceiver.buffer2, x);
            y += 30f;
        }
    }
}
