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
    public static class CQFAction_GainExperienceEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_GainExperience cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            if (Widgets.ButtonText(rect, "SkillType".Translate(cqfReceiver.skill == null ? "Random".Translate().ToString() : cqfReceiver.skill?.label), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<SkillDef>.AllDefsListForReading, d => cqfReceiver.skill = d, d => d.label, new List<FloatMenuOption>() { new FloatMenuOption("Random".Translate().ToString(), () => cqfReceiver.skill = null) });
            }

            y += 30f;
            CQFEditorTools.DrawFloatRange(ref y, "GainExperienceRange".Translate(), ref cqfReceiver.experienceRange, ref cqfReceiver.buffer, ref cqfReceiver.maxBuffer, x, 100f);
            y += 30f;
        }
    }
}
