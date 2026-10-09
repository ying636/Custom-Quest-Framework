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
    public static class CQFAction_UpgradeTraitEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_UpgradeTrait cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            if (CQFUIStyle.ButtonText(rect, "GiveTrait".Translate(cqfReceiver.trait?.defName), false))
            {
                Find.WindowStack.Add(new Dialog_Select<TraitDef>(new TextSelectDrawer<TraitDef>(DefDatabase<TraitDef>.AllDefsListForReading, t => t.defName, t =>
                {
                    cqfReceiver.trait = t;
                }, null, t =>
                {
                    StringBuilder tip = new StringBuilder();
                    foreach (var traitDegreeData in t.degreeDatas)
                    {
                        tip.AppendLine(traitDegreeData.label);
                    }

                    return tip.ToString().Trim();
                }, null, null, null, null), "Select".Translate()));
            }

            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "InitDegree".Translate(), ref cqfReceiver.initDegree, ref cqfReceiver.buffer, x, 100f);
            y += 30f;
            string source_initMessage = cqfReceiver.initMessage.CanTranslate() ? cqfReceiver.initMessage.Translate().ToString() : cqfReceiver.initMessage;
            string edited_initMessage = source_initMessage;
            CQFEditorTools.DrawLabelAndText_Line(y, "InitMessage".Translate(), ref edited_initMessage, x, 100f);
            if (edited_initMessage != source_initMessage) cqfReceiver.initMessage = edited_initMessage;
            y += 30f;
            string source_message = cqfReceiver.message.CanTranslate() ? cqfReceiver.message.Translate().ToString() : cqfReceiver.message;
            string edited_message = source_message;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQFMessage".Translate(), ref edited_message, x, 100f);
            if (edited_message != source_message) cqfReceiver.message = edited_message;
        }
    }
}
