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
            object[] arguments = new object[]
            {
                pawnDef,
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Apparel.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
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
        internal void OpenLayerSelectDialog(List<ThingData> apparels, ApparelLayerDef layer)
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
        internal List<ApparelLayerDef> AvailableLayers()
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
        internal ThingData ApparelForLayer(List<ThingData> apparels, ApparelLayerDef layer)
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
        internal void ClearLayer(List<ThingData> apparels, ApparelLayerDef layer)
        {
            apparels.RemoveAll(data => this.ApparelInLayer(data?.def, layer));
        }
        internal void RemoveDuplicateLayers(List<ThingData> apparels)
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
        internal string LayerLabel(ApparelLayerDef layer)
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
        internal string ThingLabel(ThingData data)
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
        internal ThingDef StuffFor(ThingDef def, ThingDef stuff)
        {
            return def.MadeFromStuff ? stuff ?? GenStuff.DefaultStuffFor(def) : null;
        }
    }
}
