using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;
using System.Text;

namespace QuestEditor_Library
{
    public static class QuestNode_RandomCustomMapEditor
    {
        public static void Draw_0(QuestEditor_Library.QuestNode_RandomCustomMap cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            QuestNode_Root_CustomMapEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            y += 10f;
            Widgets.Label(new Rect(x, y, Mathf.Max(40f, inRect.width - x - 92f), 25f), "MapTags".Translate());
            CQFEditorTools.DrawButtonWithIcon(y, () => Find.WindowStack.Add(new Window_StringAndChance((t, c) => cqfReceiver.tags.SetOrAdd(t, c))), () =>
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (KeyValuePair<string, float> data in cqfReceiver.tags)
                {
                    options.Add(new FloatMenuOption(data.Key, () => cqfReceiver.tags.Remove(data.Key)));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }, inRect.width - 80f);
            y += 34f;
            foreach (KeyValuePair<string, float> tag in cqfReceiver.tags)
            {
                Widgets.Label(new Rect(x, y, Mathf.Max(40f, inRect.width - x - 12f), 25f), tag.Key + "*" + tag.Value);
                y += 30f;
            }
            y += 10f;
            CQFEditorTools.DrawButtonWithIcon(y, () => Find.WindowStack.Add(new Window_AddMapWithChance() { action = (data, chance) => cqfReceiver.datas.Add(data, chance) }), () =>
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (KeyValuePair<string, float> data in cqfReceiver.datas)
                {
                    options.Add(new FloatMenuOption(data.Key, () => cqfReceiver.datas.Remove(data.Key)));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }, x);
            y += 30f;
            StringBuilder datas = new StringBuilder();
            foreach (KeyValuePair<string, float> data in cqfReceiver.datas)
            {
                datas.AppendLine(data.Key + "，" + "GenerationChance".Translate() + data.Value * 100f + "%");
            }

            string text = "MapDatas".Translate(datas.ToString());
            float dataHeight = Mathf.Max(25f, Text.CalcHeight(text, Mathf.Max(40f, inRect.width - x - 19f)));
            Widgets.Label(new Rect(x + 7f, y, Mathf.Max(40f, inRect.width - x - 19f), dataHeight), text);
            y += dataHeight + 12f;
        }
    }
}
