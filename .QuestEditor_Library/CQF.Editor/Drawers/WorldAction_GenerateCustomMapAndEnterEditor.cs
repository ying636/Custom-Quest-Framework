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
    public static class WorldAction_GenerateCustomMapAndEnterEditor
    {
        public static void Draw_0(QuestEditor_Library.WorldAction_GenerateCustomMapAndEnter cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            WorldActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "SiteIconPath".Translate(), ref cqfReceiver.siteIconPath, x, 150f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "ExpandingIconPath".Translate(), ref cqfReceiver.expandingIconPath, x, 150f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 300f, 25f), "ReplaceMapGeneration".Translate(), ref cqfReceiver.replaceMapGeneration);
            y += 30f;
            CQFEditorTools.DrawFactionSelectableText(y, "MapFaction".Translate(), ref cqfReceiver.faction, f => cqfReceiver.faction = f, x, 150f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 300f, 25f), "SetCaravanTileAsTarget".Translate(), ref cqfReceiver.setCaravanTileAsTarget);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 300f, 25f), "SentDefeatLetter".Translate(), ref cqfReceiver.sentDefeatLetter);
            y += 30f;
            CQFEditorTools.DrawSelectButton(x, ref y, "SitePartDef".Translate(cqfReceiver.part?.label ?? cqfReceiver.part?.defName), DefDatabase<SitePartDef>.AllDefsListForReading, d => cqfReceiver.part = d, d => d.label ?? d.defName);
            CQFEditorTools.DrawEditableList(cqfReceiver.customMapDataTags, ref y, (textField, t) =>
            {
                textField.width = Mathf.Min(textField.width, Mathf.Max(20f, inRect.width - textField.x - 192f));
                t.tag = Widgets.TextField(textField, t.tag);
                Rect chance = new Rect(textField.xMax + 8f, textField.y, 80f, 25f);
                Widgets.Label(chance, "LootChance".Translate());
                chance.x += 88f;
                chance.width = Mathf.Max(20f, Mathf.Min(80f, inRect.width - chance.x - 12f));
                Widgets.TextFieldPercent(chance, ref t.weight, ref t.buffer);
            }, t => t.tag, "TagWithChance".Translate(), "TagWithChance_Tip".Translate(), true, x, 350f);
            CQFEditorTools.DrawEditableList(cqfReceiver.customMapDatas, ref y, (textField, t) =>
            {
                textField.width = Mathf.Min(textField.width, Mathf.Max(20f, inRect.width - textField.x - 192f));
                string buttonText = "CustomMapDef".Translate(t.data?.label);
                if (CQFUIStyle.ButtonText(textField, buttonText, false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, d => t.data = d, d => d.label);
                }

                Rect chance = new Rect(textField.xMax + 8f, textField.y, 80f, 25f);
                Widgets.Label(chance, "Chance".Translate());
                chance.x += 88f;
                chance.width = Mathf.Max(20f, Mathf.Min(80f, inRect.width - chance.x - 12f));
                Widgets.TextFieldPercent(chance, ref t.weight, ref t.buffer);
            }, t => t.data?.label, "MapDefWithChance".Translate(), "MapDefWithChance_Tip".Translate(), true, x, 350f);
        }
    }
}
