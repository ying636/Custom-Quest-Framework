using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_MainMapDefEditor : Page
    {
        public QuestEditor_MainMapDefEditor()
        {
            this.preventCameraMotion = false;
            this.absorbInputAroundWindow = false;
            this.doCloseX = true;
        }

        public override string PageTitle => "MainMapDefEditor".Translate().Colorize(ColorLibrary.SkyBlue);

        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            CQFEditorTools.DrawLabelAndText_Line(45f, "MainMapDefName".Translate(), ref this.CurDef.defName, 5f, 300f);
            TooltipHandler.TipRegion(new Rect(5f, 45f, 455f, 25f), "MainMapDefNameTip".Translate());
            this.DrawButton();
            float y = 55f;
            Widgets.BeginScrollView(new Rect(5f, 75f, inRect.width - 10f, inRect.height - 83f), ref this.scrollPos, new Rect(0f, 75f, inRect.width - 50f, this.height));
            y += 35f;
            this.DrawMainMapAndConditions(ref y, inRect);
            Widgets.EndScrollView();
            this.height = y;
        }

        private MainMapDef CurDef => QuestEditor_MainMapDefEditor.curDef;

        private void DrawMainMapTip()
        {
            Rect tip = new Rect(875f, 32.5f, 25f, 25f);
            Widgets.ButtonImage(tip, CQFEditorTools.TipIcon);
            TooltipHandler.TipRegion(tip, "MainMapSystemTip".Translate());
        }

        private void DrawButton()
        {
            if (Widgets.ButtonText(new Rect(780f, 30f, 90f, 30f), "LoadPremade".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<MainMapDef>.AllDefsListForReading, def => QuestEditor_MainMapDefEditor.curDef = def, def => def.defName);
            }
            this.DrawMainMapTip();
            if (Widgets.ButtonText(new Rect(670f, 30f, 90f, 30f), "Save".Translate()))
            {
                try
                {
                    string dir = Path.Combine(Page_QuestEditor.Path, "Map");
                    Directory.CreateDirectory(dir);
                    string path = dir + @"\" + this.CurDef.defName + ".xml";
                    XElement defs = new XElement("Defs");
                    defs.Add(this.CurDef.SaveToXElement("QuestEditor_Library.MainMapDef"));
                    defs.Save(path);
                    CQFQuestDefBootstrap.HotLoadMainMapDef(this.CurDef);
                    Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                }
                catch (Exception e)
                {
                    Log.Error("Save error:" + e.Message);
                }
            }
            if (Widgets.ButtonText(new Rect(560f, 30f, 90f, 30f), "ResetBinding".Translate()))
            {
                Dialog_MessageBox dialog = new Dialog_MessageBox("ConfirmCreateNewMainMapDef".Translate());
                dialog.buttonBText = "Cancel".Translate();
                dialog.buttonBAction = () => dialog.Close();
                dialog.buttonAText = "Confirm".Translate();
                dialog.buttonAAction = () =>
                {
                    QuestEditor_MainMapDefEditor.curDef = new MainMapDef();
                    dialog.Close();
                };
                Find.WindowStack.Add(dialog);
            }
        }

        private string GetMainMapAndConditionLabel(MainMapAndCondition item, int index)
        {
            if (!item.name.NullOrEmpty())
            {
                return item.name;
            }
            return "MainMapAndConditionLabel".Translate(index + 1);
        }

        private void DrawMainMapAndConditions(ref float y, Rect inRect)
        {
            Rect titleRect = new Rect(5f, y, 255f, 25f);
            Widgets.Label(titleRect, "MainMapAndConditions".Translate().Colorize(ColorLibrary.PaleBlue));
            TooltipHandler.TipRegion(titleRect, "MainMapAndConditionsTip".Translate());
            y += 30f;
            int dragTargetIndex = -1;
            List<MainMapAndCondition> drawingMaps = this.GetDrawingMaps();
            for (int i = 0; i < drawingMaps.Count; i++)
            {
                MainMapAndCondition item = drawingMaps[i];
                int originalIndex = this.CurDef.maps.IndexOf(item);
                Rect dragRect = new Rect(5f, y + 3f, 18f, 18f);
                Rect row = new Rect(30f, y, 615f, 25f);
                Rect hitRect = new Rect(5f, y, 640f, 25f);
                this.HandleDragStart(originalIndex, dragRect);
                if (this.draggingIndex >= 0 && Mouse.IsOver(hitRect))
                {
                    dragTargetIndex = i;
                    this.dragTargetIndex = i;
                    Widgets.DrawHighlight(hitRect);
                }
                if (this.draggingIndex >= 0 && originalIndex == this.draggingIndex)
                {
                    Widgets.DrawHighlight(row);
                }
                Color dragColor = Mouse.IsOver(dragRect) ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.35f, 0.35f, 0.35f);
                Widgets.DrawBoxSolid(dragRect, dragColor);
                TooltipHandler.TipRegion(dragRect, "MainMapDragTip".Translate());
                string label = this.GetMainMapAndConditionLabel(item, originalIndex);
                if (this.draggingIndex >= 0)
                {
                    Widgets.DrawHighlightIfMouseover(row);
                    Widgets.Label(row, label);
                }
                else if (Widgets.ButtonText(row, label, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(item));
                }
                TooltipHandler.TipRegion(row, "MainMapCandidateTip".Translate());
                y += 30f;
            }
            this.HandleDragEnd(dragTargetIndex);
            y += 5f;
            Rect addRect = new Rect(5f, y, 120f, 25f);
            if (Widgets.ButtonText(addRect, "Add".Translate()))
            {
                this.CurDef.maps.Add(new MainMapAndCondition()
                {
                    name = "MainMapDefaultMapName".Translate(),
                    set = new CustomMapGenerationSet()
                });
            }
            TooltipHandler.TipRegion(addRect, "MainMapAddCandidateTip".Translate());
            Rect deleteRect = new Rect(135f, y, 120f, 25f);
            if (Widgets.ButtonText(deleteRect, "Delete".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(this.CurDef.maps, item => this.CurDef.maps.Remove(item), item => this.GetMainMapAndConditionLabel(item, this.CurDef.maps.IndexOf(item)));
            }
            TooltipHandler.TipRegion(deleteRect, "MainMapDeleteCandidateTip".Translate());
            y += 30f;
        }

        private List<MainMapAndCondition> GetDrawingMaps()
        {
            List<MainMapAndCondition> drawingMaps = new List<MainMapAndCondition>(this.CurDef.maps);
            if (this.draggingIndex < 0 || this.dragTargetIndex < 0 || this.draggingIndex == this.dragTargetIndex)
            {
                return drawingMaps;
            }
            MainMapAndCondition item = this.CurDef.maps[this.draggingIndex];
            drawingMaps.Remove(item);
            drawingMaps.Insert(this.dragTargetIndex, item);
            return drawingMaps;
        }

        private void HandleDragStart(int index, Rect dragRect)
        {
            UnityEngine.Event ev = UnityEngine.Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && Mouse.IsOver(dragRect))
            {
                this.draggingIndex = index;
                ev.Use();
            }
        }

        private void HandleDragEnd(int targetIndex)
        {
            UnityEngine.Event ev = UnityEngine.Event.current;
            if (this.draggingIndex < 0 || ev.type != EventType.MouseUp)
            {
                return;
            }
            if (targetIndex >= 0 && this.draggingIndex != targetIndex)
            {
                MainMapAndCondition item = this.CurDef.maps[this.draggingIndex];
                this.CurDef.maps.RemoveAt(this.draggingIndex);
                this.CurDef.maps.Insert(targetIndex, item);
            }
            this.draggingIndex = -1;
            this.dragTargetIndex = -1;
            ev.Use();
        }

        public float height = 0f;
        public Vector2 scrollPos = Vector2.zero;
        private int draggingIndex = -1;
        private int dragTargetIndex = -1;
        private static MainMapDef curDef = new MainMapDef();
    }
}
