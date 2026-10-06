using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using UnityEvent = UnityEngine.Event;

namespace QuestEditor_Library
{
    public sealed class CQFDialogNodeCanvas
    {
        public CQFDialogNodeCanvas(QuestEditor_Dialog editor)
        {
            this.editor = editor;
        }

        public int? SelectedIndex { get; private set; }
        public HashSet<int> HighlightedNodes { get; set; } = new HashSet<int>();
        private DialogTreeDef Tree => this.previewTree ?? this.editor.CurTree;

        public void SetPreview(DialogTreeDef tree)
        {
            this.previewTree = tree;
            this.EnsurePositions();
        }

        public void Reset()
        {
            this.SelectedIndex = null;
            this.dragging = null;
            this.linking = null;
            this.pan = new Vector2(35f, 35f);
            this.zoom = 0.85f;
        }

        public void Arrange()
        {
            Dictionary<int, int> depths = new Dictionary<int, int>();
            Queue<int> pending = new Queue<int>();
            if (this.Tree.nodeMoulds.ContainsKey(0))
            {
                depths.Add(0, 0);
                pending.Enqueue(0);
            }
            while (pending.Count > 0)
            {
                int index = pending.Dequeue();
                foreach (DialogResult result in this.Tree.nodeMoulds[index].options.SelectMany(option => option.results))
                {
                    if (result.nextIndex.HasValue && this.Tree.nodeMoulds.ContainsKey(result.nextIndex.Value)
                        && !depths.ContainsKey(result.nextIndex.Value))
                    {
                        depths.Add(result.nextIndex.Value, depths[index] + 1);
                        pending.Enqueue(result.nextIndex.Value);
                    }
                }
            }
            Dictionary<int, float> heights = new Dictionary<int, float>();
            int idleColumn = depths.Count == 0 ? 0 : depths.Values.Max() + 1;
            foreach (KeyValuePair<int, DialogNode> pair in this.Tree.nodeMoulds.OrderBy(pair => pair.Key))
            {
                int column = depths.TryGetValue(pair.Key, out int depth) ? depth : idleColumn;
                heights.TryGetValue(column, out float y);
                pair.Value.editorX = column * 360f;
                pair.Value.editorY = y;
                pair.Value.editorPositionSet = true;
                heights[column] = y + this.NodeHeight(pair.Value) + 55f;
            }
            this.editor.RecordChanges();
        }

        public void EnsurePositions()
        {
            DialogNode[] placed = this.Tree.nodeMoulds.Values.Where(node => node.editorPositionSet).ToArray();
            float nextX = placed.Length == 0 ? 0f : placed.Max(node => node.editorX) + 360f;
            int index = 0;
            foreach (DialogNode node in this.Tree.nodeMoulds.Values)
            {
                if (!node.editorPositionSet)
                {
                    node.editorX = nextX;
                    node.editorY = index * 360f;
                    node.editorPositionSet = true;
                    index++;
                }
            }
        }

        public void Focus(int index)
        {
            if (this.Tree.nodeMoulds.TryGetValue(index, out DialogNode node))
            {
                this.SelectedIndex = index;
                this.pan = new Vector2(this.viewSize.x * 0.35f, this.viewSize.y * 0.3f)
                    - new Vector2(node.editorX, node.editorY) * this.zoom;
                this.editor.SelectNode(node);
            }
        }

        public void Fit()
        {
            if (this.Tree.nodeMoulds.Count == 0)
            {
                return;
            }
            float minX = this.Tree.nodeMoulds.Values.Min(node => node.editorX);
            float minY = this.Tree.nodeMoulds.Values.Min(node => node.editorY);
            float maxX = this.Tree.nodeMoulds.Values.Max(node => node.editorX + CardWidth);
            float maxY = this.Tree.nodeMoulds.Values.Max(node => node.editorY + this.NodeHeight(node));
            this.zoom = Mathf.Clamp(Mathf.Min((this.viewSize.x - 60f) / (maxX - minX),
                (this.viewSize.y - 60f) / (maxY - minY)), 0.25f, 1.35f);
            this.pan = new Vector2(30f - minX * this.zoom, 30f - minY * this.zoom);
        }

        public void Draw(Rect rect, bool editable = true)
        {
            this.viewSize = rect.size;
            Widgets.DrawBoxSolid(rect, new Color(0.055f, 0.085f, 0.075f));
            GUI.BeginGroup(rect);
            Rect localRect = new Rect(Vector2.zero, rect.size);
            Dictionary<DialogNode, Rect> cards = this.Tree.nodeMoulds.Values.ToDictionary(node => node,
                node => new Rect(new Vector2(node.editorX, node.editorY) * this.zoom + this.pan,
                    new Vector2(CardWidth, this.NodeHeight(node)) * this.zoom));
            if (editable) this.HandleInput(localRect, cards);
            else this.HandleViewInput(localRect);
            if (UnityEvent.current.type == EventType.Repaint)
            {
                float step = 32f * this.zoom;
                for (float x = this.pan.x % step; x < rect.width; x += step)
                {
                    for (float y = this.pan.y % step; y < rect.height; y += step)
                    {
                        Widgets.DrawBoxSolid(new Rect(x, y, 1f, 1f), new Color(0.2f, 0.27f, 0.22f));
                    }
                }
                foreach (KeyValuePair<DialogNode, Rect> pair in cards)
                {
                    foreach (DialogOption option in pair.Key.options)
                    {
                        foreach (DialogResult result in option.results)
                        {
                            if (result.nextIndex.HasValue && this.Tree.nodeMoulds.TryGetValue(result.nextIndex.Value, out DialogNode target)
                                && cards.TryGetValue(target, out Rect targetRect))
                            {
                                Vector2 start = this.ResultPort(pair.Key, result, pair.Value);
                                Vector2 end = new Vector2(targetRect.x, targetRect.y + 18f * this.zoom);
                                if (localRect.Overlaps(new Rect(Mathf.Min(start.x, end.x) - 120f, Mathf.Min(start.y, end.y) - 120f,
                                    Mathf.Abs(start.x - end.x) + 240f, Mathf.Abs(start.y - end.y) + 240f)))
                                {
                                    this.DrawLink(start, end, pair.Key.index == this.SelectedIndex ? ColorLibrary.SkyBlue : new Color(0.5f, 0.66f, 0.4f));
                                }
                            }
                        }
                    }
                }
                if (this.linking != null && this.linkSource != null && cards.TryGetValue(this.linkSource, out Rect sourceRect))
                {
                    this.DrawLink(this.ResultPort(this.linkSource, this.linking, sourceRect), UnityEvent.current.mousePosition, ColorLibrary.SkyBlue);
                }
            }
            foreach (KeyValuePair<DialogNode, Rect> pair in cards)
            {
                if (localRect.Overlaps(pair.Value.ExpandedBy(12f)))
                {
                    this.DrawNode(pair.Key, pair.Value);
                }
            }
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(10f, rect.height - 25f, rect.width - 20f, 22f), "CQF_DialogGraph_Hint".Translate());
            Widgets.Label(new Rect(rect.width - 70f, 8f, 60f, 22f), Mathf.RoundToInt(this.zoom * 100f) + "%");
            Text.Font = GameFont.Small;
            GUI.EndGroup();
        }

        private float NodeHeight(DialogNode node)
        {
            return 145f + node.options.Sum(option => 30f + Math.Max(1, option.results.Count) * 25f);
        }

        private Vector2 ResultPort(DialogNode node, DialogResult result, Rect card)
        {
            float y = 132f;
            foreach (DialogOption option in node.options)
            {
                y += 30f;
                foreach (DialogResult current in option.results)
                {
                    if (current == result)
                    {
                        return new Vector2(card.xMax, card.y + (y + 12f) * this.zoom);
                    }
                    y += 25f;
                }
                if (option.results.Count == 0)
                {
                    y += 25f;
                }
            }
            return card.center;
        }

        private void DrawNode(DialogNode node, Rect card)
        {
            Color border = node.index == this.SelectedIndex ? new Color(0.72f, 0.86f, 0.45f) : new Color(0.24f, 0.36f, 0.27f);
            if (node.index != this.SelectedIndex && this.HighlightedNodes.Contains(node.index.GetValueOrDefault())) border = ColorLibrary.SkyBlue;
            Widgets.DrawBoxSolid(card, border);
            Widgets.DrawBoxSolid(card.ContractedBy(2f), new Color(0.12f, 0.18f, 0.14f));
            Widgets.DrawBoxSolid(new Rect(card.x + 2f, card.y + 2f, card.width - 4f, 32f * this.zoom), new Color(0.17f, 0.25f, 0.19f));
            Text.Font = this.zoom < 0.85f ? GameFont.Tiny : GameFont.Small;
            Widgets.Label(new Rect(card.x + 12f * this.zoom, card.y + 5f * this.zoom, card.width - 24f * this.zoom, 27f),
                this.zoom < 0.55f ? "#" + node.index : "CQF_DialogGraph_Node".Translate(node.index.GetValueOrDefault()).ToString()
                    + (node.index == 0 ? "  " + "CQF_DialogGraph_Entry".Translate().ToString() : ""));
            Widgets.DrawBoxSolid(new Rect(card.x - 5f, card.y + 18f * this.zoom - 5f, 10f, 10f), border);
            Rect body = new Rect(card.x + 12f * this.zoom, card.y + 42f * this.zoom, card.width - 24f * this.zoom, 72f * this.zoom);
            GUI.BeginGroup(body);
            string text = node.text.CanTranslate() ? node.text.Translate().ToString() : node.text;
            if (this.zoom >= 0.55f) Widgets.Label(new Rect(0f, 0f, body.width, 500f), text);
            GUI.EndGroup();
            TooltipHandler.TipRegion(body, text);
            Text.Font = GameFont.Tiny;
            if (this.zoom >= 0.55f) Widgets.Label(new Rect(card.x + 12f * this.zoom, card.y + 112f * this.zoom, card.width - 24f * this.zoom, 22f),
                "CQF_DialogGraph_NodeInfo".Translate(node.options.Count, node.images.Count));
            float y = 132f;
            foreach (DialogOption option in node.options)
            {
                Rect optionRect = new Rect(card.x + 8f * this.zoom, card.y + y * this.zoom, card.width - 16f * this.zoom, 28f * this.zoom);
                Widgets.DrawHighlight(optionRect);
                if (this.zoom >= 0.55f) Widgets.Label(optionRect.ContractedBy(3f), option.text.CanTranslate() ? option.text.Translate().ToString() : option.text);
                TooltipHandler.TipRegion(optionRect, option.text);
                y += 30f;
                foreach (DialogResult result in option.results)
                {
                    string target = result.nextIndex.HasValue ? "#" + result.nextIndex.Value : "CQF_DialogGraph_End".Translate().ToString();
                    Rect resultRect = new Rect(card.x + 16f * this.zoom, card.y + y * this.zoom, card.width - 30f * this.zoom, 23f * this.zoom);
                    if (this.zoom >= 0.55f) Widgets.Label(resultRect, result.resultName + " → " + target);
                    TooltipHandler.TipRegion(resultRect, "CQF_DialogGraph_ResultInfo".Translate(result.conditions.Count, result.actions.Count));
                    Vector2 port = this.ResultPort(node, result, card);
                    bool missing = result.nextIndex.HasValue && !this.Tree.nodeMoulds.ContainsKey(result.nextIndex.Value);
                    Widgets.DrawBoxSolid(new Rect(port.x - 5f, port.y - 5f, 10f, 10f), missing ? ColorLibrary.RedReadable : result == this.linking ? ColorLibrary.SkyBlue : border);
                    if (missing) TooltipHandler.TipRegion(resultRect, "CQF_DialogAI_MissingNode".Translate() + ": " + result.nextIndex);
                    y += 25f;
                }
                if (option.results.Count == 0)
                {
                    if (this.zoom >= 0.55f) Widgets.Label(new Rect(optionRect.x, card.y + y * this.zoom, optionRect.width, 23f), "CQF_NoDialogResults".Translate());
                    y += 25f;
                }
            }
            Text.Font = GameFont.Small;
        }

        private void DrawLink(Vector2 start, Vector2 end, Color color)
        {
            float bend = Mathf.Clamp(Mathf.Abs(end.x - start.x) * 0.5f, 70f, 180f);
            Vector2 a = start + Vector2.right * bend;
            Vector2 b = end - Vector2.right * bend;
            Vector2 previous = start;
            for (int i = 1; i <= 20; i++)
            {
                float t = i / 20f;
                float u = 1f - t;
                Vector2 point = u * u * u * start + 3f * u * u * t * a + 3f * u * t * t * b + t * t * t * end;
                Widgets.DrawLine(previous, point, color, 0.65f);
                previous = point;
            }
            Widgets.DrawLine(end + new Vector2(-8f, -4f), end, color, 0.65f);
            Widgets.DrawLine(end + new Vector2(-8f, 4f), end, color, 0.65f);
        }

        private void HandleInput(Rect view, Dictionary<DialogNode, Rect> cards)
        {
            if (Find.WindowStack.MouseObscuredNow) return;
            UnityEvent input = UnityEvent.current;
            Vector2 mouse = input.mousePosition;
            if (input.type == EventType.KeyDown && input.keyCode == KeyCode.Escape && this.linking != null)
            {
                this.linking = null;
                input.Use();
            }
            if (input.type == EventType.ScrollWheel && view.Contains(mouse))
            {
                Vector2 world = (mouse - this.pan) / this.zoom;
                this.zoom = Mathf.Clamp(this.zoom * Mathf.Pow(1.1f, -input.delta.y), 0.25f, 1.6f);
                this.pan = mouse - world * this.zoom;
                input.Use();
            }
            if (input.type == EventType.MouseDrag)
            {
                if (this.dragging != null)
                {
                    this.dragging.editorX += input.delta.x / this.zoom;
                    this.dragging.editorY += input.delta.y / this.zoom;
                    input.Use();
                }
                else if (this.panning)
                {
                    this.pan += input.delta;
                    input.Use();
                }
                else if (this.linking != null)
                {
                    input.Use();
                }
            }
            if (input.type == EventType.MouseUp)
            {
                if (this.linking != null)
                {
                    DialogNode target = cards.LastOrDefault(pair => pair.Value.ExpandedBy(8f).Contains(mouse)).Key;
                    if (target != null && view.Contains(mouse))
                    {
                        this.Tree.ChangeNextNodeToOtherNode(this.linkSource, target, this.linking);
                        this.editor.RecordChanges();
                    }
                    this.linking = null;
                    input.Use();
                }
                if (this.dragging != null)
                {
                    this.editor.RecordChanges();
                    this.dragging = null;
                    input.Use();
                }
                this.panning = false;
            }
            if (input.type != EventType.MouseDown || !view.Contains(mouse))
            {
                return;
            }
            foreach (KeyValuePair<DialogNode, Rect> pair in cards.Reverse())
            {
                foreach (DialogOption option in pair.Key.options)
                {
                    foreach (DialogResult result in option.results)
                    {
                        Vector2 port = this.ResultPort(pair.Key, result, pair.Value);
                        if (new Rect(port.x - 9f, port.y - 9f, 18f, 18f).Contains(mouse))
                        {
                            this.SelectedIndex = pair.Key.index;
                            this.editor.SelectResult(pair.Key, option, result);
                            if (input.button == 0)
                            {
                                this.linking = result;
                                this.linkSource = pair.Key;
                            }
                            else if (input.button == 1)
                            {
                                this.Tree.ChangeNextNodeToOtherNode(pair.Key, null, result);
                                this.editor.RecordChanges();
                            }
                            input.Use();
                            return;
                        }
                    }
                }
                if (!pair.Value.Contains(mouse) || input.button == 2)
                {
                    continue;
                }
                this.SelectedIndex = pair.Key.index;
                this.editor.SelectNode(pair.Key);
                if (input.button == 1)
                {
                    DialogNode node = pair.Key;
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                    {
                        new FloatMenuOption("Add".Translate(), () => this.editor.AddOption(node)),
                        new FloatMenuOption("Remove".Translate(), () => this.editor.RemoveNode(node))
                    }));
                }
                else if (mouse.y < pair.Value.y + 34f * this.zoom)
                {
                    this.dragging = pair.Key;
                }
                else
                {
                    float y = 132f;
                    foreach (DialogOption option in pair.Key.options)
                    {
                        if (mouse.y >= pair.Value.y + y * this.zoom && mouse.y < pair.Value.y + (y + 30f) * this.zoom)
                        {
                            this.editor.SelectOption(pair.Key, option);
                        }
                        y += 30f;
                        foreach (DialogResult result in option.results)
                        {
                            if (mouse.y >= pair.Value.y + y * this.zoom && mouse.y < pair.Value.y + (y + 25f) * this.zoom)
                            {
                                this.editor.SelectResult(pair.Key, option, result);
                            }
                            y += 25f;
                        }
                        if (option.results.Count == 0)
                        {
                            y += 25f;
                        }
                    }
                }
                input.Use();
                return;
            }
            if (input.button == 1)
            {
                Vector2 position = (mouse - this.pan) / this.zoom;
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption("AddNewNode".Translate(), () => this.CreateNodeAt(position))
                }));
            }
            this.panning = input.button == 0 || input.button == 2;
            input.Use();
        }

        private void CreateNodeAt(Vector2 position)
        {
            DialogNode node = this.Tree.CreateNewNode(null);
            node.editorX = position.x;
            node.editorY = position.y;
            node.editorPositionSet = true;
            this.SelectedIndex = node.index;
            this.editor.SelectNode(node, false);
            this.editor.InitCurTree();
        }

        private void HandleViewInput(Rect view)
        {
            UnityEvent input = UnityEvent.current;
            if (!view.Contains(input.mousePosition))
            {
                if (input.type == EventType.MouseUp) this.panning = false;
                return;
            }
            if (input.type == EventType.ScrollWheel)
            {
                Vector2 world = (input.mousePosition - this.pan) / this.zoom;
                this.zoom = Mathf.Clamp(this.zoom * Mathf.Pow(1.1f, -input.delta.y), 0.25f, 1.6f);
                this.pan = input.mousePosition - world * this.zoom;
                input.Use();
            }
            else if (input.type == EventType.MouseDown)
            {
                this.panning = true;
                input.Use();
            }
            else if (input.type == EventType.MouseDrag && this.panning)
            {
                this.pan += input.delta;
                input.Use();
            }
            else if (input.type == EventType.MouseUp)
            {
                this.panning = false;
                input.Use();
            }
        }

        private const float CardWidth = 290f;
        private readonly QuestEditor_Dialog editor;
        private DialogTreeDef? previewTree;
        private DialogNode? dragging;
        private DialogNode? linkSource;
        private DialogResult? linking;
        private bool panning;
        private Vector2 pan = new Vector2(35f, 35f);
        private Vector2 viewSize = new Vector2(800f, 600f);
        private float zoom = 0.85f;
    }
}
