using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Window_BlueprintPalette : Window
    {
        public Window_BlueprintPalette(Designator_Blueprint designator)
        {
            this.designator = designator;
            this.layer = WindowLayer.GameUI;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.doCloseX = false;
            this.draggable = true;
            this.preventCameraMotion = false;
            this.absorbInputAroundWindow = false;
        }

        public override Vector2 InitialSize => new Vector2(280f, 230f);

        protected override float Margin => 6f;

        public override void PreOpen()
        {
            base.PreOpen();
            this.RefreshBlueprints();
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            try
            {
                if (this.repositoryVersion != BlueprintRepository.Version)
                {
                    this.RefreshBlueprints();
                }
                this.DrawContents(inRect);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
            }
        }

        private void DrawContents(Rect inRect)
        {
            float contentWidth = inRect.width - ToolbarWidth;
            Widgets.LabelFit(new Rect(0f, 0f, contentWidth, TitleHeight), "CQF_BlueprintPalette".Translate());
            this.DrawToolbar(contentWidth);

            float y = TitleHeight + SectionGap;
            Rect previewRect = new Rect(0f, y, contentWidth, PreviewSectionHeight);
            Widgets.DrawMenuSection(previewRect);
            this.DrawSelectedPreview(previewRect.ContractedBy(3f));
            y = previewRect.yMax + SectionGap;

            GUI.DrawTexture(new Rect(0f, y + 1f, SearchIconSize, SearchIconSize), TexButton.Search);
            string newSearchTerms = Widgets.TextField(
                new Rect(SearchIconSize + SearchGap, y, contentWidth - SearchIconSize - SearchGap, SearchHeight),
                this.searchTerms);
            if (newSearchTerms != this.searchTerms)
            {
                this.searchTerms = newSearchTerms;
                this.RefreshBlueprints();
            }
            y += SearchHeight + SectionGap;

            Rect listRect = new Rect(0f, y, contentWidth, inRect.height - y);
            Widgets.DrawMenuSection(listRect);
            this.DrawBlueprintList(listRect.ContractedBy(2f));
        }

        private void DrawToolbar(float contentWidth)
        {
            float x = contentWidth + ToolbarGap;
            Rect closeRect = new Rect(x, 1f, ToolbarButtonSize, ToolbarButtonSize);
            if (Widgets.ButtonImage(closeRect, TexButton.CloseXSmall, true, "CloseButton".Translate()))
            {
                this.Close();
            }

            Rect pinRect = new Rect(x, closeRect.yMax + ToolbarGap, ToolbarButtonSize, ToolbarButtonSize);
            if (this.windowPinned)
            {
                Widgets.DrawHighlightSelected(pinRect);
            }
            Texture2D pinIcon = this.windowPinned ? TexCommand.ForbidOff : TexCommand.ForbidOn;
            string pinTip = (this.windowPinned ? "CQF_UnpinWindow" : "CQF_PinWindow").Translate();
            if (Widgets.ButtonImage(pinRect, pinIcon, true, pinTip))
            {
                this.windowPinned = !this.windowPinned;
                this.draggable = !this.windowPinned;
            }

            Rect saveRect = new Rect(x, pinRect.yMax + ToolbarGap, ToolbarButtonSize, ToolbarButtonSize);
            Texture2D saveIcon = ContentFinder<Texture2D>.Get("UI/Icon_SaveZoneAsDef_Round", false)
                ?? TexButton.NewFile;
            if (Widgets.ButtonImage(saveRect, saveIcon, true, "CQF_SaveBlueprintDesc".Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption("SaveMode_Round".Translate(), () =>
                        new Designator_SaveBlueprint(this.designator, SaveMode.Round).BeginSelection()),
                    new FloatMenuOption("SaveMode_Rectangle".Translate(), () =>
                        new Designator_SaveBlueprint(this.designator, SaveMode.Rectangle).BeginSelection()),
                    new FloatMenuOption("SaveMode_RectangleUseCentre".Translate(), () =>
                        new Designator_SaveBlueprint(this.designator, SaveMode.RectangleUseCentre).BeginSelection()),
                    new FloatMenuOption("CQF_SaveWholeMap".Translate(), () =>
                        new Designator_SaveBlueprint(this.designator, SaveMode.None).SaveWholeMap())
                }));
            }

            Rect importRect = new Rect(x, saveRect.yMax + ToolbarGap, ToolbarButtonSize, ToolbarButtonSize);
            if (Widgets.ButtonImage(importRect, TexButton.Add, true, "CQF_ImportLoadedMaps".Translate()))
            {
                BlueprintRepository.ConfirmImportLoadedBlueprints();
            }
        }

        private void DrawSelectedPreview(Rect rect)
        {
            CustomMapDataDef blueprint = this.designator.SelectedBlueprint;
            Rect imageRect = new Rect(rect.x, rect.y, PreviewWidth, rect.height);
            BlueprintPreviewCache.Draw(imageRect, blueprint);
            Rect textRect = new Rect(imageRect.xMax + 5f, rect.y, rect.width - imageRect.width - 5f, rect.height);
            if (blueprint == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(textRect, "CQF_NoBlueprintSelected".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }
            string label = blueprint.label.NullOrEmpty() ? blueprint.defName : blueprint.label;
            Widgets.LabelFit(new Rect(textRect.x, textRect.y + 2f, textRect.width, 18f), label);
            Widgets.LabelFit(new Rect(textRect.x, textRect.y + 23f, textRect.width, 18f),
                "CQF_BlueprintPreviewSize".Translate(blueprint.size.x, blueprint.size.z));
            string source = (BlueprintRepository.IsImported(blueprint)
                ? "CQF_BlueprintSourceImported"
                : "CQF_BlueprintSourceMemory").Translate();
            Widgets.LabelFit(new Rect(textRect.x, textRect.y + 44f, textRect.width, 18f), source);
        }

        private void DrawBlueprintList(Rect outRect)
        {
            if (this.filteredBlueprints.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(outRect, "CQF_NoBlueprints".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            float contentHeight = Math.Max(outRect.height, this.filteredBlueprints.Count * RowHeight);
            Rect viewRect = new Rect(0f, 0f, outRect.width - ScrollbarWidth, contentHeight);
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
            int firstVisible = Math.Max(0, Mathf.FloorToInt(this.scrollPosition.y / RowHeight) - 1);
            int lastVisible = Math.Min(this.filteredBlueprints.Count,
                Mathf.CeilToInt((this.scrollPosition.y + outRect.height) / RowHeight) + 1);
            for (int index = firstVisible; index < lastVisible; index++)
            {
                Rect rowRect = new Rect(0f, index * RowHeight, viewRect.width, RowHeight - 1f);
                this.DrawBlueprintRow(this.filteredBlueprints[index], rowRect);
            }
            Widgets.EndScrollView();
        }

        private void DrawBlueprintRow(CustomMapDataDef blueprint, Rect rowRect)
        {
            if (blueprint == this.designator.SelectedBlueprint)
            {
                Widgets.DrawHighlightSelected(rowRect);
            }
            Widgets.DrawHighlightIfMouseover(rowRect);

            Rect iconRect = new Rect(rowRect.x + 2f, rowRect.y + 2f, ThumbnailWidth, rowRect.height - 4f);
            BlueprintPreviewCache.Draw(iconRect, blueprint);
            string label = blueprint.label.NullOrEmpty() ? blueprint.defName : blueprint.label;
            Widgets.LabelFit(new Rect(iconRect.xMax + 4f, rowRect.y + 2f,
                rowRect.width - iconRect.width - SizeLabelWidth - 10f, rowRect.height - 4f), label);

            string size = blueprint.size.IsValid ? blueprint.size.x + "x" + blueprint.size.z : "-";
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(rowRect.xMax - SizeLabelWidth - 3f, rowRect.y, SizeLabelWidth, rowRect.height), size);
            Text.Anchor = TextAnchor.UpperLeft;

            UnityEngine.Event currentEvent = UnityEngine.Event.current;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1
                && rowRect.Contains(currentEvent.mousePosition))
            {
                this.OpenBlueprintMenu(blueprint);
                currentEvent.Use();
                return;
            }
            if (Widgets.ButtonInvisible(rowRect))
            {
                this.designator.SelectBlueprint(blueprint);
            }
            if (Mouse.IsOver(rowRect))
            {
                string source = (BlueprintRepository.IsImported(blueprint)
                    ? "CQF_BlueprintSourceImported"
                    : "CQF_BlueprintSourceMemory").Translate();
                TooltipHandler.TipRegion(rowRect, "CQF_BlueprintTooltip".Translate(
                    label, blueprint.defName, size, source, blueprint.description ?? string.Empty));
            }
        }

        private void OpenBlueprintMenu(CustomMapDataDef blueprint)
        {
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_DeleteBlueprint".Translate(), () => this.DeleteBlueprint(blueprint)),
                new FloatMenuOption("CQF_SaveBlueprintXml".Translate(), () => BlueprintRepository.ExportToXml(blueprint))
            }));
        }

        private void DeleteBlueprint(CustomMapDataDef blueprint)
        {
            bool wasSelected = blueprint == this.designator.SelectedBlueprint;
            BlueprintRepository.Delete(blueprint);
            this.RefreshBlueprints();
            if (!wasSelected)
            {
                return;
            }
            CustomMapDataDef replacement = BlueprintRepository.AllBlueprints.FirstOrDefault();
            if (replacement == null)
            {
                this.designator.ClearBlueprint();
                return;
            }
            this.designator.SelectBlueprint(replacement);
        }

        private void RefreshBlueprints()
        {
            List<CustomMapDataDef> availableBlueprints = BlueprintRepository.AllBlueprints.ToList();
            if (this.designator.SelectedBlueprint != null
                && !availableBlueprints.Contains(this.designator.SelectedBlueprint))
            {
                this.designator.ClearBlueprint();
            }
            IEnumerable<CustomMapDataDef> blueprints = availableBlueprints;
            if (!this.searchTerms.NullOrEmpty())
            {
                blueprints = blueprints.Where(blueprint =>
                    (blueprint.label ?? string.Empty).IndexOf(this.searchTerms, StringComparison.OrdinalIgnoreCase) >= 0
                    || (blueprint.defName ?? string.Empty).IndexOf(this.searchTerms, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            this.filteredBlueprints = blueprints
                .OrderByDescending(BlueprintRepository.IsTemporary)
                .ThenBy(blueprint => blueprint.label ?? blueprint.defName)
                .ToList();
            this.repositoryVersion = BlueprintRepository.Version;
            this.scrollPosition = Vector2.zero;
        }

        private const float PreviewSectionHeight = 70f;
        private const float PreviewWidth = 96f;
        private const float RowHeight = 29f;
        private const float ScrollbarWidth = 16f;
        private const float SearchGap = 3f;
        private const float SearchHeight = 20f;
        private const float SearchIconSize = 18f;
        private const float SectionGap = 3f;
        private const float SizeLabelWidth = 42f;
        private const float TitleHeight = 20f;
        private const float ThumbnailWidth = 38f;
        private const float ToolbarButtonSize = 18f;
        private const float ToolbarGap = 2f;
        private const float ToolbarWidth = 20f;

        private readonly Designator_Blueprint designator;
        private List<CustomMapDataDef> filteredBlueprints = new List<CustomMapDataDef>();
        private int repositoryVersion = -1;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchTerms = string.Empty;
        private bool windowPinned;
    }
}
