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
            Rect outRect = new Rect(0f, 0f, 540f, 590f);
            float width = outRect.width - 40f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, this.height + 10f));
            Widgets.BeginScrollView(outRect, ref this.scrollPos, viewRect);
            float x = 10f;
            float y = 10f;

            this.DrawCopyPasteHeader(ref y, x, width);
            this.DrawMapPool(ref y, x, width);
            this.DrawTagPool(ref y, x, width);

            this.DrawSectionHeader(ref y, x, width, "CQF_PortalSettingsSection".Translate(), "CQF_PortalSettingsSectionTip".Translate());
            Widgets.CheckboxLabeled(new Rect(x + 8f, y, width - 16f, 25f), "DefaultOpened".Translate(), ref this.opended);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "ExitName".Translate(), ref this.exitName, x + 8f, 150f);
            y += 30f;

            this.DrawActionSection(ref y, x, width, this.enterActions);
            this.height = y + 10f;
            Widgets.EndScrollView();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.tagWithChance, "CQF_CustomMapEntrance_tagWithChance",LookMode.Deep);
            Scribe_Collections.Look(ref this.mapDefWithChance, "CQF_CustomMapEntrance_mapDefWithChance",LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.tagWithChance ??= new List<TagWithChance>();
                this.mapDefWithChance ??= new List<MapDefWithChance>();
            }
        }

        private void DrawCopyPasteHeader(ref float y, float x, float width)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Widgets.Label(new Rect(x + 8f, y + 4f, width - 84f, 25f), "CustomMapEntrance_Chance".Translate().Colorize(ColorLibrary.SkyBlue));
            Rect buttonRect = new Rect(x + width - 66f, y + 2f, 25f, 25f);
            if (Widgets.ButtonImage(buttonRect, TexButton.Copy))
            {
                CQFEditorTools.exitName = this.exitName;
                CQFEditorTools.tagWithChance = this.tagWithChance.ListFullCopy();
                CQFEditorTools.mapDefWithChance = this.mapDefWithChance.ListFullCopy();
            }
            TooltipHandler.TipRegion(buttonRect, "CQF_ChanceCopySettingsTip".Translate());
            buttonRect.x += 30f;
            if (Widgets.ButtonImage(buttonRect, TexButton.Paste))
            {
                this.exitName = CQFEditorTools.exitName;
                this.tagWithChance = CQFEditorTools.tagWithChance.ListFullCopy();
                this.mapDefWithChance = CQFEditorTools.mapDefWithChance.ListFullCopy();
            }
            TooltipHandler.TipRegion(buttonRect, "CQF_ChancePasteSettingsTip".Translate());
            y += 38f;
        }

        private void DrawMapPool(ref float y, float x, float width)
        {
            this.DrawSectionHeader(ref y, x, width, "CQF_ChanceMapPoolSection".Translate(), "MapDefWithChance_Tip".Translate(),
                () => this.mapDefWithChance.Add(new MapDefWithChance()),
                () => CQFEditorTools.DrawFloatMenu(this.mapDefWithChance, item => this.mapDefWithChance.Remove(item), item => item.def?.label ?? "Null".Translate()),
                this.mapDefWithChance.Any());
            if (!this.mapDefWithChance.Any())
            {
                this.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_ChanceNoMapDefs".Translate());
                y += 8f;
                return;
            }
            foreach (MapDefWithChance item in this.mapDefWithChance)
            {
                Rect rowRect = new Rect(x + 8f, y, width - 16f, 30f);
                Widgets.DrawHighlightIfMouseover(rowRect);
                string mapLabel = item.def == null ? "Null".Translate().ToString() : item.def.label;
                Rect mapButtonRect = new Rect(rowRect.x + 4f, rowRect.y + 2f, 278f, 25f);
                if (Widgets.ButtonText(mapButtonRect, "CustomMapDef".Translate(mapLabel), false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, def => item.def = def, def => def.label);
                }
                Widgets.Label(new Rect(rowRect.x + 288f, rowRect.y + 2f, 70f, 25f), "Chance".Translate());
                Widgets.TextFieldPercent(new Rect(rowRect.x + 360f, rowRect.y + 2f, 110f, 25f), ref item.chance, ref item.buffer);
                y += 34f;
            }
            y += 8f;
        }

        private void DrawTagPool(ref float y, float x, float width)
        {
            this.DrawSectionHeader(ref y, x, width, "CQF_ChanceTagPoolSection".Translate(), "TagWithChance_Tip".Translate(),
                () => this.tagWithChance.Add(new TagWithChance()),
                () => CQFEditorTools.DrawFloatMenu(this.tagWithChance, item => this.tagWithChance.Remove(item), item => item.tag),
                this.tagWithChance.Any());
            if (!this.tagWithChance.Any())
            {
                this.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_ChanceNoTags".Translate());
                y += 8f;
                return;
            }
            foreach (TagWithChance item in this.tagWithChance)
            {
                Rect rowRect = new Rect(x + 8f, y, width - 16f, 30f);
                Widgets.DrawHighlightIfMouseover(rowRect);
                item.tag = Widgets.TextField(new Rect(rowRect.x + 4f, rowRect.y + 2f, 278f, 25f), item.tag);
                Widgets.Label(new Rect(rowRect.x + 288f, rowRect.y + 2f, 70f, 25f), "Chance".Translate());
                Widgets.TextFieldPercent(new Rect(rowRect.x + 360f, rowRect.y + 2f, 110f, 25f), ref item.chance, ref item.buffer);
                y += 34f;
            }
            y += 8f;
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
