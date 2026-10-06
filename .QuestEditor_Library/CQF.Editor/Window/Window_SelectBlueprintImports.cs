using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Window_SelectBlueprintImports : Window
    {
        public Window_SelectBlueprintImports(List<CustomMapDataDef> availableBlueprints)
        {
            this.availableBlueprints = availableBlueprints ?? new List<CustomMapDataDef>();
            this.filteredBlueprints = this.availableBlueprints;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.closeOnAccept = false;
            this.doCloseX = true;
        }

        public override Vector2 InitialSize => new Vector2(500f, 520f);

        public override void DoWindowContents(Rect inRect)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            try
            {
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
            Widgets.Label(new Rect(0f, 0f, inRect.width, TitleHeight), "CQF_SelectBlueprintImports".Translate());

            float y = TitleHeight + SectionGap;
            GUI.DrawTexture(new Rect(0f, y + 2f, SearchIconSize, SearchIconSize), TexButton.Search);
            string newSearchTerms = Widgets.TextField(
                new Rect(SearchIconSize + SearchGap, y, inRect.width - SearchIconSize - SearchGap, SearchHeight),
                this.searchTerms);
            if (newSearchTerms != this.searchTerms)
            {
                this.searchTerms = newSearchTerms;
                this.RefreshFilter();
            }

            y += SearchHeight + SectionGap;
            float buttonWidth = (inRect.width - SectionGap) / 2f;
            if (Widgets.ButtonText(new Rect(0f, y, buttonWidth, ActionButtonHeight), "CQF_SelectAll".Translate()))
            {
                foreach (CustomMapDataDef blueprint in this.filteredBlueprints)
                {
                    this.selectedBlueprints.Add(blueprint);
                }
            }
            if (Widgets.ButtonText(new Rect(buttonWidth + SectionGap, y, buttonWidth, ActionButtonHeight),
                "CQF_ClearSelection".Translate()))
            {
                this.selectedBlueprints.Clear();
            }

            y += ActionButtonHeight + SectionGap;
            float footerY = inRect.height - FooterHeight;
            Rect listRect = new Rect(0f, y, inRect.width, footerY - y - SectionGap);
            Widgets.DrawMenuSection(listRect);
            this.DrawBlueprintList(listRect.ContractedBy(2f));
            this.DrawFooter(new Rect(0f, footerY, inRect.width, FooterHeight));
        }

        private void DrawBlueprintList(Rect outRect)
        {
            if (this.filteredBlueprints.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(outRect, "CQF_NoMatchingLoadedMaps".Translate());
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
                this.DrawBlueprintRow(this.filteredBlueprints[index],
                    new Rect(0f, index * RowHeight, viewRect.width, RowHeight - 1f));
            }
            Widgets.EndScrollView();
        }

        private void DrawBlueprintRow(CustomMapDataDef blueprint, Rect rowRect)
        {
            Widgets.DrawHighlightIfMouseover(rowRect);
            bool selected = this.selectedBlueprints.Contains(blueprint);
            bool newSelected = selected;
            Widgets.Checkbox(new Vector2(rowRect.x + CheckboxPadding, rowRect.y + CheckboxPadding), ref newSelected,
                CheckboxSize);
            if (newSelected != selected)
            {
                this.SetSelected(blueprint, newSelected);
            }

            Rect previewRect = new Rect(rowRect.x + CheckboxColumnWidth, rowRect.y + 2f, PreviewWidth,
                rowRect.height - 4f);
            BlueprintPreviewCache.Draw(previewRect, blueprint);
            float textX = previewRect.xMax + 6f;
            float sizeX = rowRect.xMax - SizeWidth;
            string label = blueprint.label.NullOrEmpty() ? blueprint.defName : blueprint.label;
            Widgets.LabelFit(new Rect(textX, rowRect.y + 3f, sizeX - textX - 4f, 20f), label);
            Text.Font = GameFont.Tiny;
            Widgets.LabelFit(new Rect(textX, rowRect.y + 25f, sizeX - textX - 4f, 18f), blueprint.defName);
            Text.Font = GameFont.Small;

            string size = blueprint.size.IsValid ? blueprint.size.x + "x" + blueprint.size.z : "-";
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(sizeX, rowRect.y, SizeWidth - 3f, rowRect.height), size);
            Text.Anchor = TextAnchor.UpperLeft;

            Rect toggleRect = new Rect(rowRect.x + CheckboxColumnWidth, rowRect.y,
                rowRect.width - CheckboxColumnWidth, rowRect.height);
            if (Widgets.ButtonInvisible(toggleRect))
            {
                this.SetSelected(blueprint, !this.selectedBlueprints.Contains(blueprint));
            }
            TooltipHandler.TipRegion(rowRect, "CQF_BlueprintImportTooltip".Translate(label, blueprint.defName, size));
        }

        private void DrawFooter(Rect rect)
        {
            string selectionText = "CQF_SelectedBlueprintCount".Translate(
                this.selectedBlueprints.Count, this.availableBlueprints.Count);
            Widgets.Label(new Rect(rect.x, rect.y + 4f, rect.width - FooterButtonsWidth - SectionGap,
                FooterButtonHeight), selectionText);

            float cancelX = rect.xMax - FooterButtonsWidth;
            if (Widgets.ButtonText(new Rect(cancelX, rect.y, FooterButtonWidth, FooterButtonHeight),
                "CancelButton".Translate()))
            {
                this.Close();
            }

            Rect importRect = new Rect(cancelX + FooterButtonWidth + SectionGap, rect.y,
                FooterButtonWidth, FooterButtonHeight);
            if (!this.selectedBlueprints.Any())
            {
                GUI.color = Color.gray;
            }
            bool importClicked = Widgets.ButtonText(importRect, "CQF_ImportSelected".Translate());
            GUI.color = Color.white;
            if (!importClicked)
            {
                return;
            }
            if (!this.selectedBlueprints.Any())
            {
                Messages.Message("CQF_NoBlueprintsSelectedForImport".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            BlueprintRepository.ImportLoadedBlueprints(this.selectedBlueprints);
            this.Close();
        }

        private void RefreshFilter()
        {
            IEnumerable<CustomMapDataDef> blueprints = this.availableBlueprints;
            if (!this.searchTerms.NullOrEmpty())
            {
                blueprints = blueprints.Where(blueprint =>
                    (blueprint.label ?? string.Empty).IndexOf(this.searchTerms,
                        StringComparison.OrdinalIgnoreCase) >= 0
                    || (blueprint.defName ?? string.Empty).IndexOf(this.searchTerms,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }
            this.filteredBlueprints = blueprints.ToList();
            this.scrollPosition = Vector2.zero;
        }

        private void SetSelected(CustomMapDataDef blueprint, bool selected)
        {
            if (selected)
            {
                this.selectedBlueprints.Add(blueprint);
                return;
            }
            this.selectedBlueprints.Remove(blueprint);
        }

        private const float ActionButtonHeight = 28f;
        private const float CheckboxColumnWidth = 30f;
        private const float CheckboxPadding = 5f;
        private const float CheckboxSize = 24f;
        private const float FooterButtonHeight = 30f;
        private const float FooterButtonWidth = 105f;
        private const float FooterButtonsWidth = FooterButtonWidth * 2f + SectionGap;
        private const float FooterHeight = FooterButtonHeight;
        private const float PreviewWidth = 72f;
        private const float RowHeight = 50f;
        private const float ScrollbarWidth = 16f;
        private const float SearchGap = 4f;
        private const float SearchHeight = 25f;
        private const float SearchIconSize = 21f;
        private const float SectionGap = 6f;
        private const float SizeWidth = 58f;
        private const float TitleHeight = 30f;

        private readonly List<CustomMapDataDef> availableBlueprints;
        private readonly HashSet<CustomMapDataDef> selectedBlueprints = new HashSet<CustomMapDataDef>();
        private List<CustomMapDataDef> filteredBlueprints;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchTerms = string.Empty;
    }
}
