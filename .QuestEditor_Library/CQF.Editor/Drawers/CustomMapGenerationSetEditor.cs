using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CustomMapGenerationSetEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomMapGenerationSet cqfReceiver, ref float y, Rect inRect, float x)
        {
            y += 5f;
            CQFEditorTools.DrawEditableList(cqfReceiver.datas, ref y, (textField, t) =>
            {
                string buttomText = "CustomMapDef".Translate(t.data?.label);
                if (Widgets.ButtonText(textField, buttomText, false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, (d) => t.data = d, (d) => d.label);
                }

                Rect chance = new Rect(Text.CalcSize(buttomText).x + textField.x + 10f, textField.y, 100f, 25f);
                Widgets.Label(chance, "Chance".Translate());
                chance.x += 80f;
                Widgets.TextFieldPercent(chance, ref t.weight, ref t.buffer);
            }, t => t.data?.label, "MapDefWithChance".Translate(), "MapDefWithChance_Tip".Translate(), true, 15f, 350f);
            y += 5f;
            CQFEditorTools.DrawEditableList(cqfReceiver.tags, ref y, (textField, t) =>
            {
                t.tag = Widgets.TextField(textField, t.tag);
                Rect chance = new Rect(textField.width + textField.x + 10f, textField.y, 100f, 25f);
                Widgets.Label(chance, "LootChance".Translate());
                chance.x += 80f;
                Widgets.TextFieldPercent(chance, ref t.weight, ref t.buffer);
            }, t => t.tag, "TagWithChance".Translate(), "TagWithChance_Tip".Translate(), true, 15f, 350f);
            y += 5f;
        }
    }
}
