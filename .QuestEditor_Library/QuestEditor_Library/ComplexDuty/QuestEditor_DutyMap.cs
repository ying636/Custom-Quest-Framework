using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class QuestEditor_DutyMap : Page
    {
        public static DutyMapDef CurrentEditingDutyMap => QuestEditor_DutyMap.curDutyMap;

        public QuestEditor_DutyMap()
        {
            this.preventCameraMotion = false;
            this.absorbInputAroundWindow = false;
            this.doCloseX = true;
            if (QuestEditor_DutyMap.curDutyMap.nodes.NullOrEmpty())
            {
                QuestEditor_DutyMap.curDutyMap.CreateNode();
            }
        }

        public override string PageTitle => "CQF_DutyMapEditor".Translate().Colorize(ColorLibrary.SkyBlue);

        public DutyMapDef CurDutyMap => QuestEditor_DutyMap.curDutyMap;

        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            this.DrawButtons(inRect);
            CQFEditorTools.DrawLabelAndText_Line(45f, "CQF_DefName".Translate(), ref this.CurDutyMap.defName, 5f, 100f);
            CQFEditorTools.DrawLabelAndText_Line(45f, "CQF_DutyMapLabel".Translate(), ref this.CurDutyMap.label, 310f, 120f);

            Rect canvasRect = new Rect(5f, 85f, inRect.width - 10f, inRect.height - 95f);
            this.canvasSize = canvasRect.size;
            Rect viewRect = new Rect(0f, 0f, this.canvasSize.x, this.canvasSize.y);
            Widgets.DrawBox(canvasRect, 1, QuestEditor_Dialog.blueTex);
            GUI.BeginGroup(canvasRect);
            this.UpdateHoveredTransition(viewRect);
            this.DrawTransitions();
            this.DrawNodes();
            this.DrawTransitionCreationPreview();
            this.HandleCanvasInput(viewRect);
            GUI.EndGroup();
        }

        private void DrawButtons(Rect inRect)
        {
            float x = inRect.width - 450f;
            if (Widgets.ButtonText(new Rect(x, 30f, 100f, 38f), "LoadPremade".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DutyMapDef>.AllDefsListForReading, d =>
                {
                    QuestEditor_DutyMap.curDutyMap = d;
                    this.ClearTransitionSelection();
                    this.MarkTransitionLayoutDirty();
                }, d => d.defName);
            }
            x += 110f;
            if (Widgets.ButtonText(new Rect(x, 30f, 100f, 38f), "Save".Translate()))
            {
                this.Save();
            }
            x += 110f;
            if (Widgets.ButtonText(new Rect(x, 30f, 100f, 38f), "ResetBinding".Translate()))
            {
                QuestEditor_DutyMap.curDutyMap = new DutyMapDef();
                QuestEditor_DutyMap.curDutyMap.CreateNode();
                this.selectedNode = null;
                this.ClearTransitionSelection();
                this.MarkTransitionLayoutDirty();
            }
        }

        private void DrawNodes()
        {
            foreach (DutyMapNode node in this.CurDutyMap.nodes)
            {
                this.ClampNodePosition(node);
                Rect nodeRect = new Rect(node.editorPosition, QuestEditor_Dialog.nodeSize);
                Color oldColor = GUI.color;
                GUI.color = node.nodeId == this.CurDutyMap.startNodeId ? ColorLibrary.Yellow : Color.white;
                Widgets.DrawTextureFitted(nodeRect, QuestEditor_Dialog.nodeTexture, 1f);
                GUI.color = oldColor;
                this.DrawTransitionTargetHint(node, nodeRect);
                this.HandleNodeInput(node, nodeRect);
                TooltipHandler.TipRegion(nodeRect, node.nodeId + "\n" + (node.duty?.defName ?? "Null"));
                Widgets.Label(new Rect(node.editorPosition.x + 24f, node.editorPosition.y - 2f, 160f, 25f), node.nodeId);
            }
        }

        private void ClampNodePosition(DutyMapNode node)
        {
            node.editorPosition.x = Mathf.Clamp(node.editorPosition.x, 0f, Mathf.Max(0f, this.canvasSize.x - QuestEditor_Dialog.nodeSize.x));
            node.editorPosition.y = Mathf.Clamp(node.editorPosition.y, 0f, Mathf.Max(0f, this.canvasSize.y - QuestEditor_Dialog.nodeSize.y));
        }

        private void HandleNodeInput(DutyMapNode node, Rect nodeRect)
        {
            UnityEngine.Event ev = UnityEngine.Event.current;
            if (ev.type == EventType.MouseDown && nodeRect.Contains(ev.mousePosition))
            {
                if (this.transitionSourceNode != null && ev.button == 0)
                {
                    this.TryCreateTransitionTo(node);
                    ev.Use();
                    return;
                }
                if (ev.button == 0)
                {
                    this.draggingNode = node;
                    this.dragOffset = ev.mousePosition - node.editorPosition;
                    this.draggedNode = false;
                    this.selectedNode = node;
                    this.selectedTransition = null;
                    ev.Use();
                }
                else if (ev.button == 1)
                {
                    this.selectedNode = node;
                    this.selectedTransition = null;
                    this.OpenNodeMenu(node);
                    ev.Use();
                }
            }
            if (this.draggingNode == node && ev.type == EventType.MouseDrag && ev.button == 0)
            {
                node.editorPosition = ev.mousePosition - this.dragOffset;
                node.editorPosition.x = Mathf.Clamp(node.editorPosition.x, 0f, this.canvasSize.x - QuestEditor_Dialog.nodeSize.x);
                node.editorPosition.y = Mathf.Clamp(node.editorPosition.y, 0f, this.canvasSize.y - QuestEditor_Dialog.nodeSize.y);
                this.MarkTransitionLayoutDirty();
                this.draggedNode = true;
                ev.Use();
            }
            if (this.draggingNode == node && ev.type == EventType.MouseUp && ev.button == 0)
            {
                if (!this.draggedNode)
                {
                    Find.WindowStack.Add(new Dialog_EditDutyMapNode(node));
                }
                this.draggingNode = null;
                this.draggedNode = false;
                ev.Use();
            }
        }

        private void HandleCanvasInput(Rect viewRect)
        {
            UnityEngine.Event ev = UnityEngine.Event.current;
            if (ev.type != EventType.MouseDown || ev.button != 1 || !viewRect.Contains(ev.mousePosition))
            {
                return;
            }
            if (this.transitionSourceNode != null)
            {
                this.transitionSourceNode = null;
                ev.Use();
                return;
            }
            bool overNode = this.CurDutyMap.nodes.Any(node => new Rect(node.editorPosition, QuestEditor_Dialog.nodeSize).Contains(ev.mousePosition));
            if (overNode)
            {
                return;
            }
            Vector2 position = ev.mousePosition;
            ev.Use();
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("AddNewNode".Translate(), () =>
                {
                    this.selectedNode = this.CurDutyMap.CreateNode(position);
                    this.selectedTransition = null;
                })
            }));
        }

        private void OpenNodeMenu(DutyMapNode node)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("Delete".Translate(), () =>
                {
                    this.CurDutyMap.nodes.Remove(node);
                    this.CurDutyMap.transitions.RemoveAll(t => t.fromNodeId == node.nodeId || t.toNodeId == node.nodeId);
                    if (this.CurDutyMap.startNodeId == node.nodeId)
                    {
                        this.CurDutyMap.startNodeId = this.CurDutyMap.StartNode?.nodeId;
                    }
                    if (this.selectedNode == node)
                    {
                        this.selectedNode = null;
                    }
                    this.ClearTransitionSelection();
                    this.MarkTransitionLayoutDirty();
                }),
                new FloatMenuOption("CQF_EditDutyMapNode".Translate(), () => Find.WindowStack.Add(new Dialog_EditDutyMapNode(node))),
                new FloatMenuOption("CQF_StartCreateDutyMapTransition".Translate(), () => this.StartTransitionCreation(node))
            };
            if (this.CurDutyMap.startNodeId != node.nodeId)
            {
                options.Insert(1, new FloatMenuOption("CQF_SetStartNode".Translate(), () => this.CurDutyMap.startNodeId = node.nodeId));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void StartTransitionCreation(DutyMapNode node)
        {
            this.transitionSourceNode = node;
            this.selectedNode = node;
            this.ClearTransitionSelection();
        }

        private void TryCreateTransitionTo(DutyMapNode target)
        {
            if (this.transitionSourceNode == null)
            {
                return;
            }
            DutyMapNode source = this.transitionSourceNode;
            if (target == source)
            {
                this.transitionSourceNode = null;
                return;
            }
            DutyMapTransition existing = this.CurDutyMap.transitions.FirstOrDefault(t => t.fromNodeId == source.nodeId && t.toNodeId == target.nodeId);
            if (existing != null)
            {
                this.selectedNode = null;
                this.transitionSourceNode = null;
                this.ClearTransitionSelection();
                return;
            }
            DutyMapTransition transition = new DutyMapTransition
            {
                fromNodeId = source.nodeId,
                toNodeId = target.nodeId
            };
            this.CurDutyMap.transitions.Add(transition);
            this.selectedNode = null;
            this.transitionSourceNode = null;
            this.ClearTransitionSelection();
            this.MarkTransitionLayoutDirty();
        }

        private void DrawTransitions()
        {
            this.EnsureTransitionLayout();
            foreach (TransitionHitRecord record in this.transitionLayout)
            {
                bool isHovered = this.hoveredTransition == record.Transition;
                Color color = isHovered ? ColorLibrary.Yellow : ColorLibrary.SkyBlue;
                Widgets.DrawLine(record.From, record.To, color, isHovered ? 2f : 1f);
                this.HandleTransitionInput(record.Transition, isHovered);
            }
        }

        private void HandleTransitionInput(DutyMapTransition transition, bool mouseOver)
        {
            UnityEngine.Event ev = UnityEngine.Event.current;
            if (!mouseOver)
            {
                return;
            }
            TooltipHandler.TipRegion(new Rect(ev.mousePosition.x - 8f, ev.mousePosition.y - 8f, 16f, 16f), transition.fromNodeId + " -> " + transition.toNodeId);
            if (ev.type != EventType.MouseDown)
            {
                return;
            }
            if (ev.button == 0)
            {
                this.selectedTransition = transition;
                this.selectedNode = null;
                Find.WindowStack.Add(new Dialog_EditDutyMapTransition(transition));
                ev.Use();
            }
            else if (ev.button == 1)
            {
                ev.Use();
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption("Delete".Translate(), () =>
                    {
                        this.CurDutyMap.transitions.Remove(transition);
                        this.ClearTransitionSelection();
                        this.MarkTransitionLayoutDirty();
                    })
                }));
            }
        }

        private void EnsureTransitionLayout()
        {
            if (!this.transitionLayoutDirty)
            {
                return;
            }
            this.transitionLayout.Clear();
            this.transitionGrid.Clear();
            List<DutyMapTransition> transitions = this.CurDutyMap.transitions.ToList();
            for (int i = 0; i < transitions.Count; i++)
            {
                DutyMapTransition transition = transitions[i];
                if (!this.TryGetTransitionLine(transition, transitions, i, out Vector2 fromPos, out Vector2 toPos))
                {
                    continue;
                }
                TransitionHitRecord record = new TransitionHitRecord(transition, fromPos, toPos);
                int recordIndex = this.transitionLayout.Count;
                this.transitionLayout.Add(record);
                this.AddTransitionToGrid(record.Bounds, recordIndex);
            }
            if (this.hoveredTransition != null && !this.CurDutyMap.transitions.Contains(this.hoveredTransition))
            {
                this.hoveredTransition = null;
            }
            if (this.selectedTransition != null && !this.CurDutyMap.transitions.Contains(this.selectedTransition))
            {
                this.selectedTransition = null;
            }
            this.transitionLayoutDirty = false;
        }

        private void MarkTransitionLayoutDirty()
        {
            this.transitionLayoutDirty = true;
        }

        private void ClearTransitionSelection()
        {
            this.selectedTransition = null;
            this.hoveredTransition = null;
        }

        private Vector2 GetTransitionOffset(List<DutyMapTransition> transitions, DutyMapTransition transition, int index, Vector2 fromPos, Vector2 toPos)
        {
            string groupKey = this.TransitionGroupKey(transition);
            List<int> group = new List<int>();
            for (int i = 0; i < transitions.Count; i++)
            {
                if (this.TransitionGroupKey(transitions[i]) == groupKey)
                {
                    group.Add(i);
                }
            }
            if (group.Count <= 1)
            {
                return Vector2.zero;
            }
            int order = group.IndexOf(index);
            float center = (group.Count - 1) / 2f;
            Vector2 direction = toPos - fromPos;
            if (direction.sqrMagnitude < 0.001f)
            {
                return Vector2.zero;
            }
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized;
            return normal * ((order - center) * 8f);
        }

        private string TransitionGroupKey(DutyMapTransition transition)
        {
            if (string.CompareOrdinal(transition.fromNodeId, transition.toNodeId) <= 0)
            {
                return transition.fromNodeId + "|" + transition.toNodeId;
            }
            return transition.toNodeId + "|" + transition.fromNodeId;
        }

        private void UpdateHoveredTransition(Rect viewRect)
        {
            UnityEngine.Event ev = UnityEngine.Event.current;
            if (this.draggingNode != null)
            {
                this.hoveredTransition = null;
                return;
            }
            if (ev.type == EventType.MouseLeaveWindow)
            {
                this.hoveredTransition = null;
                return;
            }
            if (!viewRect.Contains(ev.mousePosition))
            {
                this.hoveredTransition = null;
                return;
            }
            this.hoveredTransition = this.FindHoveredTransition(ev.mousePosition);
        }

        private DutyMapTransition FindHoveredTransition(Vector2 mousePosition)
        {
            if (this.IsMouseOverNode(mousePosition))
            {
                return null;
            }
            this.EnsureTransitionLayout();
            DutyMapTransition result = null;
            float bestDistance = 6f;
            int gridKey = this.TransitionGridKey(mousePosition);
            if (!this.transitionGrid.TryGetValue(gridKey, out List<int> candidates))
            {
                return null;
            }
            foreach (int candidate in candidates)
            {
                if (candidate < 0 || candidate >= this.transitionLayout.Count)
                {
                    continue;
                }
                TransitionHitRecord record = this.transitionLayout[candidate];
                float distance = this.DistanceToSegment(mousePosition, record.From, record.To);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    result = record.Transition;
                }
            }
            return result;
        }

        private void AddTransitionToGrid(Rect bounds, int transitionIndex)
        {
            int minX = Mathf.FloorToInt(bounds.xMin / TransitionGridSize);
            int maxX = Mathf.FloorToInt(bounds.xMax / TransitionGridSize);
            int minY = Mathf.FloorToInt(bounds.yMin / TransitionGridSize);
            int maxY = Mathf.FloorToInt(bounds.yMax / TransitionGridSize);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    int key = this.TransitionGridKey(x, y);
                    if (!this.transitionGrid.TryGetValue(key, out List<int> list))
                    {
                        list = new List<int>();
                        this.transitionGrid[key] = list;
                    }
                    list.Add(transitionIndex);
                }
            }
        }

        private int TransitionGridKey(Vector2 position)
        {
            return this.TransitionGridKey(Mathf.FloorToInt(position.x / TransitionGridSize), Mathf.FloorToInt(position.y / TransitionGridSize));
        }

        private int TransitionGridKey(int x, int y)
        {
            return (x * 73856093) ^ (y * 19349663);
        }

        private bool TryGetTransitionLine(DutyMapTransition transition, List<DutyMapTransition> transitions, int index, out Vector2 fromPos, out Vector2 toPos)
        {
            fromPos = Vector2.zero;
            toPos = Vector2.zero;
            DutyMapNode from = this.CurDutyMap.GetNode(transition.fromNodeId);
            DutyMapNode to = this.CurDutyMap.GetNode(transition.toNodeId);
            if (from == null || to == null)
            {
                return false;
            }
            Vector2 start = from.editorPosition + new Vector2(10f, 10f);
            Vector2 end = to.editorPosition + new Vector2(10f, 10f);
            Vector2 offset = this.GetTransitionOffset(transitions, transition, index, start, end);
            fromPos = start + offset;
            toPos = end + offset;
            return true;
        }

        private bool IsMouseOverNode(Vector2 mousePosition)
        {
            return this.CurDutyMap.nodes.Any(node => new Rect(node.editorPosition, QuestEditor_Dialog.nodeSize).Contains(mousePosition));
        }

        private float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            if (segment.sqrMagnitude < 0.001f)
            {
                return Vector2.Distance(point, start);
            }
            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
            Vector2 projection = start + segment * t;
            return Vector2.Distance(point, projection);
        }

        private void DrawTransitionTargetHint(DutyMapNode node, Rect nodeRect)
        {
            if (this.transitionSourceNode == null)
            {
                return;
            }
            bool source = node == this.transitionSourceNode;
            bool available = !source && this.CurDutyMap.transitions.All(t => t.fromNodeId != this.transitionSourceNode.nodeId || t.toNodeId != node.nodeId);
            if (!available && !source)
            {
                return;
            }
            Color color = source ? ColorLibrary.Yellow : ColorLibrary.SkyBlue;
            Widgets.DrawBox(nodeRect.ExpandedBy(4f), 2, BaseContent.WhiteTex);
            GUI.color = color;
            Widgets.DrawBox(nodeRect.ExpandedBy(3f));
            GUI.color = Color.white;
            string label = source ? "CQF_DutyMapTransitionSource".Translate() : "CQF_DutyMapTransitionTargetHint".Translate();
            Widgets.Label(new Rect(nodeRect.x, nodeRect.yMax + 2f, 150f, 24f), label.Colorize(color));
        }

        private void DrawTransitionCreationPreview()
        {
            if (this.transitionSourceNode == null)
            {
                return;
            }
            Vector2 fromPos = this.transitionSourceNode.editorPosition + new Vector2(10f, 10f);
            Widgets.DrawLine(fromPos, UnityEngine.Event.current.mousePosition, ColorLibrary.SkyBlue, 1f);
        }

        private void Save()
        {
            try
            {
                if (this.CurDutyMap.defName.NullOrEmpty())
                {
                    Messages.Message("NoName".Translate(), MessageTypeDefOf.CautionInput);
                    return;
                }
                string directory = System.IO.Path.Combine(Page_QuestEditor.Path, "Duty");
                System.IO.Directory.CreateDirectory(directory);
                string path = System.IO.Path.Combine(directory, this.CurDutyMap.defName + ".xml");
                XElement defs = new XElement("Defs");
                defs.Add(this.CurDutyMap.SaveToXElement("QuestEditor_Library.DutyMapDef"));
                defs.Save(path);
                CQFQuestDefBootstrap.HotLoadDutyMapDef(this.CurDutyMap);
                Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception e)
            {
                Log.Error("Save duty map error:" + e);
            }
        }

        private DutyMapNode selectedNode;
        private DutyMapTransition selectedTransition;
        private DutyMapTransition hoveredTransition;
        private DutyMapNode transitionSourceNode;
        private DutyMapNode draggingNode;
        private Vector2 dragOffset;
        private bool draggedNode;
        private bool transitionLayoutDirty = true;
        private Vector2 canvasSize = new Vector2(1800f, 1200f);
        private readonly List<TransitionHitRecord> transitionLayout = new List<TransitionHitRecord>();
        private readonly Dictionary<int, List<int>> transitionGrid = new Dictionary<int, List<int>>();
        private const float TransitionGridSize = 96f;
        private static DutyMapDef curDutyMap = new DutyMapDef();

        private readonly struct TransitionHitRecord
        {
            public TransitionHitRecord(DutyMapTransition transition, Vector2 from, Vector2 to)
            {
                this.Transition = transition;
                this.From = from;
                this.To = to;
                this.Bounds = new Rect(
                    Mathf.Min(from.x, to.x),
                    Mathf.Min(from.y, to.y),
                    Mathf.Abs(from.x - to.x),
                    Mathf.Abs(from.y - to.y)).ExpandedBy(8f);
            }

            public DutyMapTransition Transition { get; }

            public Vector2 From { get; }

            public Vector2 To { get; }

            public Rect Bounds { get; }
        }
    }
}
