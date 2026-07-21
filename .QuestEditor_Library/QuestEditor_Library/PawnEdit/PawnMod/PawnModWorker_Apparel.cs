using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Apparel : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.KindDef == null || pawnDef.KindDef.race.race.Humanlike;
        }

        public override PawnModData CreateData()
        {
            return new PawnModData_Apparel();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Apparel modData = pawnDef.DataFor<PawnModData_Apparel>();
            this.RemoveDuplicateLayers(modData.apparels);
            foreach (ApparelLayerDef layer in this.AvailableLayers())
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 36f);
                Widgets.DrawLightHighlight(row);
                ThingData data = this.ApparelForLayer(modData.apparels, layer);
                Rect layerRect = new Rect(row.x + 8f, row.y + 6f, 120f, 24f);
                Widgets.Label(layerRect, this.LayerLabel(layer).Colorize(ColorLibrary.PaleBlue));
                Rect iconRect = new Rect(layerRect.xMax + 8f, row.y + 4f, 28f, 28f);
                if (data?.def?.uiIcon != null)
                {
                    Widgets.DefIcon(iconRect, data.def, this.StuffFor(data.def, data.stuff));
                }
                float deleteWidth = data?.def == null ? 0f : 76f;
                Rect buttonRect = new Rect(iconRect.xMax + 8f, row.y + 3f, row.xMax - iconRect.xMax - deleteWidth - 16f, 30f);
                if (this.DrawTextButton(buttonRect, this.ThingLabel(data)))
                {
                    this.OpenLayerSelectDialog(modData.apparels, layer);
                }
                if (data?.def != null && this.DrawCommandText(new Rect(row.xMax - 76f, row.y + 3f, 68f, 30f), "CQF_PawnEditor_Delete".Translate()))
                {
                    this.ClearLayer(modData.apparels, layer);
                }
                y += 42f;
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.apparel == null)
            {
                return;
            }
            pawn.apparel.DestroyAll();
            PawnModData_Apparel modData = pawnDef.DataFor<PawnModData_Apparel>();
            this.RemoveDuplicateLayers(modData.apparels);
            foreach (ThingData data in modData.apparels)
            {
                if (data?.def == null)
                {
                    continue;
                }
                Apparel apparel = ThingMaker.MakeThing(data.def, this.StuffFor(data.def, data.stuff)) as Apparel;
                if (apparel != null)
                {
                    pawn.apparel.Wear(apparel);
                }
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            if (node["apparels"] != null)
            {
                pawnDef.DataFor<PawnModData_Apparel>().apparels = this.LoadSaveableList<ThingData>(node["apparels"]);
            }
        }

        private void OpenLayerSelectDialog(List<ThingData> apparels, ApparelLayerDef layer)
        {
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => this.ApparelInLayer(def, layer)).ToList();
            ThingData data = this.ApparelForLayer(apparels, layer) ?? new ThingData();
            this.OpenSelectDialog(data, defs, () => this.SetLayerApparel(apparels, layer, data));
        }

        private void OpenSelectDialog(ThingData data, List<ThingDef> defs, Action onSelected = null)
        {
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(new LabeledTextureSelectDrawer<ThingDef>(defs, def => def.uiIcon, def => def.label, def =>
            {
                if (def.MadeFromStuff)
                {
                    this.OpenStuffDialog(data, def, onSelected);
                    return;
                }
                this.SetThingData(data, def, null);
                onSelected?.Invoke();
            }, def => def.MadeFromStuff ? def.GetColorForStuff(GenStuff.DefaultStuffFor(def)) : def.uiIconColor, null, null, null, def => def.defName, null, null, null), "CQF_PawnEditor_Select".Translate()));
        }

        private List<ApparelLayerDef> AvailableLayers()
        {
            return DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsApparel && !def.apparel.layers.NullOrEmpty())
                .Select(def => def.apparel.LastLayer)
                .Distinct()
                .OrderBy(layer => layer.drawOrder)
                .ThenBy(layer => layer.defName)
                .ToList();
        }

        private bool ApparelInLayer(ThingDef def, ApparelLayerDef layer)
        {
            return def != null && def.IsApparel && def.apparel?.LastLayer == layer;
        }

        private ThingData ApparelForLayer(List<ThingData> apparels, ApparelLayerDef layer)
        {
            return apparels?.FirstOrDefault(data => this.ApparelInLayer(data?.def, layer));
        }

        private void SetLayerApparel(List<ThingData> apparels, ApparelLayerDef layer, ThingData data)
        {
            apparels.RemoveAll(item => item == data || this.ApparelInLayer(item?.def, layer));
            if (data?.def != null)
            {
                apparels.Add(data);
            }
        }

        private void ClearLayer(List<ThingData> apparels, ApparelLayerDef layer)
        {
            apparels.RemoveAll(data => this.ApparelInLayer(data?.def, layer));
        }

        private void RemoveDuplicateLayers(List<ThingData> apparels)
        {
            HashSet<ApparelLayerDef> layers = new HashSet<ApparelLayerDef>();
            for (int i = apparels.Count - 1; i >= 0; i--)
            {
                ApparelLayerDef layer = apparels[i]?.def?.apparel?.LastLayer;
                if (layer == null || !layers.Add(layer))
                {
                    apparels.RemoveAt(i);
                }
            }
        }

        private string LayerLabel(ApparelLayerDef layer)
        {
            return layer.label.NullOrEmpty() ? layer.defName : layer.label;
        }

        private void OpenStuffDialog(ThingData data, ThingDef def, Action onSelected)
        {
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(new LabeledTextureSelectDrawer<ThingDef>(GenStuff.AllowedStuffsFor(def).ToList(), stuff => stuff.uiIcon, stuff => stuff.label, stuff =>
            {
                this.SetThingData(data, def, stuff);
                onSelected?.Invoke();
            }, stuff => stuff.uiIconColor, null, null, null, stuff => stuff.defName, null, null, null), "CQF_PawnEditor_SelectStuff".Translate()));
        }

        private void SetThingData(ThingData data, ThingDef def, ThingDef stuff)
        {
            data.def = def;
            data.hitPoint = def.BaseMaxHitPoints;
            data.stuff = def.MadeFromStuff ? stuff : null;
        }

        private string ThingLabel(ThingData data)
        {
            if (data?.def == null)
            {
                return "CQF_PawnEditor_None".Translate();
            }
            if (data.def.MadeFromStuff && data.stuff != null)
            {
                return data.def.label + " - " + data.stuff.label;
            }
            return data.def.label;
        }

        private ThingDef StuffFor(ThingDef def, ThingDef stuff)
        {
            return def.MadeFromStuff ? stuff ?? GenStuff.DefaultStuffFor(def) : null;
        }
    }
}
