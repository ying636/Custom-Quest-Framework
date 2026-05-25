using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CustomMapEntrance_Chance : CustomMapEntrance
    {
        public override void SetMapDef(CustomMapDataDef mapDef)
        {
            Dictionary<CustomMapDataDef, float> mapPool = new Dictionary<CustomMapDataDef, float>();
            this.mapDefWithChance.ForEach(m => mapPool.Add(m.def, m.chance));
            List<CustomMapDataDef> defs = DefDatabase<CustomMapDataDef>.AllDefsListForReading;
            foreach (TagWithChance tag in this.tagWithChance)
            {
                defs.FindAll(d => d.tags.Contains(tag.tag)).ForEach(d =>
                {
                    if (!mapPool.ContainsKey(d)) 
                    {
                        mapPool.Add(d, tag.chance * d.commonality);
                    }
                });
            }
            mapPool.RemoveAll(c => 
            {
                if (this.Map.Parent is MapParent_Custom parent0 && parent0.rootSite is CustomSite site0 && site0.GenerationCount.TryGetValue(c.Key,out int v) && v >= c.Key.generationLimit && c.Key.generationLimit != 0)
                {
                    return true;
                }
                return false;
            });
            if (mapPool.Any()) 
            {
                this.mapDef = GenCollection.RandomElementByWeight(mapPool, x => x.Value).Key;
                //if (DebugSettings.godMode)
                //{
                //    for (int i = 0;i<100;i++) 
                //    {
                //        Log.Message(GenCollection.RandomElementByWeight(mapPool, x => x.Value).Key.label);
                //    }
                //}
            }
            if (this.Map.Parent is MapParent_Custom parent && parent.rootSite is CustomSite site) 
            {
                site.GenerationCount.SetOrAdd(this.mapDef,!site.GenerationCount.ContainsKey(this.mapDef) ? 1 : site.GenerationCount[this.mapDef] + 1);
            }
        }
        public override void DrawTab()
        {
            Widgets.BeginScrollView(new Rect(7f, 25f, 475f, 590f), ref this.scrollPos, new Rect(7f, 10f, 475f, this.height));
            Widgets.DrawBox(new Rect(8f, 10f, 470f, this.height), 1, QuestEditor_Dialog.blueTex);
            float y = 20f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(15f, y, 900f, 38f), "CustomMap".Translate().Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            Rect rect = new Rect(300f,y,25f,25f);
            if (Widgets.ButtonImage(rect, TexButton.Copy))
            {
                CQFEditorTools.exitName = this.exitName;
                CQFEditorTools.tagWithChance = this.tagWithChance.ListFullCopy();
                CQFEditorTools.mapDefWithChance = this.mapDefWithChance.ListFullCopy();
            }
            TooltipHandler.TipRegion(rect, "Copy".Translate());
            rect.x += 30f;
            if (Widgets.ButtonImage(rect, TexButton.Paste))
            {
                this.exitName = CQFEditorTools.exitName;
                this.tagWithChance = CQFEditorTools.tagWithChance.ListFullCopy();
                this.mapDefWithChance = CQFEditorTools.mapDefWithChance.ListFullCopy();
            }
            TooltipHandler.TipRegion(rect, "Paste".Translate());
            y += 40f;
            CQFEditorTools.DrawEditableList<MapDefWithChance>(this.mapDefWithChance, ref y,(textField, t) => 
            {
                string buttomText = "CustomMapDef".Translate(t.def?.label);
                if (Widgets.ButtonText(textField, buttomText, false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, (x) => t.def = x, (x) => x.label);
                }
                Rect chance = new Rect(Text.CalcSize(buttomText).x + textField.x + 10f, textField.y, 100f, 25f);
                Widgets.Label(chance,"Chance".Translate());
                chance.x += 80f;
                Widgets.TextFieldPercent(chance, ref t.chance,ref t.buffer);
            },t => t.def?.label, "MapDefWithChance".Translate(), "MapDefWithChance_Tip".Translate(), true,15f,350f);
            CQFEditorTools.DrawEditableList<TagWithChance>(this.tagWithChance, ref y, (textField, t) =>
            {
                t.tag = Widgets.TextField(textField, t.tag);
                Rect chance = new Rect(textField.width + textField.x + 10f, textField.y, 100f, 25f);
                Widgets.Label(chance, "LootChance".Translate());
                chance.x += 80f;
                Widgets.TextFieldPercent(chance, ref t.chance, ref t.buffer);
            }, t => t.tag, "TagWithChance".Translate(), "TagWithChance_Tip".Translate(), true,15f,350f);
            y += 35f;       
            Widgets.CheckboxLabeled(new Rect(15f,y,350f,25f), "DefaultOpened".Translate(), ref this.opended);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "ExitName".Translate(), ref this.exitName, 15f, 150f);
            y += 30f;
            this.height = y + 5f;
            Widgets.EndScrollView();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.tagWithChance, "CQF_CustomMapEntrance_tagWithChance",LookMode.Deep);
            Scribe_Collections.Look(ref this.mapDefWithChance, "CQF_CustomMapEntrance_mapDefWithChance",LookMode.Deep);
        }

        public List<TagWithChance> tagWithChance = new List<TagWithChance>();
        public List<MapDefWithChance> mapDefWithChance = new List<MapDefWithChance>();
    }

    public class TagWithChance : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.chance, "TagWithChance_chance");
            Scribe_Values.Look(ref this.tag, "TagWithChance_tag");
        }

        public string buffer;
        public float chance = 1;
        public string tag = "defined";
    }
    public class MapDefWithChance : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.chance, "TagWithChance_chance");
            Scribe_Defs.Look(ref this.def, "TagWithChance_def");
        }

        public string buffer;
        public float chance = 1;
        public CustomMapDataDef def = null;
    }
}
