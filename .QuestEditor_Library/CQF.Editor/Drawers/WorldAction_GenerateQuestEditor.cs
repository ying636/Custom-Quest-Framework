using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine.Tilemaps;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class WorldAction_GenerateQuestEditor
    {
        public static void Draw_0(QuestEditor_Library.WorldAction_GenerateQuest cqfReceiver, ref float y, Rect inRect, float x)
        {
            WorldActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawSelectButton(x, ref y, "CQFQuestDef".Translate(cqfReceiver.quest?.label ?? cqfReceiver.quest?.defName), DefDatabase<QuestScriptDef>.AllDefsListForReading, d => cqfReceiver.quest = d, d => d.label ?? d.defName);
            Widgets.CheckboxLabeled(new Rect(x, y, 300f, 25f), "SetCaravanTileAsTarget".Translate(), ref cqfReceiver.setCaravanTileAsTarget);
            y += 30f;
        }
    }
}
