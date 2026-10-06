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
    public static class DialogCondition_ColonistCountEditor
    {
        public static void Draw_0(QuestEditor_Library.DialogCondition_ColonistCount cqfReceiver, ref float y, Rect inRect, float x)
        {
            DialogConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Widgets.CheckboxLabeled(new Rect(x, y, 325f, 20f), "NeedToBeGreater".Translate(), ref cqfReceiver.needGreater);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "NeedCount".Translate(), ref cqfReceiver.count, ref cqfReceiver.buffer, x, 100f);
            y += 30f;
        }
    }
}
