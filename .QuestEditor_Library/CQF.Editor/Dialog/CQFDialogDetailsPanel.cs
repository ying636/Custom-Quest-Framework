using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using UnityEvent = UnityEngine.Event;

namespace QuestEditor_Library
{
    public sealed class CQFDialogDetailsPanel
    {
        public CQFDialogDetailsPanel(QuestEditor_Dialog editor) { this.editor = editor; }

        public void Reset() { this.scroll = Vector2.zero; this.dragOption = null; }

        public void Draw(Rect rect)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWrap = Text.WordWrap;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            try
            {
                DialogNode? node = this.editor.SelectedNode;
                DialogOption? option = this.editor.CurrentOption;
                DialogResult? result = this.editor.CurrentResult;
                if (node == null && option == null)
                { Widgets.Label(rect.ContractedBy(12f), "CQF_DialogGraph_SelectHint".Translate()); return; }
                Widgets.Label(new Rect(10f, 8f, rect.width - (result != null ? 85f : 50f), 28f),
                    (result != null ? "DialogResults" : option != null ? "OptionText" : "DialogText").Translate().Colorize(CQFEditorPalette.Accent));
                Rect close = new Rect(rect.width - 35f, 8f, 24f, 24f);
                if (CQFAIIconButton.DrawFramedImage(close, TexButton.CloseXSmall, "CQF_DialogGraph_HideInspector".Translate())) { this.editor.CloseSidePanel(); return; }
                if (option != null && result != null && CQFAIIconButton.DrawFramedImage(new Rect(rect.width - 67f, 8f, 24f, 24f), TexUI.ArrowTexLeft, "Back".Translate()))
                { this.editor.SelectOption(node, option); return; }
                Rect view = new Rect(0f, 44f, rect.width, rect.height - 44f);
                Rect content = new Rect(0f, 0f, view.width - 20f, Mathf.Max(view.height, this.height));
                Widgets.BeginScrollView(view, ref this.scroll, content);
                using CQFUIScope cqfContentScope1 = new CQFUIScope(content.width);
                try
                {
                    float y = 6f, width = content.width - 16f;
                    if (result != null && option != null) this.DrawResult(result, option, node, ref y, width, content);
                    else if (option != null) this.DrawOption(option, node, ref y, width, content);
                    else if (node != null) this.DrawNode(node, ref y, width, content);
                    this.height = y + 20f;
                }
                finally { Widgets.EndScrollView(); }
            }
            finally { Text.Font = oldFont; Text.Anchor = oldAnchor; Text.WordWrap = oldWrap; }
        }

        private void DrawNode(DialogNode node, ref float y, float width, Rect content)
        {
            node.text = Widgets.TextArea(new Rect(8f, y, width, 150f), node.text ?? string.Empty);
            y += 164f;
            this.Header("DialogOptions", ref y, width, () => this.editor.AddOption(node));
            for (int i = 0; i < node.options.Count; i++)
            {
                DialogOption option = node.options[i];
                Rect row = new Rect(8f, y, width, 34f);
                Widgets.DrawBoxSolid(row, CQFEditorPalette.Header);
                Rect grip = new Rect(12f, y + 5f, 23f, 23f);
                CQFAIIconButton.DrawGlyph(grip, TexButton.DragHash);
                TooltipHandler.TipRegion(grip, "CQF_DialogGraph_OptionOrder".Translate());
                UnityEvent input = UnityEvent.current;
                if (input.type == EventType.MouseDown && input.button == 0 && grip.Contains(input.mousePosition))
                { this.dragOption = option; input.Use(); }
                if (input.type == EventType.MouseDrag && this.dragOption != null) input.Use();
                if (input.type == EventType.MouseUp && this.dragOption != null && row.Contains(input.mousePosition))
                {
                    int from = node.options.IndexOf(this.dragOption);
                    if (from >= 0) { node.options.RemoveAt(from); node.options.Insert(i, this.dragOption); this.editor.InitCurTree(); }
                    this.dragOption = null; input.Use();
                }
                this.Context(row, () => this.editor.ShowOptionMenu(option, node));
                Rect text = new Rect(40f, y + 4f, width - 38f, 27f);
                if (CQFAIIconButton.DrawText(text, this.Display(option.text).Replace('\n', ' '))) this.editor.SelectOption(node, option);
                y += 39f;
            }
            if (UnityEvent.current.type == EventType.MouseUp) this.dragOption = null;
            if (node.options.Count == 0) this.Empty("CQF_NoDialogOptions", ref y, width);
            this.Fold("ExtraDialogText", ref this.extra, ref y, width);
            if (this.extra)
            {
                this.Header("ExtraDialogText", ref y, width, () => node.extraText.Add("CQF_Dialog_Extra_" + node.extraText.Count));
                for (int i = 0; i < node.extraText.Count; i++)
                {
                    int index = i;
                    Rect row = new Rect(8f, y, width, 80f);
                    this.Context(row, () => this.RemoveMenu(() => node.extraText.RemoveAt(index)));
                    node.extraText[i] = Widgets.TextArea(row, node.extraText[i]); y += 88f;
                }
            }
            this.Fold("DialogImages", ref this.images, ref y, width);
            if (!this.images) return;
            this.Header("DialogImages", ref y, width, () => node.images.Add(new DialogImage()));
            foreach (DialogImage image in node.images.ToArray())
            {
                Rect row = new Rect(8f, y, width, 100f);
                this.Context(row, () => this.RemoveMenu(() => node.images.Remove(image)));
                Texture2D? texture = string.IsNullOrEmpty(image.imagePath) ? null : ContentFinder<Texture2D>.Get(image.imagePath, false);
                Rect thumb = new Rect(8f, y, 80f, 70f);
                if (texture != null) Widgets.DrawTextureFitted(thumb, texture, 1f);
                else Widgets.DrawBoxSolid(thumb, CQFEditorPalette.Canvas);
                if (Widgets.ButtonInvisible(thumb)) Find.WindowStack.Add(new Dialog_SelectDialogImage(path => image.imagePath = path, image.imagePath ?? string.Empty));
                TooltipHandler.TipRegion(thumb, "DialogImage_SelectTip".Translate());
                Widgets.Label(new Rect(98f, y, width - 96f, 24f), "DialogImage_Scale".Translate());
                Widgets.TextFieldNumeric(new Rect(98f, y + 30f, width - 96f, 28f), ref image.scale, ref image.buffer_scale);
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(8f, y + 74f, width, 24f), (image.imagePath ?? string.Empty).Truncate(width));
                Text.Font = GameFont.Small;
                y += 110f;
            }
        }

        private void DrawOption(DialogOption option, DialogNode? owner, ref float y, float width, Rect content)
        {
            int references = this.editor.CurTree.nodeMoulds.Values.Count(node => node.options.Contains(option));
            if (references > 1)
            {
                string message = "CQF_DialogGraph_SharedHint".Translate(references);
                float height = Text.CalcHeight(message, width);
                Widgets.Label(new Rect(8f, y, width, height), message.Colorize(CQFEditorPalette.Accent));
                y += height + 12f;
            }
            option.text = Widgets.TextArea(new Rect(8f, y, width, 100f), option.text ?? string.Empty);
            y += 114f;
            this.Header("DialogResults", ref y, width, () => option.results.Add(new DialogResult { resultName = "CQF_Dialog_Result_" + option.results.Count }));
            float resultsHeight = option.results.Count * CQFDialogResultList.RowHeight;
            this.results.Draw(option, new Rect(8f, y, width, resultsHeight), result => !result.nextIndex.HasValue ? "CQF_DialogGraph_End".Translate().ToString()
                : this.editor.CurTree.nodeMoulds.TryGetValue(result.nextIndex.Value, out DialogNode target) ? this.Display(target.text) : "CQF_DialogAI_MissingNode".Translate().ToString(),
                result => this.editor.SelectResult(owner, option, result), result => this.editor.ShowOptionMenu(option, owner, result), this.editor.InitCurTree);
            y += resultsHeight;
            this.Fold("CQF_DialogGraph_Advanced", ref this.advanced, ref y, width);
            if (!this.advanced) return;
            Widgets.CheckboxLabeled(new Rect(8f, y, width, 28f), "HideWhenDisable".Translate(), ref option.hideWhenDisabled); y += 34f;
            Widgets.CheckboxLabeled(new Rect(8f, y, width, 28f), "hideFailReason".Translate(), ref option.hideFailReason); y += 34f;
            Widgets.CheckboxLabeled(new Rect(8f, y, width, 28f), "removeDialogAfterSelect".Translate(), ref option.removeDialogAfterSelect); y += 34f;
            this.Header("InteractionOption_RequiredThing", ref y, width, () => CQFEditorTools.DrawFloatMenu(typeof(CQFThingData).AllSubclassesNonAbstract().Where(type => type != typeof(CQFThingCategoryCount)).ToList(),
                type => CQFThingData.OpenSelectWindow(type, data => option.requiredThings.Add(data)), type => type.Name.Translate()));
            foreach (CQFThingData data in option.requiredThings.ToArray())
            {
                float start = y;
                this.Entry(data, ref y, width, area => { float innerY = 0f; data.DrawWithSingleCount(ref innerY, area, 0f); return innerY; });
                this.Context(new Rect(8f, start, width, y - start), () => this.RemoveMenu(() => option.requiredThings.Remove(data))); y += 8f;
            }
            this.DrawConditions(option.conditions, ref y, width, content);
        }

        private void DrawResult(DialogResult result, DialogOption option, DialogNode? owner, ref float y, float width, Rect content)
        {
            Widgets.Label(new Rect(8f, y, width, 25f), "ResultName".Translate()); y += 28f;
            result.resultName = Widgets.TextField(new Rect(8f, y, width, 30f), result.resultName ?? string.Empty); y += 42f;
            string target = !result.nextIndex.HasValue ? "CQF_DialogGraph_End".Translate().ToString()
                : this.editor.CurTree.nodeMoulds.TryGetValue(result.nextIndex.Value, out DialogNode node) ? this.Display(node.text) : "CQF_DialogAI_MissingNode".Translate().ToString();
            if (CQFAIIconButton.DrawText(new Rect(8f, y, width, 32f), target.Replace('\n', ' ')))
            {
                List<FloatMenuOption> menu = this.editor.CurTree.nodeMoulds.Values.Select(value => new FloatMenuOption(this.Display(value.text).Replace('\n', ' '),
                    () => { result.nextIndex = value.index; this.editor.InitCurTree(); })).ToList();
                menu.Insert(0, new FloatMenuOption("CQF_DialogGraph_End".Translate(), () => { result.nextIndex = null; this.editor.InitCurTree(); }));
                Find.WindowStack.Add(new FloatMenu(menu));
            }
            y += 48f;
            this.Header("CQFActions", ref y, width, () => CQFEditorTools.OpenCQFActionSelect(type => result.actions.Add((CQFAction)Activator.CreateInstance(type))));
            foreach (CQFAction action in result.actions.ToArray())
            {
                float start = y;
                this.Entry(action, ref y, width, area => { float innerY = 0f; action.Draw(ref innerY, area, 0f); return innerY; });
                this.Context(new Rect(8f, start, width, Math.Max(30f, y - start)), () => this.RemoveMenu(() => result.actions.Remove(action))); y += 8f;
            }
            if (result.actions.Count == 0) this.Empty("CQF_NoDialogActions", ref y, width);
            this.DrawConditions(result.conditions, ref y, width, content);
        }

        private void DrawConditions(List<DialogCondition> conditions, ref float y, float width, Rect content)
        {
            this.Header("DialogConditions", ref y, width, () => CQFEditorTools.DrawFloatMenu(typeof(DialogCondition).AllSubclassesNonAbstract(),
                type => conditions.Add((DialogCondition)Activator.CreateInstance(type)), type => type.Name.Translate()));
            foreach (DialogCondition condition in conditions.ToArray())
            {
                float start = y;
                this.Entry(condition, ref y, width, area => { float innerY = 0f; condition.Draw(ref innerY, area, 0f); return innerY; });
                this.Context(new Rect(8f, start, width, Math.Max(30f, y - start)), () => this.RemoveMenu(() => conditions.Remove(condition))); y += 8f;
            }
            if (conditions.Count == 0) this.Empty("CQF_NoDialogConditions", ref y, width);
        }

        private void Header(string key, ref float y, float width, Action add)
        {
            Widgets.DrawBoxSolid(new Rect(8f, y, width, 32f), CQFEditorPalette.Header);
            Widgets.Label(new Rect(14f, y + 4f, width - 42f, 25f), key.Translate());
            if (CQFAIIconButton.DrawFramedImage(new Rect(width - 18f, y + 4f, 24f, 24f), TexButton.Plus, "Add".Translate())) add();
            y += 42f;
        }

        private void Fold(string key, ref bool open, ref float y, float width)
        {
            Rect row = new Rect(8f, y + 8f, width, 32f);
            CQFAIIconButton.DrawBackground(row, open);
            CQFAIIconButton.DrawGlyph(new Rect(14f, row.y + 6f, 20f, 20f), open ? TexButton.ReorderDown : TexUI.ArrowTexRight);
            Widgets.Label(new Rect(42f, row.y + 4f, width - 42f, 26f), key.Translate().ToString().Truncate(width - 42f));
            TooltipHandler.TipRegion(row, key.Translate());
            if (Widgets.ButtonInvisible(row)) open = !open;
            y += 46f;
        }

        private void Entry(object entry, ref float y, float width, Func<Rect, float> draw)
        {
            Rect row = new Rect(8f, y, width, 32f);
            bool open = this.expanded.Contains(entry);
            CQFAIIconButton.DrawBackground(row, open);
            CQFAIIconButton.DrawGlyph(new Rect(14f, y + 6f, 20f, 20f), open ? TexButton.ReorderDown : TexUI.ArrowTexRight);
            string title = CQFEditorEntrySummary.Describe(entry);
            Widgets.Label(new Rect(42f, y + 4f, width - 42f, 26f), title.Truncate(width - 42f));
            TooltipHandler.TipRegion(row, CQFEditorEntrySummary.Describe(entry));
            if (Widgets.ButtonInvisible(row))
            {
                if (open) this.expanded.Remove(entry);
                else this.expanded.Add(entry);
            }
            y += 38f;
            if (open) CQFEditorInlineLayout.Draw(entry, ref y, 8f, width, draw);
        }

        private void Empty(string key, ref float y, float width) { Widgets.Label(new Rect(8f, y, width, 28f), key.Translate().Colorize(CQFUIStyle.Muted)); y += 34f; }
        private string Display(string text) => text.CanTranslate() ? text.Translate().ToString() : text ?? string.Empty;
        private void RemoveMenu(Action remove) => Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> { new FloatMenuOption("Remove".Translate(), () => { remove(); this.editor.InitCurTree(); }) }));

        private void Context(Rect rect, Action menu)
        {
            if (UnityEvent.current.type == EventType.MouseDown && UnityEvent.current.button == 1 && rect.Contains(UnityEvent.current.mousePosition))
            { menu(); UnityEvent.current.Use(); }
        }

        private readonly QuestEditor_Dialog editor;
        private readonly CQFDialogResultList results = new CQFDialogResultList();
        private Vector2 scroll;
        private float height = 700f;
        private bool advanced;
        private bool extra;
        private bool images;
        private DialogOption? dragOption;
        private readonly HashSet<object> expanded = new HashSet<object>();
    }
}
