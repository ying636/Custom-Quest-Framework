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
    public static class WorldCondition_WorldObjectEditor
    {
        public static void Draw_0(QuestEditor_Library.WorldCondition_WorldObject cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            WorldConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Widgets.CheckboxLabeled(new Rect(x, y, 300f, 25f), "NonHostile".Translate(), ref cqfReceiver.nonHostile);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 300f, 25f), "NonPlayer".Translate(), ref cqfReceiver.nonPlayer);
            y += 30f;
            CQFEditorTools.DrawSelectButton(x, ref y, "WorldConditionFaction".Translate(cqfReceiver.faction?.label ?? cqfReceiver.faction?.defName), DefDatabase<FactionDef>.AllDefsListForReading, d => cqfReceiver.faction = d, d => d.label);
            CQFEditorTools.DrawSelectButton(x, ref y, "WorldObjectDef".Translate(cqfReceiver.objectDef?.label ?? cqfReceiver.objectDef?.defName), DefDatabase<WorldObjectDef>.AllDefsListForReading, d => cqfReceiver.objectDef = d, d => d.label ?? d.defName);
        }
    }
}
