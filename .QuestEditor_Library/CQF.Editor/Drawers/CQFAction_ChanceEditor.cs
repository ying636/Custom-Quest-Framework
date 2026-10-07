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
    public static class CQFAction_ChanceEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Chance cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            if (cqfReceiver.action != null)
            {
                Widgets.DrawLine(new Vector2(x, y), new Vector2(inRect.width, y), CQFUIStyle.Accent, 1f);
                cqfReceiver.action.Draw(ref y, inRect, x);
                Widgets.DrawLine(new Vector2(x, y), new Vector2(inRect.width, y), CQFUIStyle.Accent, 1f);
                y += 5f;
            }

            if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 25f), "SelectAction".Translate(), false))
            {
                CQFEditorTools.OpenCQFActionSelect(a => cqfReceiver.action = (CQFAction)Activator.CreateInstance(a));
            }

            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "LootChance".Translate(), ref cqfReceiver.chance, ref cqfReceiver.buffer, x, 150f);
            y += 30f;
        }
    }
}
