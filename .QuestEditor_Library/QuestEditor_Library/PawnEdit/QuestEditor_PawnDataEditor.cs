using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_PawnDataEditor : Page
    {
        public QuestEditor_PawnDataEditor()
        {
            this.preventCameraMotion = false;
            this.absorbInputAroundWindow = false;
            this.doCloseX = true;
        }

        public override string PageTitle => "CQF_PawnEditor_Title".Translate().Colorize(ColorLibrary.SkyBlue);

        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            this.DrawButtons(inRect);
            Rect mainRect = new Rect(5f, 76f, inRect.width - 10f, inRect.height - 84f);
            float leftWidth = 270f;
            float rightWidth = 230f;
            Rect previewRect = new Rect(mainRect.x, mainRect.y, leftWidth, mainRect.height);
            Rect moduleRect = new Rect(mainRect.xMax - rightWidth, mainRect.y, rightWidth, mainRect.height);
            Rect editorRect = new Rect(previewRect.xMax + 10f, mainRect.y, mainRect.width - leftWidth - rightWidth - 20f, mainRect.height);
            this.DrawPreviewPanel(previewRect);
            this.DrawModulePanel(moduleRect);
            this.DrawCurrentModule(editorRect);
        }

        private ComplexPawnDef CurDef => QuestEditor_PawnDataEditor.curDef;

        private void DrawButtons(Rect inRect)
        {
            float y = 30f;
            if (Widgets.ButtonText(new Rect(inRect.width - 320f, y, 90f, 30f), "CQF_PawnEditor_LoadPremade".Translate()))
            {
                List<ComplexPawnDef> defs = new List<ComplexPawnDef>();
                defs.AddRange(DefDatabase<ComplexPawnDef>.AllDefsListForReading);
                defs.AddRange(CQFEditorTools.GetObject<ComplexPawnDef>(QuestEditor_PawnDataEditor.SaveDir, "//QuestEditor_Library.ComplexPawnDef"));
                CQFEditorTools.DrawFloatMenu(defs, def =>
                {
                    QuestEditor_PawnDataEditor.curDef = def;
                    this.selectedModDefName = null;
                    QuestEditor_PawnDataEditor.ResetPreview();
                }, this.PawnDisplayName);
            }
            if (Widgets.ButtonText(new Rect(inRect.width - 220f, y, 90f, 30f), "CQF_PawnEditor_Save".Translate()))
            {
                this.Save();
            }
            if (Widgets.ButtonText(new Rect(inRect.width - 120f, y, 90f, 30f), "CQF_PawnEditor_New".Translate()))
            {
                Dialog_MessageBox dialog = new Dialog_MessageBox("CQF_PawnEditor_ConfirmCreateNew".Translate());
                dialog.buttonBText = "CQF_PawnEditor_Cancel".Translate();
                dialog.buttonBAction = () => dialog.Close();
                dialog.buttonAText = "CQF_PawnEditor_Confirm".Translate();
                dialog.buttonAAction = () =>
                {
                    QuestEditor_PawnDataEditor.curDef = new ComplexPawnDef();
                    QuestEditor_PawnDataEditor.ResetPreview();
                    dialog.Close();
                };
                Find.WindowStack.Add(dialog);
            }
        }

        private void DrawPreviewPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            float y = rect.y + 12f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 12f, y, rect.width - 24f, 30f), "CQF_PawnEditor_Preview".Translate().Colorize(ColorLibrary.PaleBlue));
            Text.Font = GameFont.Small;
            y += 40f;
            Rect portraitRect = new Rect(rect.x + 35f, y, rect.width - 70f, 260f);
            Widgets.DrawLightHighlight(portraitRect);
            this.DrawPawnPreview(portraitRect.ContractedBy(10f));
            y = portraitRect.yMax + 18f;
            PawnModData_Basic basic = this.CurDef.DataFor<PawnModData_Basic>();
            PawnModData_NameAndBody nameAndBody = this.CurDef.DataFor<PawnModData_NameAndBody>();
            this.DrawSummaryLine(rect, ref y, "CQF_PawnEditor_DefName".Translate(), this.CurDef.defName);
            this.DrawSummaryLine(rect, ref y, "CQF_PawnEditor_Label".Translate(), this.CurDef.label);
            this.DrawSummaryLine(rect, ref y, "CQF_PawnEditor_PawnKind".Translate(""), basic.kindDef?.label);
            this.DrawSummaryLine(rect, ref y, "CQF_PawnEditor_Faction".Translate(), basic.faction?.label);
            this.DrawSummaryLine(rect, ref y, "CQF_PawnEditor_Gender".Translate(""), nameAndBody.gender.ToString().Translate());
            this.DrawSummaryLine(rect, ref y, "CQF_PawnEditor_BioAge".Translate(), nameAndBody.bioAge.ToString());
            this.DrawSummaryLine(rect, ref y, "CQF_PawnEditor_ChronologicalAge".Translate(), nameAndBody.chrAge.ToString());
        }

        private void DrawSummaryLine(Rect rect, ref float y, string label, string value)
        {
            Rect labelRect = new Rect(rect.x + 14f, y, 105f, 24f);
            Rect valueRect = new Rect(labelRect.xMax + 6f, y, rect.width - 139f, 24f);
            Widgets.Label(labelRect, label.Colorize(ColorLibrary.PaleBlue));
            Widgets.Label(valueRect, value.NullOrEmpty() ? "CQF_PawnEditor_None".Translate().ToString() : value);
            y += 26f;
        }

        private void DrawModulePanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            float y = rect.y + 12f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 12f, y, rect.width - 24f, 30f), "CQF_PawnEditor_Modules".Translate().Colorize(ColorLibrary.PaleBlue));
            Text.Font = GameFont.Small;
            y += 38f;
            List<PawnModDef> mods = this.CurDef.AvailableMods();
            this.EnsureSelectedMod(mods);
            foreach (PawnModDef mod in mods)
            {
                Rect row = new Rect(rect.x + 10f, y, rect.width - 20f, 34f);
                bool selected = mod.defName == this.selectedModDefName;
                if (selected)
                {
                    Widgets.DrawHighlightSelected(row);
                }
                else
                {
                    Widgets.DrawHighlightIfMouseover(row);
                }
                if (Widgets.ButtonText(row, mod.EditorLabel, false, true, true, TextAnchor.MiddleLeft))
                {
                    this.selectedModDefName = mod.defName;
                    this.scrollPos = Vector2.zero;
                }
                TooltipHandler.TipRegion(row, mod.EditorDescription);
                y += 38f;
            }
            if (mods.NullOrEmpty())
            {
                Widgets.Label(new Rect(rect.x + 12f, y, rect.width - 24f, 60f), "CQF_PawnEditor_NoPawnModAvailable".Translate());
            }
        }

        private void DrawCurrentModule(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            List<PawnModDef> mods = this.CurDef.AvailableMods();
            this.EnsureSelectedMod(mods);
            PawnModDef mod = mods.FirstOrDefault(def => def.defName == this.selectedModDefName);
            if (mod == null)
            {
                Widgets.Label(rect.ContractedBy(14f), "CQF_PawnEditor_NoPawnModAvailable".Translate());
                return;
            }
            Rect titleRect = new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 34f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, mod.EditorLabel.Colorize(ColorLibrary.PaleBlue));
            Text.Font = GameFont.Small;
            TooltipHandler.TipRegion(titleRect, mod.EditorDescription);
            Rect outRect = new Rect(rect.x + 10f, rect.y + 52f, rect.width - 20f, rect.height - 62f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(this.height, outRect.height));
            Widgets.BeginScrollView(outRect, ref this.scrollPos, viewRect);
            float y = 8f;
            mod.Worker.Draw(this.CurDef, ref y, viewRect, 8f);
            this.height = y + 20f;
            Widgets.EndScrollView();
        }

        private void DrawPawnPreview(Rect rect)
        {
            this.EnsurePreviewPawn();
            if (QuestEditor_PawnDataEditor.previewPawn == null)
            {
                if (this.CurDef.KindDef?.race != null)
                {
                    Widgets.DefIcon(new Rect(rect.center.x - 48f, rect.center.y - 48f, 96f, 96f), this.CurDef.KindDef.race);
                }
                else
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(rect, "CQF_PawnEditor_PawnKind".Translate("CQF_PawnEditor_None".Translate()));
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                return;
            }
            this.SyncPreviewPawn();
            RenderTexture portrait = PortraitsCache.Get(QuestEditor_PawnDataEditor.previewPawn, new Vector2(rect.width, rect.height), Rot4.South, default(Vector3), 1.15f, true, true, true, true);
            GUI.DrawTexture(rect, portrait, ScaleMode.ScaleToFit);
        }

        private void EnsurePreviewPawn()
        {
            string key = this.GetPreviewKey();
            if (key == QuestEditor_PawnDataEditor.previewKey)
            {
                return;
            }
            QuestEditor_PawnDataEditor.previewKey = key;
            QuestEditor_PawnDataEditor.previewPawn = null;
            if (this.CurDef.KindDef == null || Current.Game == null)
            {
                return;
            }
            try
            {
                QuestEditor_PawnDataEditor.previewPawn = this.CurDef.CreatePreviewPawn();
                QuestEditor_PawnDataEditor.previewApplyKey = null;
            }
            catch (Exception e)
            {
                Log.Error("Create ComplexPawnDef preview pawn error:" + e);
            }
        }

        private string GetPreviewKey()
        {
            return this.CurDef.KindDef?.defName ?? "";
        }

        private void SyncPreviewPawn()
        {
            if (QuestEditor_PawnDataEditor.previewPawn == null)
            {
                return;
            }
            string key = this.GetPreviewApplyKey();
            if (key == QuestEditor_PawnDataEditor.previewApplyKey)
            {
                return;
            }
            QuestEditor_PawnDataEditor.previewApplyKey = key;
            this.CurDef.ApplyModsToPawn(QuestEditor_PawnDataEditor.previewPawn, true);
            QuestEditor_PawnDataEditor.previewPawn.Drawer?.renderer?.SetAllGraphicsDirty();
            PortraitsCache.SetDirty(QuestEditor_PawnDataEditor.previewPawn);
        }

        private string GetPreviewApplyKey()
        {
            List<string> parts = this.CurDef.modDatas
                .Where(data => data != null)
                .Select(data => data.SaveToXElement("li").ToString(SaveOptions.DisableFormatting))
                .ToList();
            foreach (PawnModDef mod in this.CurDef.AvailableMods())
            {
                parts.AddRange(mod.Worker.GetPreviewApplyKeyParts(this.CurDef));
            }
            return string.Join("|", parts);
        }

        private void EnsureSelectedMod(List<PawnModDef> mods)
        {
            if (mods.NullOrEmpty())
            {
                this.selectedModDefName = null;
                return;
            }
            if (this.selectedModDefName.NullOrEmpty() || !mods.Any(mod => mod.defName == this.selectedModDefName))
            {
                this.selectedModDefName = mods[0].defName;
            }
        }

        private string PawnDisplayName(ComplexPawnDef def)
        {
            return def?.label.NullOrEmpty() == false ? def.label : def?.defName;
        }

        private static void ResetPreview()
        {
            QuestEditor_PawnDataEditor.previewPawn = null;
            QuestEditor_PawnDataEditor.previewKey = null;
            QuestEditor_PawnDataEditor.previewApplyKey = null;
        }

        private void Save()
        {
            try
            {
                if (this.CurDef.defName.NullOrEmpty())
                {
                    Messages.Message("CQF_PawnEditor_NoName".Translate(), MessageTypeDefOf.CautionInput);
                    return;
                }
                Directory.CreateDirectory(QuestEditor_PawnDataEditor.SaveDir);
                string path = Path.Combine(QuestEditor_PawnDataEditor.SaveDir, this.CurDef.defName + ".xml");
                XElement defs = new XElement("Defs");
                defs.Add(this.CurDef.SaveToXElement("QuestEditor_Library.ComplexPawnDef"));
                defs.Save(path);
                CQFQuestDefBootstrap.HotLoadComplexPawnDef(this.CurDef);
                Messages.Message("CQF_PawnEditor_SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception e)
            {
                Log.Error("Save ComplexPawnDef error:" + e);
            }
        }

        private static string SaveDir => Page_QuestEditor.Path + @"\Pawn";

        public float height;
        public Vector2 scrollPos = Vector2.zero;
        private string selectedModDefName;
        private static Pawn previewPawn;
        private static string previewKey;
        private static string previewApplyKey;
        private static ComplexPawnDef curDef = new ComplexPawnDef();
    }
}
