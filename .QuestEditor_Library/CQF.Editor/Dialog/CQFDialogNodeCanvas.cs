using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using UnityEvent = UnityEngine.Event;

namespace QuestEditor_Library
{
    public sealed partial class CQFDialogNodeCanvas
    {
        public CQFDialogNodeCanvas(QuestEditor_Dialog editor) { this.editor = editor; }

        public int? SelectedIndex { get; private set; }
        public DialogOption? SelectedOption { get; private set; }
        public HashSet<int> HighlightedNodes { get; set; } = new HashSet<int>();
        public Rect? InputBlockedRect { get; set; }
        private DialogTreeDef Tree => this.previewTree ?? this.editor.CurTree;
        private Vector2 pan { get => this.viewport.Pan; set => this.viewport.Pan = value; }
        private Vector2 viewSize { get => this.viewport.Size; set => this.viewport.Size = value; }
        private float zoom { get => this.viewport.Zoom; set => this.viewport.Zoom = value; }

        public void SetPreview(DialogTreeDef tree) { this.previewTree = tree; this.EnsurePositions(); }

        public void Select(DialogNode? node, DialogOption? option)
        {
            this.SelectedIndex = node?.index;
            this.SelectedOption = option;
        }

        public void Reset()
        {
            this.SelectedIndex = null;
            this.SelectedOption = null;
            this.dragging = null;
            this.linkNode = null;
            this.linkReference = null;
            this.linkResult = null;
            this.linkOption = null;
            this.viewport.Reset();
        }

        public void Arrange()
        {
            this.layouts.Clear(); this.bodyHeights.Clear();
            this.Tree.Update();
            Dictionary<int, int> depths = new Dictionary<int, int>();
            Queue<int> pending = new Queue<int>();
            if (this.Tree.nodeMoulds.ContainsKey(0)) { depths[0] = 0; pending.Enqueue(0); }
            while (pending.Count > 0)
            {
                int id = pending.Dequeue();
                foreach (DialogResult result in this.Tree.nodeMoulds[id].options.SelectMany(option => option.results))
                    if (result.nextIndex.HasValue && this.Tree.nodeMoulds.ContainsKey(result.nextIndex.Value) && !depths.ContainsKey(result.nextIndex.Value))
                    { depths[result.nextIndex.Value] = depths[id] + 1; pending.Enqueue(result.nextIndex.Value); }
            }
            Dictionary<int, float> heights = new Dictionary<int, float>();
            int idleColumn = depths.Count == 0 ? 0 : depths.Values.Max() + 1;
            foreach (var pair in this.Tree.nodeMoulds.OrderBy(pair => pair.Key))
            {
                int column = depths.TryGetValue(pair.Key, out int depth) ? depth : idleColumn;
                heights.TryGetValue(column, out float y);
                pair.Value.editorX = column * 720f;
                pair.Value.editorY = y;
                pair.Value.editorPositionSet = true;
                heights[column] = y + this.NodeHeight(pair.Value) + 60f;
            }
            Dictionary<int, float> optionHeights = new Dictionary<int, float>();
            foreach (DialogOption option in this.Tree.optionMoulds.Values)
            {
                DialogNode? owner = this.Tree.nodeMoulds.Values.FirstOrDefault(node => node.options.Contains(option));
                int column = owner != null ? Mathf.RoundToInt(owner.editorX / 720f) : idleColumn;
                optionHeights.TryGetValue(column, out float y);
                option.editorX = column * 720f + 360f;
                option.editorY = y;
                option.editorPositionSet = true;
                optionHeights[column] = y + this.OptionHeight(option) + 36f;
            }
            this.editor.RecordChanges();
        }

        public void EnsurePositions()
        {
            this.Tree.ResolveOptions();
            float nextX = this.Tree.nodeMoulds.Values.Where(node => node.editorPositionSet).Select(node => node.editorX + 720f).DefaultIfEmpty(0f).Max();
            int i = 0;
            foreach (DialogNode node in this.Tree.nodeMoulds.Values)
                if (!node.editorPositionSet) { node.editorX = nextX; node.editorY = i++ * 280f; node.editorPositionSet = true; }
            foreach (DialogOption option in this.Tree.optionMoulds.Values)
            {
                if (option.editorPositionSet) continue;
                DialogNode? owner = this.Tree.nodeMoulds.Values.FirstOrDefault(node => node.options.Contains(option));
                Vector2 position = this.GetOptionPosition(owner, this.OptionHeight(option));
                option.editorX = position.x;
                option.editorY = position.y;
                option.editorPositionSet = true;
            }
        }

        public Vector2 GetOptionPosition(DialogNode? owner, float height = 170f)
        {
            float x = (owner?.editorX ?? 0f) + 360f, y = owner?.editorY ?? 0f;
            Rect[] occupied = this.Tree.optionMoulds.Values.Where(option => option.editorPositionSet)
                .Select(option => new Rect(option.editorX, option.editorY, CardWidth, this.OptionHeight(option))).ToArray();
            while (true)
            {
                Rect area = new Rect(x, y, CardWidth, height).ExpandedBy(12f);
                Rect[] overlaps = occupied.Where(rect => rect.Overlaps(area)).ToArray();
                if (overlaps.Length == 0) return new Vector2(x, y);
                y = overlaps.Max(rect => rect.yMax) + 36f;
            }
        }

        public void Focus(int index)
        {
            if (!this.Tree.nodeMoulds.TryGetValue(index, out DialogNode node)) return;
            this.SelectedIndex = index;
            this.SelectedOption = null;
            this.pan = this.viewSize * 0.3f - new Vector2(node.editorX, node.editorY) * this.zoom;
            this.editor.SelectNode(node);
        }

        public void Fit()
        {
            this.layouts.Clear(); this.bodyHeights.Clear();
            this.EnsurePositions();
            Rect[] rects = this.Tree.nodeMoulds.Values.Select(node => new Rect(node.editorX, node.editorY, CardWidth, this.NodeHeight(node)))
                .Concat(this.Tree.optionMoulds.Values.Select(option => new Rect(option.editorX, option.editorY, CardWidth, this.OptionHeight(option)))).ToArray();
            if (rects.Length == 0) return;
            float minX = rects.Min(rect => rect.x), minY = rects.Min(rect => rect.y);
            this.zoom = Mathf.Clamp(Mathf.Min((this.viewSize.x - 70f) / (rects.Max(rect => rect.xMax) - minX),
                (this.viewSize.y - 70f) / (rects.Max(rect => rect.yMax) - minY)), 0.25f, 1.35f);
            this.pan = new Vector2(35f - minX * this.zoom, 35f - minY * this.zoom);
        }

        public void Draw(Rect rect, bool editable = true)
        {
            GameFont previousFont = Text.Font;
            Text.Font = GameFont.Small;
            this.viewSize = rect.size;
            this.bodyHeights.Clear();
            this.layouts.Clear();
            this.cardLayouts.Clear();
            this.EnsurePositions();
            Rect? blocked = this.InputBlockedRect;
            if (blocked.HasValue) this.InputBlockedRect = new Rect(blocked.Value.position - rect.position, blocked.Value.size);
            Widgets.DrawBoxSolid(rect, CQFEditorPalette.Canvas);
            GUI.BeginGroup(rect);
            try
            {
                Rect view = new Rect(Vector2.zero, rect.size);
                Dictionary<DialogNode, Rect> nodes = this.Tree.nodeMoulds.Values.ToDictionary(node => node, node => this.ScreenRect(node.editorX, node.editorY, this.NodeHeight(node)));
                Dictionary<DialogOption, Rect> options = this.Tree.optionMoulds.Values.ToDictionary(option => option, option => this.ScreenRect(option.editorX, option.editorY, this.OptionHeight(option)));
                this.HandleInput(view, nodes, options, editable);
                nodes = this.Tree.nodeMoulds.Values.ToDictionary(node => node, node => this.ScreenRect(node.editorX, node.editorY, this.NodeHeight(node)));
                options = this.Tree.optionMoulds.Values.ToDictionary(option => option, option => this.ScreenRect(option.editorX, option.editorY, this.OptionHeight(option)));
                if (UnityEvent.current.type == EventType.Repaint)
                {
                    float step = 32f * this.zoom;
                    for (float x = this.pan.x % step; x < view.width; x += step)
                        for (float y = this.pan.y % step; y < view.height; y += step)
                            Widgets.DrawBoxSolid(new Rect(x, y, 1f, 1f), new Color(0.24f, 0.27f, 0.30f));
                    foreach (var pair in nodes)
                        for (int i = 0; i < pair.Key.options.Count; i++)
                            if (options.TryGetValue(pair.Key.options[i], out Rect target))
                                this.DrawLink(this.NodePort(pair.Key, pair.Value, i), this.InputPort(target), pair.Key.index == this.SelectedIndex ? Accent : LinkColor);
                    foreach (var pair in options)
                        for (int i = 0; i < pair.Key.results.Count; i++)
                            if (pair.Key.results[i].nextIndex.HasValue && this.Tree.nodeMoulds.TryGetValue(pair.Key.results[i].nextIndex!.Value, out DialogNode target) && nodes.TryGetValue(target, out Rect targetRect))
                                this.DrawLink(this.OptionPort(pair.Key, pair.Value, i), this.InputPort(targetRect), pair.Key == this.SelectedOption ? Accent : LinkColor);
                    if (this.linkNode != null && nodes.TryGetValue(this.linkNode, out Rect source))
                        this.DrawLink(this.NodePort(this.linkNode, source, this.linkReference == null ? -1 : this.linkNode.options.IndexOf(this.linkReference)), UnityEvent.current.mousePosition, Accent);
                    if (this.linkOption != null && this.linkResult != null && options.TryGetValue(this.linkOption, out Rect choice))
                        this.DrawLink(this.OptionPort(this.linkOption, choice, this.linkOption.results.IndexOf(this.linkResult)), UnityEvent.current.mousePosition, Accent);
                }
                foreach (var pair in nodes) if (view.Overlaps(pair.Value.ExpandedBy(12f))) this.DrawNode(pair.Key, pair.Value);
                foreach (var pair in options) if (view.Overlaps(pair.Value.ExpandedBy(12f))) this.DrawOption(pair.Key, pair.Value);
                float controlsWidth = this.InputBlockedRect.HasValue ? Mathf.Max(0f, this.InputBlockedRect.Value.x) : view.width;
                if (controlsWidth >= 190f) this.DrawControls(new Rect(0f, 0f, controlsWidth, view.height));
            }
            finally { GUI.EndGroup(); Text.Font = previousFont; this.InputBlockedRect = blocked; }
        }

        private Rect ScreenRect(float x, float y, float height) => this.viewport.ToScreen(new Rect(x, y, CardWidth, height));
        private string Display(string text) => text.CanTranslate() ? text.Translate().ToString() : text ?? string.Empty;
        private float BodyHeight(string text)
        {
            string value = this.Display(text);
            if (this.bodyHeights.TryGetValue(value, out float height)) return height;
            GameFont previous = Text.Font;
            Text.Font = GameFont.Small;
            try
            {
                height = this.renderer.MeasureBody(value, this.zoom);
                this.bodyHeights[value] = height;
                return height;
            }
            finally { Text.Font = previous; }
        }
        private float NodeHeight(DialogNode node)
        {
            if (!this.layouts.TryGetValue(node, out CQFDialogNodeLayout layout))
                this.layouts[node] = layout = new CQFDialogNodeLayout(node, this.BodyHeight(node.text));
            return layout.Height;
        }
        private float OptionHeight(DialogOption option)
        {
            if (!this.layouts.TryGetValue(option, out CQFDialogNodeLayout layout))
                this.layouts[option] = layout = new CQFDialogNodeLayout(option, this.BodyHeight(option.text));
            return layout.Height;
        }
        private CQFDialogCardLayout CardLayout(object entry, Rect card)
        {
            if (!this.cardLayouts.TryGetValue(entry, out CQFDialogCardLayout layout) || layout.Bounds != card)
                this.cardLayouts[entry] = layout = new CQFDialogCardLayout(this.layouts[entry], card);
            return layout;
        }
        private Vector2 InputPort(Rect card) => CQFDialogCardLayout.InputAnchor(card);
        private Vector2 NodePort(DialogNode node, Rect card, int index) => index < 0 ? this.CardLayout(node, card).Output : this.CardLayout(node, card).RowOutput(index);
        private Vector2 OptionPort(DialogOption option, Rect card, int index) => this.CardLayout(option, card).RowOutput(Math.Max(0, index));
        private Rect PortRect(Vector2 point) => CQFDialogCardLayout.HitArea(point, this.zoom);

        private void ZoomAt(Vector2 center, float factor)
        {
            this.viewport.ZoomAt(center, factor);
            this.layouts.Clear(); this.bodyHeights.Clear();
            this.cardLayouts.Clear();
        }

        private const float CardWidth = CQFDialogNodeLayout.Width;
        private static readonly Color Accent = CQFEditorPalette.Accent;
        private static readonly Color LinkColor = CQFEditorPalette.Link;
        private readonly QuestEditor_Dialog editor;
        private DialogTreeDef? previewTree;
        private object? dragging;
        private DialogNode? linkNode;
        private DialogOption? linkReference;
        private DialogOption? linkOption;
        private DialogResult? linkResult;
        private bool panning;
        private readonly CQFDialogCanvasViewport viewport = new CQFDialogCanvasViewport();
        private readonly CQFDialogCardRenderer renderer = new CQFDialogCardRenderer();
        private readonly Dictionary<string, float> bodyHeights = new Dictionary<string, float>();
        private readonly Dictionary<object, CQFDialogNodeLayout> layouts = new Dictionary<object, CQFDialogNodeLayout>();
        private readonly Dictionary<object, CQFDialogCardLayout> cardLayouts = new Dictionary<object, CQFDialogCardLayout>();
    }
}
