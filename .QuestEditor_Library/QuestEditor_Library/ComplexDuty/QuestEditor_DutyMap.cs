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
            this.DrawTransitions();
            this.DrawNodes();
            this.HandleCanvasInput(viewRect);
            GUI.EndGroup();
        }

        private void DrawButtons(Rect inRect)
        {
            float x = inRect.width - 450f;
            if (Widgets.ButtonText(new Rect(x, 30f, 100f, 38f), "LoadPremade".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DutyMapDef>.AllDefsListForReading, d => QuestEditor_DutyMap.curDutyMap = d, d => d.defName);
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
                this.selectedTransition = null;
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
                }),
                new FloatMenuOption("CQF_EditDutyMapNode".Translate(), () => Find.WindowStack.Add(new Dialog_EditDutyMapNode(node))),
                new FloatMenuOption("CQF_CustomDutyTransition".Translate(), () => this.OpenTransitionMenu(node))
            };
            if (this.CurDutyMap.startNodeId != node.nodeId)
            {
                options.Insert(1, new FloatMenuOption("CQF_SetStartNode".Translate(), () => this.CurDutyMap.startNodeId = node.nodeId));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void OpenTransitionMenu(DutyMapNode node)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (DutyMapNode target in this.CurDutyMap.nodes.Where(n => n != node))
            {
                DutyMapTransition existing = this.CurDutyMap.transitions.FirstOrDefault(t => t.fromNodeId == node.nodeId && t.toNodeId == target.nodeId);
                if (existing == null)
                {
                    options.Add(new FloatMenuOption("CQF_AddTransition".Translate() + $" {node.nodeId}->{target.nodeId}", () =>
                    {
                        DutyMapTransition transition = new DutyMapTransition
                        {
                            fromNodeId = node.nodeId,
                            toNodeId = target.nodeId
                        };
                        this.CurDutyMap.transitions.Add(transition);
                        this.selectedTransition = transition;
                        this.selectedNode = null;
                    }));
                }
                else
                {
                    options.Add(new FloatMenuOption("Delete".Translate() + $" {node.nodeId}->{target.nodeId}", () =>
                    {
                        this.CurDutyMap.transitions.Remove(existing);
                        if (this.selectedTransition == existing)
                        {
                            this.selectedTransition = null;
                        }
                    }));
                }
            }
            foreach (DutyMapTransition transition in this.CurDutyMap.transitions.Where(t => t.toNodeId == node.nodeId).ToList())
            {
                options.Add(new FloatMenuOption("Delete".Translate() + $" {transition.fromNodeId}->{node.nodeId}", () =>
                {
                    this.CurDutyMap.transitions.Remove(transition);
                    if (this.selectedTransition == transition)
                    {
                        this.selectedTransition = null;
                    }
                }));
            }
            if (!options.Any())
            {
                options.Add(new FloatMenuOption("CQF_DutyMapNoOptions".Translate(), null));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void DrawTransitions()
        {
            foreach (DutyMapTransition transition in this.CurDutyMap.transitions)
            {
                DutyMapNode from = this.CurDutyMap.GetNode(transition.fromNodeId);
                DutyMapNode to = this.CurDutyMap.GetNode(transition.toNodeId);
                if (from == null || to == null)
                {
                    continue;
                }
                Vector2 fromPos = from.editorPosition + new Vector2(10f, 10f);
                Vector2 toPos = to.editorPosition + new Vector2(10f, 10f);
                Widgets.DrawLine(fromPos, toPos, ColorLibrary.SkyBlue, 1f);
                Rect hitRect = new Rect((fromPos + toPos) / 2f - new Vector2(10f, 10f), QuestEditor_Dialog.nodeSize);
                if (Widgets.ButtonImage(hitRect, QuestEditor_Dialog.optionTexture))
                {
                    this.selectedTransition = transition;
                    this.selectedNode = null;
                    Find.WindowStack.Add(new Dialog_EditDutyMapTransition(transition));
                }
            }
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
                string directory = Page_QuestEditor.Path + @"\Duty";
                System.IO.Directory.CreateDirectory(directory);
                string path = directory + @"\" + this.CurDutyMap.defName + ".xml";
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
        private DutyMapNode draggingNode;
        private Vector2 dragOffset;
        private bool draggedNode;
        private Vector2 canvasSize = new Vector2(1800f, 1200f);
        private static DutyMapDef curDutyMap = new DutyMapDef();
    }
}
