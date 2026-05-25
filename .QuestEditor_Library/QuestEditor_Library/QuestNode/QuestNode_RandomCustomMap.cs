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
    public class QuestNode_RandomCustomMap : QuestNode_Root_CustomMap
    {
        public override CustomMapDataDef GetMap()
        {
            Dictionary<CustomMapDataDef,float> datas = new Dictionary<CustomMapDataDef, float>();
            this.datas.ToList().ForEach(x => datas.Add(DefDatabase<CustomMapDataDef>.GetNamed(x.Key),x.Value));
            if (this.tags != null) 
            {
                DefDatabase<CustomMapDataDef>.AllDefsListForReading.ForEach(d =>
                {
                    d.tags.ForEach(t =>
                    {
                        if (this.tags.TryGetValue(t,out float weight))
                        {
                            datas.SetOrAdd(d,weight);
                        }
                    });
                });
            }
            return GenCollection.RandomElementByWeight(datas.Keys,d => datas[d]);
        }

        public override void Draw(ref float y, Rect inRect,float x)
        {
            base.Draw(ref y, inRect,x); 
            y += 10f;

            CQFEditorTools.DrawButtonWithIcon(y, () => Find.WindowStack.Add(new Window_StringAndChance((t,c) => this.tags.SetOrAdd(t,c)))
, () =>
{
    List<FloatMenuOption> options = new List<FloatMenuOption>();
    foreach (KeyValuePair<string, float> data in this.tags)
    {
        options.Add(new FloatMenuOption(data.Key, () => this.tags.Remove(data.Key)));
    }
    Find.WindowStack.Add(new FloatMenu(options));
}, x + 400f);
            float y2 = y + 60f;    
            Widgets.Label(new Rect(x + 400f, y + 30f, 350f, 25f), "MapTags".Translate());
            this.tags.ToList().ForEach(t =>
            {
                Widgets.Label(new Rect(x + 400f,y2,350f,25f),t.Key + "*" +t.Value);
                y2 += 30f;
            });
            CQFEditorTools.DrawButtonWithIcon(y,() => Find.WindowStack.Add(new Window_AddMapWithChance() { action = (data, chance) => this.datas.Add(data, chance) })
            ,() => 
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (KeyValuePair<string, float> data in this.datas)
                {
                    options.Add(new FloatMenuOption(data.Key, () => this.datas.Remove(data.Key)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            },x);
            y += 30f;
            StringBuilder datas = new StringBuilder();
            foreach (KeyValuePair<string, float> data in this.datas)
            {
                datas.AppendLine(data.Key + "，" + "GenerationChance".Translate() + data.Value * 100f + "%");
            }
            Widgets.Label(new Rect(x + 7f, y, 350f, 500f), "MapDatas".Translate(datas.ToString()));

            y += 180f;
        }

        public Dictionary<string, float> tags = new Dictionary<string, float>();
        public Dictionary<string,float> datas = new Dictionary<string, float>();
    }

}
