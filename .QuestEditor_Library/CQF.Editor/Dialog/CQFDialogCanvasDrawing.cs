using System;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed partial class CQFDialogNodeCanvas
    {
        private void DrawBody(string text, CQFDialogCardLayout layout)
        {
            this.renderer.Label(layout.Body, this.Display(text), GameFont.Small, layout.Scale);
            TooltipHandler.TipRegion(layout.Body, this.Display(text));
        }

        private void DrawNode(DialogNode node, Rect card)
        {
            CQFDialogCardLayout layout = this.CardLayout(node, card);
            this.renderer.Frame(layout, node.index == this.SelectedIndex && this.SelectedOption == null || this.HighlightedNodes.Contains(node.index.GetValueOrDefault()),
                (node.index == 0 ? "CQF_DialogGraph_Entry" : "DialogText").Translate());
            this.DrawBody(node.text, layout);
            this.DrawPort(this.NodePort(node, card, -1), Accent);
            TooltipHandler.TipRegion(this.PortRect(this.NodePort(node, card, -1)), "CQF_DialogGraph_LinkOption".Translate());
            for (int i = 0; i < node.options.Count; i++)
            {
                Vector2 point = this.NodePort(node, card, i);
                Rect row = layout.Row(i);
                Widgets.DrawBoxSolid(row, CQFEditorPalette.Panel);
                this.renderer.Label(layout.RowText(i), this.Display(node.options[i].text), GameFont.Small, layout.Scale, true);
                TooltipHandler.TipRegion(row, this.Display(node.options[i].text));
                this.DrawPort(point, LinkColor);
            }
        }

        private void DrawOption(DialogOption option, Rect card)
        {
            int owners = this.Tree.nodeMoulds.Values.Count(node => node.options.Contains(option));
            CQFDialogCardLayout layout = this.CardLayout(option, card);
            this.renderer.Frame(layout, option == this.SelectedOption, owners > 1 ? "CQF_DialogGraph_SharedCount".Translate(owners) : "OptionText".Translate());
            this.DrawBody(option.text, layout);
            for (int i = 0; i < Math.Max(1, option.results.Count); i++)
            {
                Vector2 point = this.OptionPort(option, card, i);
                DialogResult? result = i < option.results.Count ? option.results[i] : null;
                string label = result == null ? "CQF_NoDialogResults".Translate().ToString()
                    : !result.nextIndex.HasValue ? "CQF_DialogGraph_End".Translate().ToString()
                    : this.Tree.nodeMoulds.TryGetValue(result.nextIndex.Value, out DialogNode target) ? this.Display(target.text) : "CQF_DialogAI_MissingNode".Translate().ToString();
                Rect row = layout.Row(i);
                Widgets.DrawBoxSolid(row, CQFEditorPalette.Panel);
                this.renderer.Label(layout.RowText(i), label, GameFont.Small, layout.Scale, true);
                if (result != null)
                {
                    TooltipHandler.TipRegion(row, label + "\n" + "CQF_DialogGraph_ResultCounts".Translate(result.conditions.Count, result.actions.Count));
                    this.DrawPort(point, result == this.linkResult ? Accent : LinkColor);
                }
            }
        }

        private void DrawPort(Vector2 point, Color color) => this.renderer.Port(point, color, this.zoom);

        private void DrawLink(Vector2 start, Vector2 end, Color color)
        {
            float bend = Mathf.Clamp(Mathf.Abs(end.x - start.x) * 0.5f, 70f, 180f);
            Vector2 a = start + Vector2.right * bend, b = end - Vector2.right * bend, previous = start;
            for (int i = 1; i <= 24; i++)
            {
                float t = i / 24f, u = 1f - t;
                Vector2 point = u * u * u * start + 3f * u * u * t * a + 3f * u * t * t * b + t * t * t * end;
                Widgets.DrawLine(previous, point, color, 1f);
                previous = point;
            }
        }

        private void DrawControls(Rect view)
        {
            Text.Font = GameFont.Tiny;
            Rect hint = new Rect(10f, view.height - 28f, Mathf.Max(0f, view.width - 200f), 22f);
            Widgets.Label(hint, "CQF_DialogGraph_HintShort".Translate().ToString().Truncate(hint.width));
            TooltipHandler.TipRegion(hint, "CQF_DialogGraph_Hint".Translate());
            float x = view.width - 175f, y = view.height - 34f;
            Widgets.DrawBoxSolid(new Rect(x - 6f, y - 3f, 174f, 32f), CQFEditorPalette.Panel);
            if (CQFAIIconButton.DrawFramedImage(new Rect(x, y, 26f, 26f), TexButton.Minus, "CQF_DialogGraph_ZoomOut".Translate())) this.ZoomAt(this.viewSize / 2f, 1f / 1.1f);
            Widgets.Label(new Rect(x + 32f, y + 4f, 52f, 22f), Mathf.RoundToInt(this.zoom * 100f) + "%");
            if (CQFAIIconButton.DrawFramedImage(new Rect(x + 88f, y, 26f, 26f), TexButton.Plus, "CQF_DialogGraph_ZoomIn".Translate())) this.ZoomAt(this.viewSize / 2f, 1.1f);
            if (CQFAIIconButton.DrawFramedImage(new Rect(x + 130f, y, 26f, 26f), TexButton.CenterOnPointsTex, "CQF_DialogGraph_Fit".Translate())) this.Fit();
            Text.Font = GameFont.Small;
        }

    }
}
