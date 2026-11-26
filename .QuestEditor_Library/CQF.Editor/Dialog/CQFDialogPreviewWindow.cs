using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFDialogPreviewWindow : Window
    {
        public CQFDialogPreviewWindow(DialogTreeDef tree, int index)
        {
            this.tree = tree;
            this.index = tree.nodeMoulds.ContainsKey(index) ? index : 0;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.doCloseX = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
        }

        public override Vector2 InitialSize => new Vector2(
            Mathf.Min(this.tree.ActivePresentation == null ? 700f : 1280f, UI.screenWidth - 40f),
            Mathf.Min(this.tree.ActivePresentation == null ? 700f : 900f, UI.screenHeight - 40f));

        public override void PostClose()
        {
            this.presentationPreview.Dispose();
            base.PostClose();
        }

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            Text.Font = GameFont.Medium;
            Rect title = new Rect(0f, 0f, inRect.width - 30f, 35f);
            Widgets.Label(title, "CQF_DialogGraph_Preview".Translate());
            TooltipHandler.TipRegion(title, "CQF_DialogGraph_PreviewHint".Translate());
            Text.Font = GameFont.Small;
            float buttonWidth = Mathf.Min(130f, (inRect.width - 10f) / 2f);
            if (CQFUIStyle.ButtonText(new Rect(0f, 43f, buttonWidth, 30f), "CQF_DialogGraph_Entry".Translate()))
            {
                this.index = 0;
                this.history.Clear();
                this.scroll = Vector2.zero;
            }
            if (CQFUIStyle.ButtonText(new Rect(buttonWidth + 10f, 43f, buttonWidth, 30f), "Back".Translate(), active: this.history.Count > 0))
            {
                this.index = this.history.Pop();
                this.scroll = Vector2.zero;
            }
            if (!this.index.HasValue)
            {
                Widgets.Label(new Rect(0f, 90f, inRect.width, 40f), "CQF_DialogGraph_End".Translate());
                return;
            }
            if (!this.tree.nodeMoulds.TryGetValue(this.index.Value, out DialogNode node))
            {
                Widgets.Label(new Rect(0f, 90f, inRect.width, 80f), "CQF_DialogAI_MissingNode".Translate() + ": " + this.index);
                return;
            }
            Rect view = new Rect(0f, 84f, inRect.width, inRect.height - 84f);
            if (this.presentationPreview.Draw(view, this.tree, node, next =>
            {
                this.history.Push(this.index.Value);
                this.index = next;
                this.scroll = Vector2.zero;
            })) return;
            string text = node.text.CanTranslate() ? node.text.Translate().ToString() : node.text;
            Text.WordWrap = true;
            float contentWidth = view.width - 24f;
            float textHeight = Mathf.Max(30f, Text.CalcHeight(text, contentWidth));
            float imageHeight = node.images.Count(image => !string.IsNullOrEmpty(image.imagePath)) * 130f;
            var options = node.options.Select(option =>
            {
                string label = option.text.CanTranslate() ? option.text.Translate().ToString() : option.text ?? string.Empty;
                return (Option: option, Label: label, Height: Mathf.Max(26f, Text.CalcHeight(label, contentWidth)));
            }).ToArray();
            float height = textHeight + imageHeight + options.Sum(option => option.Height + 10f + option.Option.results.Count * 40f) + 21f;
            Widgets.BeginScrollView(view, ref this.scroll, new Rect(0f, 0f, view.width - 18f, Mathf.Max(view.height, height)));
            float y = 0f;
            foreach (DialogImage image in node.images)
            {
                if (string.IsNullOrEmpty(image.imagePath)) continue;
                Texture2D texture = ContentFinder<Texture2D>.Get(image.imagePath, false);
                if (texture != null) Widgets.DrawTextureFitted(new Rect(0f, y, contentWidth, 120f), texture, Mathf.Clamp(image.scale, 0.1f, 3f));
                else Widgets.Label(new Rect(0f, y, contentWidth, 120f), "CQF_DialogGraph_MissingImage".Translate(image.imagePath));
                y += 130f;
            }
            Widgets.Label(new Rect(0f, y, contentWidth, textHeight), text);
            y += textHeight + 15f;
            foreach (var option in options)
            {
                Widgets.Label(new Rect(0f, y, contentWidth, option.Height), option.Label);
                y += option.Height + 10f;
                for (int i = 0; i < option.Option.results.Count; i++)
                {
                    DialogResult result = option.Option.results[i];
                    if (CQFUIStyle.ButtonText(new Rect(12f, y, contentWidth - 12f, 34f),
                        "CQF_DialogGraph_PreviewResult".Translate(i + 1, result.resultName, result.conditions.Count, result.actions.Count)))
                    {
                        this.history.Push(node.index.GetValueOrDefault());
                        this.index = result.nextIndex;
                        this.scroll = Vector2.zero;
                    }
                    y += 40f;
                }
            }
            Widgets.EndScrollView();
        }

        private readonly DialogTreeDef tree;
        private readonly Stack<int> history = new Stack<int>();
        private int? index;
        private Vector2 scroll;
        private readonly CQFDialogPresentationPreview presentationPreview = new();
    }
}
