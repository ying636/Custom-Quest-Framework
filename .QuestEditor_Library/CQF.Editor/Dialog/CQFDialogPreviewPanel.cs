using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFDialogPreviewPanel
    {
        public CQFDialogPreviewPanel(QuestEditor_Dialog editor) { this.editor = editor; }

        public int? CurrentIndex { get; private set; }

        public void Start(int index) { this.CurrentIndex = index; this.history.Clear(); this.scroll = Vector2.zero; }

        public void Draw(Rect rect)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(10f, 8f, rect.width - 50f, 28f), "CQF_DialogGraph_Preview".Translate().Colorize(CQFEditorPalette.Accent));
            TooltipHandler.TipRegion(new Rect(10f, 8f, rect.width - 50f, 28f), "CQF_DialogGraph_PreviewHint".Translate());
            Rect close = new Rect(rect.width - 35f, 8f, 24f, 24f);
            if (CQFAIIconButton.DrawFramedImage(close, TexButton.CloseXSmall, "CQF_DialogGraph_HideInspector".Translate())) { this.editor.CloseSidePanel(); return; }
            if (CQFAIIconButton.DrawText(new Rect(8f, 44f, (rect.width - 24f) / 2f, 32f), "CQF_DialogGraph_Entry".Translate())) this.Start(0);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && this.history.Count > 0;
            if (CQFAIIconButton.DrawText(new Rect(rect.width / 2f + 4f, 44f, (rect.width - 24f) / 2f, 32f), "Back".Translate()))
            { this.CurrentIndex = this.history.Pop(); this.scroll = Vector2.zero; }
            GUI.enabled = enabled;
            if (!this.CurrentIndex.HasValue) { Widgets.Label(new Rect(8f, 94f, rect.width - 16f, 40f), "CQF_DialogGraph_End".Translate()); return; }
            if (!this.editor.CurTree.nodeMoulds.TryGetValue(this.CurrentIndex.Value, out DialogNode node))
            { Widgets.Label(new Rect(8f, 94f, rect.width - 16f, 40f), "CQF_DialogAI_MissingNode".Translate()); return; }
            Rect view = new Rect(0f, 90f, rect.width, rect.height - 90f);
            Rect content = new Rect(0f, 0f, view.width - 20f, Mathf.Max(view.height, this.height));
            Widgets.BeginScrollView(view, ref this.scroll, content);
            using CQFUIScope cqfContentScope1 = new CQFUIScope(content.width);
            try
            {
                float width = content.width - 16f, y = 8f;
                foreach (DialogImage image in node.images)
                {
                    if (string.IsNullOrEmpty(image.imagePath)) continue;
                    Texture2D texture = ContentFinder<Texture2D>.Get(image.imagePath, false);
                    if (texture != null) Widgets.DrawTextureFitted(new Rect(8f, y, width, 120f), texture, Mathf.Clamp(image.scale, 0.1f, 3f));
                    else Widgets.Label(new Rect(8f, y, width, 120f), "CQF_DialogGraph_MissingImage".Translate(image.imagePath));
                    y += 132f;
                }
                string text = node.text.CanTranslate() ? node.text.Translate().ToString() : node.text;
                float height = Mathf.Max(80f, Text.CalcHeight(text, width));
                Widgets.Label(new Rect(8f, y, width, height), text); y += height + 20f;
                foreach (DialogOption option in node.options)
                {
                    string label = option.text.CanTranslate() ? option.text.Translate().ToString() : option.text;
                    for (int i = 0; i < Math.Max(1, option.results.Count); i++)
                    {
                        DialogResult? result = i < option.results.Count ? option.results[i] : null;
                        string caption = option.results.Count > 1 ? label + " · " + (result!.resultName.CanTranslate() ? result.resultName.Translate().ToString() : result.resultName) : label;
                        if (CQFAIIconButton.DrawText(new Rect(8f, y, width, 36f), caption, tip: caption + "\n" + "CQF_DialogGraph_PreviewHint".Translate()))
                        { this.history.Push(node.index.GetValueOrDefault()); this.CurrentIndex = result?.nextIndex; this.scroll = Vector2.zero; }
                        y += 44f;
                    }
                }
                this.height = y + 20f;
            }
            finally { Widgets.EndScrollView(); }
        }

        private readonly QuestEditor_Dialog editor;
        private readonly Stack<int> history = new Stack<int>();
        private Vector2 scroll;
        private float height = 500f;
    }
}
