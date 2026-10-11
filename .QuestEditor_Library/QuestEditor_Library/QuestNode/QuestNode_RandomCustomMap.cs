using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestNode_RandomCustomMap : QuestNode_Root_CustomMap
    {
        public override CustomMapDataDef GetMap()
        {
            Dictionary<CustomMapDataDef, float> datas = new Dictionary<CustomMapDataDef, float>();
            this.datas.ToList().ForEach(x => datas.Add(DefDatabase<CustomMapDataDef>.GetNamed(x.Key), x.Value));
            if (this.tags != null)
            {
                DefDatabase<CustomMapDataDef>.AllDefsListForReading.ForEach(d =>
                {
                    d.tags.ForEach(t =>
                    {
                        if (this.tags.TryGetValue(t, out float weight))
                        {
                            datas.SetOrAdd(d, weight);
                        }
                    });
                });
            }

            return GenCollection.RandomElementByWeight(datas.Keys, d => datas[d]);
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            y += 10f;
            Widgets.Label(new Rect(x, y, Mathf.Max(40f, inRect.width - x - 92f), 25f), "MapTags".Translate());
            CQFEditorTools.DrawButtonWithIcon(y, () => Find.WindowStack.Add(new Window_StringAndChance((t, c) => this.tags.SetOrAdd(t, c))), () =>
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (KeyValuePair<string, float> data in this.tags)
                {
                    options.Add(new FloatMenuOption(data.Key, () => this.tags.Remove(data.Key)));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }, inRect.width - 80f);
            y += 34f;
            foreach (KeyValuePair<string, float> tag in this.tags)
            {
                Widgets.Label(new Rect(x, y, Mathf.Max(40f, inRect.width - x - 12f), 25f), tag.Key + "*" + tag.Value);
                y += 30f;
            }

            y += 10f;
            CQFEditorTools.DrawButtonWithIcon(y, () => Find.WindowStack.Add(new Window_AddMapWithChance() { action = (data, chance) => this.datas.Add(data, chance) }), () =>
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (KeyValuePair<string, float> data in this.datas)
                {
                    options.Add(new FloatMenuOption(data.Key, () => this.datas.Remove(data.Key)));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }, x);
            y += 30f;
            StringBuilder datas = new StringBuilder();
            foreach (KeyValuePair<string, float> data in this.datas)
            {
                datas.AppendLine(data.Key + "，" + "GenerationChance".Translate() + data.Value * 100f + "%");
            }

            string text = "MapDatas".Translate(datas.ToString());
            float dataHeight = Mathf.Max(25f, Text.CalcHeight(text, Mathf.Max(40f, inRect.width - x - 19f)));
            Widgets.Label(new Rect(x + 7f, y, Mathf.Max(40f, inRect.width - x - 19f), dataHeight), text);
            y += dataHeight + 12f;
        }

        public Dictionary<string, float> tags = new Dictionary<string, float>();
        public Dictionary<string, float> datas = new Dictionary<string, float>();
    }
}
