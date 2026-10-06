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
    public static class WorldAction_GenerateSiteAndEnterEditor
    {
        public static void Draw_0(QuestEditor_Library.WorldAction_GenerateSiteAndEnter cqfReceiver, ref float y, Rect inRect, float x)
        {
            WorldActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawFactionSelectableText(y, "MapFaction".Translate(), ref cqfReceiver.faction, f => cqfReceiver.faction = f, x, 150f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 300f, 25f), "SetCaravanTileAsTarget".Translate(), ref cqfReceiver.setCaravanTileAsTarget);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 300f, 25f), "SentDefeatLetter".Translate(), ref cqfReceiver.sentDefeatLetter);
            y += 30f;
            CQFEditorTools.DrawSelectButton(x, ref y, "SitePartDef".Translate(cqfReceiver.part?.label ?? cqfReceiver.part?.defName), DefDatabase<SitePartDef>.AllDefsListForReading, d => cqfReceiver.part = d, d => d.label ?? d.defName);
        }
    }
}
