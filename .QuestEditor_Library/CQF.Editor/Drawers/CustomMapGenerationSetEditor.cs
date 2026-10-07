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
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            y += 5f;
            CQFEditorTools.DrawEditableList(cqfReceiver.datas, ref y, (textField, t) =>
            {
                textField.width = Mathf.Min(textField.width, Mathf.Max(20f, inRect.width - textField.x - 192f));
                string buttomText = "CustomMapDef".Translate(t.data?.label);
                if (CQFUIStyle.ButtonText(textField, buttomText, false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, (d) => t.data = d, (d) => d.label);
                }

                Rect chance = new Rect(textField.xMax + 8f, textField.y, 80f, 25f);
                Widgets.Label(chance, "Chance".Translate());
                chance.x += 88f;
                chance.width = Mathf.Max(20f, Mathf.Min(80f, inRect.width - chance.x - 12f));
                Widgets.TextFieldPercent(chance, ref t.weight, ref t.buffer);
            }, t => t.data?.label, "MapDefWithChance".Translate(), "MapDefWithChance_Tip".Translate(), true, 15f, 350f);
            y += 5f;
            CQFEditorTools.DrawEditableList(cqfReceiver.tags, ref y, (textField, t) =>
            {
                textField.width = Mathf.Min(textField.width, Mathf.Max(20f, inRect.width - textField.x - 192f));
                t.tag = Widgets.TextField(textField, t.tag);
                Rect chance = new Rect(textField.xMax + 8f, textField.y, 80f, 25f);
                Widgets.Label(chance, "LootChance".Translate());
                chance.x += 88f;
                chance.width = Mathf.Max(20f, Mathf.Min(80f, inRect.width - chance.x - 12f));
                Widgets.TextFieldPercent(chance, ref t.weight, ref t.buffer);
            }, t => t.tag, "TagWithChance".Translate(), "TagWithChance_Tip".Translate(), true, 15f, 350f);
            y += 5f;
        }
    }
}
