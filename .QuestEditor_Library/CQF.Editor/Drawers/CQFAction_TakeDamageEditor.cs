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
    public static class CQFAction_TakeDamageEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_TakeDamage cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            if (CQFUIStyle.ButtonText(rect, "CQF_DamageType".Translate() + cqfReceiver.damage?.label, false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DamageDef>.AllDefsListForReading, d => cqfReceiver.damage = d, d => d.label);
            }

            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line<float>(y, "DamageAmount".Translate(), ref cqfReceiver.amount, ref cqfReceiver.buffer, x);
            y += 30f;
        }
    }
}
