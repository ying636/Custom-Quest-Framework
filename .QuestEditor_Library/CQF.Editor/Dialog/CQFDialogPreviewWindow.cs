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

        public override Vector2 InitialSize => new Vector2(700f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 35f), "CQF_DialogGraph_Preview".Translate());
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 40f, inRect.width, 65f), "CQF_DialogGraph_PreviewHint".Translate());
            if (Widgets.ButtonText(new Rect(0f, 110f, 130f, 28f), "CQF_DialogGraph_Entry".Translate()))
            {
                this.index = 0;
                this.history.Clear();
            }
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && this.history.Count > 0;
            if (Widgets.ButtonText(new Rect(140f, 110f, 130f, 28f), "Back".Translate())) this.index = this.history.Pop();
            GUI.enabled = enabled;
            if (!this.index.HasValue)
            {
                Widgets.Label(new Rect(0f, 160f, inRect.width, 40f), "CQF_DialogGraph_End".Translate());
                return;
            }
            if (!this.tree.nodeMoulds.TryGetValue(this.index.Value, out DialogNode node))
            {
                Widgets.Label(new Rect(0f, 160f, inRect.width, 80f), "CQF_DialogAI_MissingNode".Translate() + ": " + this.index);
                return;
            }
            Rect view = new Rect(0f, 150f, inRect.width, inRect.height - 150f);
            string text = node.text.CanTranslate() ? node.text.Translate().ToString() : node.text;
            float textHeight = Mathf.Max(90f, Text.CalcHeight(text, view.width - 24f));
            float imageHeight = node.images.Count(image => !string.IsNullOrEmpty(image.imagePath)) * 130f;
            float height = textHeight + imageHeight + node.options.Sum(option => 48f + option.results.Count * 40f) + 45f;
            Widgets.BeginScrollView(view, ref this.scroll, new Rect(0f, 0f, view.width - 18f, Mathf.Max(view.height, height)));
            float y = 0f;
            foreach (DialogImage image in node.images)
            {
                if (string.IsNullOrEmpty(image.imagePath)) continue;
                Texture2D texture = ContentFinder<Texture2D>.Get(image.imagePath, false);
                if (texture != null) Widgets.DrawTextureFitted(new Rect(0f, y, view.width - 24f, 120f), texture, Mathf.Clamp(image.scale, 0.1f, 3f));
                else Widgets.Label(new Rect(0f, y, view.width - 24f, 120f), "CQF_DialogGraph_MissingImage".Translate(image.imagePath));
                y += 130f;
            }
            Widgets.Label(new Rect(0f, y, view.width - 24f, textHeight), text);
            y += textHeight + 15f;
            foreach (DialogOption option in node.options)
            {
                Widgets.Label(new Rect(0f, y, view.width - 24f, 36f), option.text.CanTranslate() ? option.text.Translate().ToString() : option.text);
                y += 42f;
                for (int i = 0; i < option.results.Count; i++)
                {
                    DialogResult result = option.results[i];
                    if (Widgets.ButtonText(new Rect(12f, y, view.width - 36f, 34f),
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
    }
}
