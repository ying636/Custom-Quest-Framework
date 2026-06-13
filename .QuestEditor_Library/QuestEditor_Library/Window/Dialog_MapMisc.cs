using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_MapMisc : Window
    {
        public Dialog_MapMisc(CustomMapDataDef def) 
        {
            this.def = def;
            this.doCloseX = true;
        }
        public override void DoWindowContents(Rect inRect)
        {
            float y = 10f;
            float x2 = 5f;
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref this.pos, new Rect(0f, 0f, inRect.width, this.height + 10f));
            CQFEditorTools.DrawSelectableText(y, "MapDataFaction".Translate(), ref def.faction, () => CQFEditorTools.DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll((f) => !f.isPlayer), (f) => def.faction = f.defName, (f) => f.label, new List<FloatMenuOption>()
            {
                new FloatMenuOption("RandomHostile".Translate(),() => def.faction = "RandomHostile"),
                new FloatMenuOption("RandomAlly".Translate(),() => def.faction = "RandomAlly"),
                new FloatMenuOption("RandomNeutral".Translate(),() => def.faction = "RandomNeutral"),
                new FloatMenuOption("PawnDataMapFaction".Translate(),() => def.faction = "MapFaction")
            }), x2, 120f);
            TooltipHandler.TipRegion(new Rect(x2, y, 340f, 25f), "MapDataFactionTip".Translate());
            y += 30f;
            CQFEditorTools.DrawEditableStringList(QuestEditor_SaveMapToFile.def.tags, ref y, "CustomMapTags".Translate(), "CustomMapTags_Tip".Translate(), true, x2, 300f);
            CQFEditorTools.DrawEditableList(QuestEditor_SaveMapToFile.def.mapPartGenerationLimit, ref y, (textField, t) =>
            {
                textField.width = 90;
                t.key = Widgets.TextField(textField, t.key);
                Rect chance = new Rect(textField.width + textField.x + 10f, textField.y, textField.width + 80f, 25f);
                Widgets.TextFieldNumericLabeled(chance, "GenerationLimit".Translate(), ref t.limit, ref t.buffer);
            }, t => t.key, "MapPartGenerationLimit".Translate(), "MapPartGenerationLimit_Tip".Translate(), true, x2, 300f);
            Rect enterDirection = new Rect(x2, y, 300f, 25f);
            if (Widgets.ButtonText(enterDirection, "CustomMapEnterDirection".Translate(this.EnterDirectionLabel(this.def.enterDirection)), false))
            {
                CQFEditorTools.DrawFloatMenu(new List<Rot4>() { Rot4.East, Rot4.West, Rot4.North, Rot4.South, Rot4.Invalid },
                    r => this.def.enterDirection = r,
                    this.EnterDirectionLabel);
            }
            TooltipHandler.TipRegion(enterDirection, "CustomMapEnterDirection_Tip".Translate());
            y += 30f;
            CQFEditorTools.DrawIDrawList_UseWindow_UseIcon(ref y,x2,this.def.customSteps,inRect,"CustomSteps".Translate(),s => s.GetType().Name.Translate());
            Rect generator = new Rect(x2, y, 300f, 25f);
            if (Widgets.ButtonText(generator, 
                "CQF_MapGenerator".Translate(
                    QuestEditor_SaveMapToFile.def.generator?.label ??
                    QuestEditor_SaveMapToFile.def.generator?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<MapGeneratorDef>.AllDefsListForReading,
                    g => QuestEditor_SaveMapToFile.def.generator = g, g => g.label ?? g.defName);
            }
            TooltipHandler.TipRegion(generator, "CQF_MapGenerator_Tip".Translate());
            Widgets.EndScrollView();
            this.height = y + 5f;
        }

        private string EnterDirectionLabel(Rot4 rot)
        {
            return rot == Rot4.Invalid ? "Rot_Invalid".Translate().ToString() : rot.ToStringHuman().Translate().ToString();
        }

        public CustomMapDataDef def;
        public string buffer;
        public float height;
        public Vector2 pos = Vector2.zero;
    }
}
