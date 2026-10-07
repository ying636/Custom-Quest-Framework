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
        public bool IsInteracting => this.dragging != null || this.linkNode != null || this.linkResult != null;

        private void HandleInput(Rect view, Dictionary<DialogNode, Rect> nodes, Dictionary<DialogOption, Rect> options, bool editable)
        {
            if (Find.WindowStack.MouseObscuredNow) return;
            UnityEvent input = UnityEvent.current;
            Vector2 mouse = input.mousePosition;
            bool blocked = this.InputBlockedRect?.Contains(mouse) == true;
            if (blocked && input.type is EventType.MouseDown or EventType.ScrollWheel) return;
            float controlsWidth = this.InputBlockedRect.HasValue ? Mathf.Max(0f, this.InputBlockedRect.Value.x) : view.width;
            if (controlsWidth >= 190f && new Rect(controlsWidth - 190f, view.height - 40f, 190f, 40f).Contains(mouse)
                && input.type is EventType.MouseDown or EventType.ScrollWheel) return;
            if (input.type == EventType.KeyDown && input.keyCode == KeyCode.Escape && this.IsInteracting)
            {
                this.linkNode = null; this.linkReference = null; this.linkOption = null; this.linkResult = null; this.dragging = null;
                input.Use(); return;
            }
            if (input.type == EventType.ScrollWheel && view.Contains(mouse))
            { this.ZoomAt(mouse, Mathf.Pow(1.1f, -input.delta.y)); input.Use(); return; }
            if (input.type == EventType.MouseDrag)
            {
                Vector2 delta = input.delta / this.zoom;
                if (editable && this.dragging is DialogNode node) { node.editorX += delta.x; node.editorY += delta.y; input.Use(); }
                else if (editable && this.dragging is DialogOption option) { option.editorX += delta.x; option.editorY += delta.y; input.Use(); }
                else if (this.panning) { this.pan += input.delta; input.Use(); }
                else if (this.IsInteracting) input.Use();
                return;
            }
            if (input.type == EventType.MouseUp)
            {
                bool changed = this.dragging != null;
                if (editable && !blocked && view.Contains(mouse) && this.linkNode != null)
                {
                    DialogOption? target = options.Reverse().FirstOrDefault(pair => pair.Value.ExpandedBy(8f).Contains(mouse)).Key;
                    if (target != null)
                    {
                        if (this.linkReference == null) { this.Tree.LinkOption(this.linkNode, target); changed = true; }
                        else if (!this.linkNode.options.Contains(target))
                        {
                            this.linkNode.options[this.linkNode.options.IndexOf(this.linkReference)] = target;
                            this.Tree.Update(); changed = true;
                        }
                    }
                }
                if (editable && !blocked && view.Contains(mouse) && this.linkResult != null)
                {
                    DialogNode? target = nodes.Reverse().FirstOrDefault(pair => pair.Value.ExpandedBy(8f).Contains(mouse)).Key;
                    if (target != null) { this.linkResult.nextIndex = target.index; this.Tree.Update(); changed = true; }
                }
                bool used = this.IsInteracting || this.panning;
                this.linkNode = null; this.linkReference = null; this.linkOption = null; this.linkResult = null; this.dragging = null; this.panning = false;
                if (changed) this.editor.RecordChanges();
                if (used) input.Use();
                return;
            }
            if (input.type != EventType.MouseDown || !view.Contains(mouse)) return;
            if (!editable || input.button == 2) { this.panning = true; input.Use(); return; }
            foreach (var pair in options.Reverse())
            {
                for (int i = 0; i < pair.Key.results.Count; i++)
                {
                    if (!this.PortRect(this.OptionPort(pair.Key, pair.Value, i)).Contains(mouse)) continue;
                    DialogResult result = pair.Key.results[i];
                    this.SelectedOption = pair.Key;
                    this.editor.SelectResult(null, pair.Key, result, input.clickCount >= 2);
                    if (input.button == 0) { this.linkOption = pair.Key; this.linkResult = result; }
                    else if (input.button == 1) this.editor.ShowOptionMenu(pair.Key, null, result);
                    input.Use(); return;
                }
                if (!pair.Value.ExpandedBy(8f).Contains(mouse)) continue;
                this.SelectedOption = pair.Key;
                this.SelectedIndex = null;
                this.editor.SelectOption(null, pair.Key, input.clickCount >= 2 && input.button == 0);
                if (input.button == 1) this.editor.ShowOptionMenu(pair.Key);
                else
                {
                    int row = this.CardLayout(pair.Key, pair.Value).RowAt(mouse);
                    if (row >= 0 && row < pair.Key.results.Count) this.editor.SelectResult(null, pair.Key, pair.Key.results[row], input.clickCount >= 2);
                    else if (input.clickCount < 2) this.dragging = pair.Key;
                }
                input.Use(); return;
            }
            foreach (var pair in nodes.Reverse())
            {
                if (this.PortRect(this.NodePort(pair.Key, pair.Value, -1)).Contains(mouse))
                {
                    this.SelectedIndex = pair.Key.index; this.SelectedOption = null;
                    this.editor.SelectNode(pair.Key, false);
                    if (input.button == 0) this.linkNode = pair.Key;
                    else if (input.button == 1) this.ShowNodeMenu(pair.Key);
                    input.Use(); return;
                }
                for (int i = 0; i < pair.Key.options.Count; i++)
                {
                    Vector2 point = this.NodePort(pair.Key, pair.Value, i);
                    Rect row = this.CardLayout(pair.Key, pair.Value).Row(i);
                    row.xMin = pair.Value.x;
                    row.xMax = pair.Value.xMax + 10f;
                    if (!row.Contains(mouse)) continue;
                    DialogOption option = pair.Key.options[i];
                    this.SelectedIndex = pair.Key.index; this.SelectedOption = option;
                    this.editor.SelectOption(pair.Key, option, input.clickCount >= 2 && input.button == 0);
                    if (input.button == 1) this.editor.ShowOptionMenu(option, pair.Key);
                    else if (this.PortRect(point).Contains(mouse)) { this.linkNode = pair.Key; this.linkReference = option; }
                    input.Use(); return;
                }
                if (!pair.Value.ExpandedBy(8f).Contains(mouse)) continue;
                this.SelectedIndex = pair.Key.index; this.SelectedOption = null;
                this.editor.SelectNode(pair.Key, input.clickCount >= 2 && input.button == 0);
                if (input.button == 1) this.ShowNodeMenu(pair.Key);
                else if (input.clickCount < 2) this.dragging = pair.Key;
                input.Use(); return;
            }
            if (input.button == 1)
            {
                Vector2 position = this.viewport.ToWorld(mouse);
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption("AddNewNode".Translate(), () => this.CreateNodeAt(position)),
                    new FloatMenuOption("CQF_DialogGraph_AddOption".Translate(), () => this.editor.CreateOption(null, position)),
                    new FloatMenuOption("AddSpecialOption".Translate(), () => this.editor.AddSpecialOption(null, position))
                }));
            }
            else this.panning = true;
            input.Use();
        }

        private void ShowNodeMenu(DialogNode node)
        {
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_DialogGraph_ShowInspector".Translate(), () => this.editor.SelectNode(node)),
                new FloatMenuOption("CQF_DialogGraph_AddOption".Translate(), () => this.editor.AddOption(node)),
                new FloatMenuOption("AddSpecialOption".Translate(), () => this.editor.AddSpecialOption(node, this.GetOptionPosition(node))),
                new FloatMenuOption("CQF_DialogGraph_LinkExisting".Translate(), () => Find.WindowStack.Add(new FloatMenu(
                    this.Tree.optionMoulds.Values.Where(option => !node.options.Contains(option)).Select(option => new FloatMenuOption(
                        this.Display(option.text).Replace('\n', ' '), () => { this.Tree.LinkOption(node, option); this.editor.RecordChanges(); })).ToList()))),
                new FloatMenuOption("Remove".Translate(), node.index == 0 ? null : () => this.editor.RemoveNode(node))
            }));
        }

        private void CreateNodeAt(Vector2 position)
        {
            DialogNode node = this.Tree.CreateNewNode(null);
            node.editorX = position.x; node.editorY = position.y; node.editorPositionSet = true;
            this.SelectedIndex = node.index; this.SelectedOption = null;
            this.editor.SelectNode(node, false);
            this.editor.InitCurTree();
        }
    }
}
