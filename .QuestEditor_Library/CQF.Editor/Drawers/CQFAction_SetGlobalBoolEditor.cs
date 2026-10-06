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
    public static class CQFAction_SetGlobalBoolEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_SetGlobalBool cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "keyOfBoolValue".Translate(), ref cqfReceiver.keyOfBool, x, 350f);
            y += 30f;
            Rect rect = new Rect(x, y, 250f, 25f);
            Widgets.CheckboxLabeled(rect, "valueOfBool".Translate(), ref cqfReceiver.valueOfBool);
            y += 30f;
        }
    }
}
