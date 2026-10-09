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
    public static class CQFAction_EndGameEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_EndGame cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            string source_message = cqfReceiver.message.CanTranslate() ? cqfReceiver.message.Translate().ToString() : cqfReceiver.message;
            string edited_message = source_message;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQFAction_EndGame_Message".Translate(), ref edited_message, x, 150f);
            if (edited_message != source_message) cqfReceiver.message = edited_message;
        }
    }
}
