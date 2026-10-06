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
            QuestNode_Root_CustomMapEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            y += 10f;
            CQFEditorTools.DrawButtonWithIcon(y, () => Find.WindowStack.Add(new Window_StringAndChance((t, c) => cqfReceiver.tags.SetOrAdd(t, c))), () =>
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (KeyValuePair<string, float> data in cqfReceiver.tags)
                {
                    options.Add(new FloatMenuOption(data.Key, () => cqfReceiver.tags.Remove(data.Key)));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }, x + 400f);
            float y2 = y + 60f;
            Widgets.Label(new Rect(x + 400f, y + 30f, 350f, 25f), "MapTags".Translate());
            cqfReceiver.tags.ToList().ForEach(t =>
            {
                Widgets.Label(new Rect(x + 400f, y2, 350f, 25f), t.Key + "*" + t.Value);
                y2 += 30f;
            });
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

            Widgets.Label(new Rect(x + 7f, y, 350f, 500f), "MapDatas".Translate(datas.ToString()));
            y += 180f;
        }
    }
}
