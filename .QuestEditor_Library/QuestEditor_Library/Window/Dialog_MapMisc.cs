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

        public override Vector2 InitialSize => new Vector2(640f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;
            float contentWidth = inRect.width - 20f;
            Rect outRect = new Rect(0f, 0f, inRect.width, inRect.height);
            Rect viewRect = new Rect(0f, 0f, contentWidth, Mathf.Max(this.height, inRect.height));
            Widgets.BeginScrollView(outRect, ref this.pos, viewRect);

            this.DrawSelectorRow(ref y, contentWidth, "MapDataFaction".Translate(), this.FactionLabel(), () =>
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<FactionDef>.AllDefs.ToList().FindAll(f => !f.isPlayer),
                    f => this.def.faction = f.defName, f => f.label, new List<FloatMenuOption>()
                    {
                        new FloatMenuOption("CQF_MapFaction_None".Translate(), () => this.def.faction = null),
                        new FloatMenuOption("RandomHostile".Translate(), () => this.def.faction = "RandomHostile"),
                        new FloatMenuOption("RandomAlly".Translate(), () => this.def.faction = "RandomAlly"),
                        new FloatMenuOption("RandomNeutral".Translate(), () => this.def.faction = "RandomNeutral"),
                        new FloatMenuOption("PawnDataMapFaction".Translate(), () => this.def.faction = "MapFaction")
                    });
            }, "MapDataFactionTip".Translate());

            this.DrawTags(ref y, contentWidth);
            this.DrawGenerationLimits(ref y, contentWidth);
            this.DrawSelectorRow(ref y, contentWidth, "CQF_MapEnterDirectionLabel".Translate(),
                this.EnterDirectionLabel(this.def.enterDirection), () =>
            {
                CQFEditorTools.DrawFloatMenu(new List<Rot4>() { Rot4.East, Rot4.West, Rot4.North, Rot4.South, Rot4.Invalid },
                    r => this.def.enterDirection = r,
                    this.EnterDirectionLabel);
            }, "CustomMapEnterDirection_Tip".Translate());

            this.DrawStepSection(ref y, contentWidth, this.def.preCustomSteps, "CustomMapPreSteps", "CustomMapPreSteps_Tip");
            this.DrawStepSection(ref y, contentWidth, this.def.customSteps, "CustomSteps", "CustomSteps_Tip");
            this.DrawSelectorRow(ref y, contentWidth, "CQF_MapGeneratorLabel".Translate(),
                this.def.generator?.label ?? this.def.generator?.defName ?? "-", () =>
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<MapGeneratorDef>.AllDefsListForReading,
                    g => this.def.generator = g, g => g.label ?? g.defName);
            }, "CQF_MapGenerator_Tip".Translate());

            Widgets.EndScrollView();
            this.height = y + 8f;
        }

        private void DrawGenerationLimits(ref float y, float width)
        {
            int rowCount = Math.Max(1, this.def.mapPartGenerationLimit.Count);
            float sectionHeight = 42f + rowCount * 32f + 8f;
            Rect sectionRect = new Rect(0f, y, width, sectionHeight);
            Widgets.DrawMenuSection(sectionRect);
            this.DrawSectionHeader(y + 6f, width, "MapPartGenerationLimit".Translate(),
                "MapPartGenerationLimit_Tip".Translate(),
                () => this.def.mapPartGenerationLimit.Add(new GenerationKeyWithLimit()),
                () => CQFEditorTools.DrawFloatMenu(this.def.mapPartGenerationLimit,
                    item => this.def.mapPartGenerationLimit.Remove(item), item => item.key));

            float rowY = y + 40f;
            if (!this.def.mapPartGenerationLimit.Any())
            {
                Widgets.Label(new Rect(12f, rowY + 4f, width - 24f, 25f), "-");
            }
            foreach (GenerationKeyWithLimit item in this.def.mapPartGenerationLimit)
            {
                item.key = Widgets.TextField(new Rect(12f, rowY, width * 0.46f, 27f), item.key);
                Widgets.TextFieldNumericLabeled(new Rect(width * 0.51f, rowY, width * 0.45f, 27f),
                    "GenerationLimit".Translate(), ref item.limit, ref item.buffer);
                rowY += 32f;
            }
            y += sectionHeight + 10f;
        }

        private void DrawSectionHeader(float y, float width, string title, string tip, Action addAction, Action removeAction)
        {
            Rect titleRect = new Rect(12f, y + 3f, width - 96f, 25f);
            Widgets.Label(titleRect, title.Colorize(ColorLibrary.PaleBlue));
            TooltipHandler.TipRegion(titleRect, tip);

            Rect addRect = new Rect(width - 72f, y, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                addAction();
            }
            TooltipHandler.TipRegion(addRect, "Add".Translate());

            Rect removeRect = new Rect(width - 36f, y, 28f, 28f);
            if (Widgets.ButtonImage(removeRect, TexButton.Delete))
            {
                removeAction();
            }
            TooltipHandler.TipRegion(removeRect, "Remove".Translate());
        }

        private void DrawSelectorRow(ref float y, float width, string label, string value, Action selectAction, string tip)
        {
            Rect rowRect = new Rect(0f, y, width, 36f);
            Widgets.DrawHighlightIfMouseover(rowRect);
            Rect labelRect = new Rect(8f, y + 6f, 170f, 25f);
            Widgets.Label(labelRect, label);
            Rect buttonRect = new Rect(180f, y + 3f, width - 188f, 29f);
            if (Widgets.ButtonText(buttonRect, value, false))
            {
                selectAction();
            }
            TooltipHandler.TipRegion(rowRect, tip);
            y += 42f;
        }

        private void DrawStepSection(ref float y, float width, List<CustomMapStep> steps, string titleKey, string tipKey)
        {
            int rowCount = Math.Max(1, steps.Count);
            float sectionHeight = 42f + rowCount * 32f + 8f;
            Rect sectionRect = new Rect(0f, y, width, sectionHeight);
            Widgets.DrawMenuSection(sectionRect);
            this.DrawSectionHeader(y + 6f, width, titleKey.Translate(), tipKey.Translate(),
                () => CQFEditorTools.DrawFloatMenu(typeof(CustomMapStep).AllSubclassesNonAbstract(),
                    type => steps.Add((CustomMapStep)Activator.CreateInstance(type)), type => type.Name.Translate()),
                () => CQFEditorTools.DrawFloatMenu(steps, step => steps.Remove(step), this.GetStepLabel));

            float rowY = y + 40f;
            if (!steps.Any())
            {
                Widgets.Label(new Rect(12f, rowY + 4f, width - 24f, 25f), "-");
            }
            foreach (CustomMapStep step in steps)
            {
                if (Widgets.ButtonText(new Rect(12f, rowY, width - 24f, 27f), this.GetStepLabel(step), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(step));
                }
                rowY += 32f;
            }
            y += sectionHeight + 10f;
        }

        private void DrawTags(ref float y, float width)
        {
            int rowCount = Math.Max(1, this.def.tags.Count);
            float sectionHeight = 42f + rowCount * 32f + 8f;
            Rect sectionRect = new Rect(0f, y, width, sectionHeight);
            Widgets.DrawMenuSection(sectionRect);
            this.DrawSectionHeader(y + 6f, width, "CustomMapTags".Translate(), "CustomMapTags_Tip".Translate(),
                () => this.def.tags.Add("undefined"),
                () => CQFEditorTools.DrawFloatMenu(this.def.tags, tag => this.def.tags.Remove(tag), tag => tag));

            float rowY = y + 40f;
            if (!this.def.tags.Any())
            {
                Widgets.Label(new Rect(12f, rowY + 4f, width - 24f, 25f), "-");
            }
            for (int i = 0; i < this.def.tags.Count; i++)
            {
                this.def.tags[i] = Widgets.TextField(new Rect(12f, rowY, width - 24f, 27f), this.def.tags[i]);
                rowY += 32f;
            }
            y += sectionHeight + 10f;
        }

        private string EnterDirectionLabel(Rot4 rot)
        {
            return rot == Rot4.Invalid ? "Rot_Invalid".Translate().ToString() : rot.ToStringHuman().Translate().ToString();
        }

        private string FactionLabel()
        {
            if (this.def.faction.NullOrEmpty())
            {
                return "CQF_MapFaction_None".Translate();
            }
            if (this.def.faction == "MapFaction")
            {
                return "PawnDataMapFaction".Translate();
            }
            if (this.def.faction == "RandomHostile" || this.def.faction == "RandomAlly" || this.def.faction == "RandomNeutral")
            {
                return this.def.faction.Translate();
            }
            return DefDatabase<FactionDef>.GetNamedSilentFail(this.def.faction)?.label ?? this.def.faction;
        }

        private string GetStepLabel(CustomMapStep step)
        {
            if (step is CustomMapStep_StartQuest startQuest && startQuest.quest != null)
            {
                return "CustomMapStep_StartQuestWithQuest".Translate(startQuest.quest.label ?? startQuest.quest.defName);
            }
            return step.GetType().Name.Translate();
        }

        public CustomMapDataDef def;
        public string buffer;
        public float height;
        public Vector2 pos = Vector2.zero;
    }
}
