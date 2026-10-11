using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public abstract class QuestBookObjective_ThingTarget : QuestBookObjective_TargetCount
    {
        public override bool UsesThingTarget => true;
        public override ThingDef TargetThingDef { get => targetThingDef; set => targetThingDef = value; }

        public override IEnumerable<ThingDef> GetThingTargets()
        {
            yield break;
        }

        public override void DrawSpecial(ref float y, Rect inRect, float x)
        {
            this.DrawDetectionSection(ref y, inRect, (Rect card, ref float rowY) =>
            {
                this.DrawRowLabel(card, rowY, "CQF_QuestBook_TargetThing");
                string label = this.TargetThingDef == null ? "CQF_QuestBook_None".Translate().ToString() : this.TargetThingDef.LabelCap;
                Rect button = new Rect(card.x + 184f, rowY, card.width - 198f, 28f);
                if (CQFUIStyle.ButtonText(button, label, false, true))
                {
                    List<ThingDef> selectableDefs = this.GetThingTargets().Where(def => def != null && QuestBookTextureEntry.GetThingTexturePath(def) != null).OrderBy(def => def.label).ToList();
                    Find.WindowStack.Add(new Dialog_Select<ThingDef>(new LabeledTextureSelectDrawer<ThingDef>(selectableDefs, def => ContentFinder<Texture2D>.Get(QuestBookTextureEntry.GetThingTexturePath(def), false), def => def.LabelCap, def =>
                    {
                        this.TargetThingDef = def;
                        if (!this.iconManuallySelected)
                        {
                            this.iconPath = QuestBookTextureEntry.GetThingTexturePath(def);
                        }
                    }), "CQF_QuestBook_TargetThing".Translate()));
                }

                rowY += 36f;
                this.DrawTargetCountField(card, ref rowY);
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref targetThingDef, "targetThingDef");
        }

        public ThingDef targetThingDef;
    }
}
