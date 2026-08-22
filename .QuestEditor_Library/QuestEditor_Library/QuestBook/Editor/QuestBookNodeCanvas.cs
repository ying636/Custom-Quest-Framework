using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookNodeCanvas
    {
        public QuestBookStep SelectedStep { get; private set; }

        public Action<QuestBookStep> OpenStepEditor { get; set; }

        public Action<QuestBookStep> OpenStepInfo { get; set; }

        public void SelectStep(QuestBookStep step)
        {
            SelectedStep = step;
        }

        public void Draw(Rect rect, QuestBookDef book, QuestBookInstance instance, bool editable, QuestBookChapter chapter = null)
        {
            Text.Font = GameFont.Small;
            if (book == null)
            {
                return;
            }
            currentBook = book;
            Widgets.DrawBox(rect, 1, QuestEditor_Dialog.blueTex);
            GUI.BeginGroup(rect);
            Dictionary<QuestBookStep, Rect> nodeRects = BuildNodeRects(book, chapter);
            DrawLinks(nodeRects);
            if (linkingSource != null && nodeRects.TryGetValue(linkingSource, out Rect linkingSourceRect))
            {
                DrawLinkPreview(linkingSourceRect, UnityEngine.Event.current.mousePosition);
            }

            QuestBookStep hoveredStep = nodeRects.FirstOrDefault(item => item.Value.Contains(UnityEngine.Event.current.mousePosition)).Key;
            SelectedStep = hoveredStep;
            foreach (KeyValuePair<QuestBookStep, Rect> item in nodeRects)
            {
                if (item.Key != hoveredStep)
                {
                    DrawNode(item.Key, item.Value, instance, item.Key == linkingSource);
                }
            }
            if (hoveredStep != null && nodeRects.TryGetValue(hoveredStep, out Rect hoveredRect))
            {
                DrawNode(hoveredStep, hoveredRect, instance, true);
            }
            if (linkingSource != null)
            {
                Widgets.Label(new Rect(10f, 8f, rect.width - 20f, 28f), "CQF_QuestBook_LinkModeHint".Translate(linkingSource.Label).Colorize(ColorLibrary.Yellow));
            }
            HandleInput(new Rect(0f, 0f, rect.width, rect.height), book, nodeRects, editable, chapter);
            ClampPan(rect, nodeRects);
            GUI.EndGroup();
        }

        public void ResetView()
        {
            pan = Vector2.zero;
            zoom = 1f;
            SelectedStep = null;
        }

        private Dictionary<QuestBookStep, Rect> BuildNodeRects(QuestBookDef book, QuestBookChapter chapter)
        {
            Dictionary<QuestBookStep, Rect> result = new Dictionary<QuestBookStep, Rect>();
            IEnumerable<QuestBookChapter> chapters = chapter == null ? book.chapters : new[] { chapter };
            int chapterIndex = 0;
            foreach (QuestBookChapter currentChapter in chapters)
            {
                for (int stepIndex = 0; stepIndex < currentChapter.steps.Count; stepIndex++)
                {
                    QuestBookStep step = currentChapter.steps[stepIndex];
                    if (step.position == Vector2.zero)
                    {
                        step.position = new Vector2(30f + chapterIndex * 280f, 70f + stepIndex * 125f);
                    }
                    Vector2 position = ToScreen(step.position);
                    result[step] = new Rect(position, new Vector2(NodeSize * zoom, NodeSize * zoom));
                }
                chapterIndex++;
            }
            return result;
        }

        private void DrawLinks(Dictionary<QuestBookStep, Rect> nodeRects)
        {
            foreach (QuestBookStep source in nodeRects.Keys)
            {
                foreach (string nextId in source.nextStepIds)
                {
                    QuestBookStep target = nodeRects.Keys.FirstOrDefault(step => step.id == nextId);
                    if (target != null && nodeRects.TryGetValue(target, out Rect targetRect))
                    {
                        DrawArrowLine(nodeRects[source], targetRect);
                    }
                }
            }
        }

        private void ClampPan(Rect rect, Dictionary<QuestBookStep, Rect> nodeRects)
        {
            if (nodeRects.NullOrEmpty())
            {
                pan = Vector2.zero;
                return;
            }
            float minX = nodeRects.Values.Min(node => node.xMin);
            float maxX = nodeRects.Values.Max(node => node.xMax);
            float minY = nodeRects.Values.Min(node => node.yMin);
            float maxY = nodeRects.Values.Max(node => node.yMax);
            float minAllowedX = -ViewPanMargin;
            float maxAllowedX = rect.width + ViewPanMargin;
            float minAllowedY = -ViewPanMargin;
            float maxAllowedY = rect.height + ViewPanMargin;
            if (minX < minAllowedX)
            {
                pan.x += minAllowedX - minX;
            }
            if (maxX > maxAllowedX)
            {
                pan.x -= maxX - maxAllowedX;
            }
            if (minY < minAllowedY)
            {
                pan.y += minAllowedY - minY;
            }
            if (maxY > maxAllowedY)
            {
                pan.y -= maxY - maxAllowedY;
            }
        }

        private void DrawArrowLine(Rect sourceRect, Rect targetRect)
        {
            Vector2 delta = targetRect.center - sourceRect.center;
            if (delta.sqrMagnitude < 0.01f)
            {
                return;
            }
            Vector2 direction = delta.normalized;
            Vector2 start = sourceRect.center + direction * (NodeSize * zoom * 0.5f);
            Vector2 end = targetRect.center - direction * (NodeSize * zoom * 0.5f);
            DrawArrowPath(start, end);
        }

        private void DrawLinkPreview(Rect sourceRect, Vector2 mousePosition)
        {
            Vector2 delta = mousePosition - sourceRect.center;
            if (delta.sqrMagnitude < 0.01f)
            {
                return;
            }
            Vector2 direction = delta.normalized;
            Vector2 start = sourceRect.center + direction * (NodeSize * zoom * 0.5f);
            DrawArrowPath(start, mousePosition);
        }

        private void DrawArrowPath(Vector2 start, Vector2 end)
        {
            Vector2 delta = end - start;
            float distance = delta.magnitude;
            if (distance < 1f)
            {
                return;
            }
            Vector2 direction = delta / distance;
            float spacing = Mathf.Clamp(32f * zoom, 22f, 42f);
            float arrowWidth = Mathf.Clamp(24f * zoom, 18f, 30f);
            float arrowHeight = Mathf.Clamp(12f * zoom, 9f, 15f);
            int arrowCount = Mathf.Max(1, Mathf.CeilToInt(distance / spacing));
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (UnityEngine.Event.current.type != EventType.Repaint)
            {
                return;
            }
            Color oldColor = GUI.color;
            GUI.color = Color.white;
            for (int index = 1; index <= arrowCount; index++)
            {
                Vector2 center = Vector2.Lerp(start, end, (index - 0.5f) / arrowCount);
                Rect arrowRect = new Rect(center.x - arrowWidth * 0.5f, center.y - arrowHeight * 0.5f, arrowWidth, arrowHeight);
                Matrix4x4 rotation = Matrix4x4.TRS(arrowRect.center, Quaternion.Euler(0f, 0f, angle), Vector3.one)
                    * Matrix4x4.TRS(-arrowRect.center, Quaternion.identity, Vector3.one);
                GL.PushMatrix();
                GL.MultMatrix(rotation);
                GUI.DrawTexture(arrowRect, questBookArrow, ScaleMode.StretchToFill, true);
                GL.PopMatrix();
            }
            GUI.color = oldColor;
        }

        private void DrawNode(QuestBookStep step, Rect rect, QuestBookInstance instance, bool selected)
        {
            QuestBookStepState state = instance?.GetStepState(step.id);
            Color tint = Color.white;
            if (state?.status == QuestBookStepStatus.Active)
            {
                tint = ColorLibrary.SkyBlue;
            }
            else if (state?.status == QuestBookStepStatus.Completed)
            {
                tint = ColorLibrary.Green;
            }
            else if (state?.status == QuestBookStepStatus.Failed)
            {
                tint = ColorLibrary.RedReadable;
            }
            float imageSize = NodeSize * zoom;
            Rect imageRect = new Rect(rect.x, rect.y, imageSize, imageSize);
            Color oldColor = GUI.color;
            GUI.color = selected ? Color.white : tint;
            Widgets.DrawTextureFitted(imageRect, selected ? nodeFrameHighlight : nodeFrame, 1f);
            GUI.color = oldColor;
            DrawIcon(step, imageRect.ContractedBy(12f * zoom));
            TooltipHandler.TipRegion(imageRect, GetNodeTooltip(step, instance));
        }

        private string GetNodeTooltip(QuestBookStep step, QuestBookInstance instance)
        {
            QuestBookStepState state = instance?.GetStepState(step.id);
            string stateKey = state == null ? "CQF_QuestBook_State_Locked" : "CQF_QuestBook_State_" + state.status;
            string description = step.Description.NullOrEmpty() ? string.Empty : "\n" + step.Description;
            return step.Label + "\n" + "CQF_QuestBook_State".Translate(stateKey.Translate()) + description;
        }

        private void DrawIcon(QuestBookStep step, Rect rect)
        {
            if (step.iconThing != null)
            {
                Widgets.DefIcon(rect, step.iconThing);
                return;
            }
            Texture2D texture = step.iconPath.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(step.iconPath, false);
            if (texture != null)
            {
                Widgets.DrawTextureFitted(rect, texture, 1f);
                return;
            }
        }

        private void HandleInput(Rect rect, QuestBookDef book, Dictionary<QuestBookStep, Rect> nodeRects, bool editable, QuestBookChapter chapter)
        {
            UnityEngine.Event current = UnityEngine.Event.current;
            Vector2 mouse = current.mousePosition;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape && linkingSource != null)
            {
                linkingSource = null;
                Messages.Message("CQF_QuestBook_LinkCancelled".Translate(), MessageTypeDefOf.NeutralEvent);
                current.Use();
                return;
            }
            HandleKeyboardPan(rect, mouse, current);
            if (current.type == EventType.ScrollWheel && rect.Contains(mouse))
            {
                zoom = Mathf.Clamp(zoom - current.delta.y * 0.03f, 0.65f, 1.35f);
                current.Use();
                return;
            }
            if (current.type == EventType.MouseDown && current.button == 2 && rect.Contains(mouse))
            {
                draggingStep = null;
                pressedStep = null;
                panning = true;
                panningButton = current.button;
                panMoved = false;
                lastMouse = mouse;
                current.Use();
                return;
            }
            if (current.type == EventType.MouseDrag && panning)
            {
                Vector2 delta = mouse - lastMouse;
                if (delta.sqrMagnitude > 0f)
                {
                    pan += delta;
                    if (delta.sqrMagnitude >= DragThreshold * DragThreshold)
                    {
                        panMoved = true;
                    }
                }
                lastMouse = mouse;
                current.Use();
                return;
            }
            if (current.type == EventType.MouseDrag && current.button == 0 && draggingStep != null && editable)
            {
                Vector2 nextPosition = FromScreen(mouse - dragOffset);
                if (Vector2.Distance(nextPosition, dragStartPosition) > DragThreshold)
                {
                    draggedStep = true;
                }
                draggingStep.position = nextPosition;
                current.Use();
                return;
            }
            if (current.type == EventType.MouseUp && current.button == 0 && draggingStep != null)
            {
                QuestBookStep releasedStep = draggingStep;
                draggingStep = null;
                if (!draggedStep)
                {
                    if (linkingSource != null)
                    {
                        SelectLinkTarget(releasedStep);
                    }
                    else
                    {
                        if (editable)
                        {
                            OpenStepEditor?.Invoke(releasedStep);
                        }
                        else
                        {
                            OpenStepInfo?.Invoke(releasedStep);
                        }
                    }
                }
                draggedStep = false;
                current.Use();
                return;
            }
            if (current.type == EventType.MouseUp && panning && current.button == panningButton)
            {
                QuestBookStep releasedStep = pressedStep;
                bool openInfo = current.button == 0 && !panMoved && releasedStep != null;
                panning = false;
                panningButton = -1;
                pressedStep = null;
                panMoved = false;
                if (openInfo)
                {
                    OpenStepInfo?.Invoke(releasedStep);
                }
                current.Use();
                return;
            }
            if (current.type != EventType.MouseDown || !rect.Contains(mouse))
            {
                return;
            }
            QuestBookStep clicked = nodeRects.FirstOrDefault(item => item.Value.Contains(mouse)).Key;
            if (current.button == 0 && clicked != null)
            {
                SelectedStep = clicked;
                if (editable)
                {
                    draggingStep = clicked;
                    dragOffset = mouse - ToScreen(clicked.position);
                    dragStartPosition = clicked.position;
                    draggedStep = false;
                }
                else
                {
                    pressedStep = clicked;
                    panning = true;
                    panningButton = current.button;
                    panMoved = false;
                    lastMouse = mouse;
                }
                current.Use();
                return;
            }
            if (current.button == 0)
            {
                pressedStep = null;
                panning = true;
                panningButton = current.button;
                panMoved = false;
                lastMouse = mouse;
                current.Use();
                return;
            }
            if (current.button == 1)
            {
                SelectedStep = clicked;
                OpenContextMenu(book, clicked, chapter, mouse);
                current.Use();
            }
        }

        private void HandleKeyboardPan(Rect rect, Vector2 mouse, UnityEngine.Event current)
        {
            if (current.type != EventType.Repaint || !rect.Contains(mouse))
            {
                return;
            }
            Vector2 direction = Vector2.zero;
            if (Input.GetKey(KeyCode.W))
            {
                direction.y += 1f;
            }
            if (Input.GetKey(KeyCode.S))
            {
                direction.y -= 1f;
            }
            if (Input.GetKey(KeyCode.A))
            {
                direction.x += 1f;
            }
            if (Input.GetKey(KeyCode.D))
            {
                direction.x -= 1f;
            }
            if (direction == Vector2.zero)
            {
                keyboardPanning = false;
                lastKeyboardPanTime = 0f;
                return;
            }
            float now = Time.realtimeSinceStartup;
            if (!keyboardPanning)
            {
                pan += direction.normalized * KeyboardInitialMoveDistance;
                keyboardPanning = true;
                lastKeyboardPanTime = now;
                return;
            }
            float deltaTime = Mathf.Clamp(now - lastKeyboardPanTime, 0f, 0.05f);
            float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? FastKeyboardPanSpeed : KeyboardPanSpeed;
            pan += direction.normalized * speed * deltaTime;
            lastKeyboardPanTime = now;
        }

        private void OpenContextMenu(QuestBookDef book, QuestBookStep clicked, QuestBookChapter chapter, Vector2 mousePosition)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            Vector2 createPosition = FromScreen(mousePosition);
            if (clicked != null)
            {
                options.Add(new FloatMenuOption("CQF_QuestBook_EditNode".Translate(), () => OpenStepEditor?.Invoke(clicked)));
                options.Add(new FloatMenuOption("CQF_QuestBook_AddNextStep".Translate(), () => AddNextStep(book, clicked, createPosition)));
                List<QuestBookStep> linkTargets = (chapter?.steps ?? book.chapters.SelectMany(item => item.steps).ToList())
                    .Where(step => step != clicked && !clicked.nextStepIds.Contains(step.id)).ToList();
                if (linkTargets.Any())
                {
                    options.Add(new FloatMenuOption("CQF_QuestBook_LinkStep".Translate(), () => BeginLink(clicked)));
                }
                List<QuestBookStep> unlinkTargets = (chapter?.steps ?? book.chapters.SelectMany(item => item.steps).ToList())
                    .Where(step => clicked.nextStepIds.Contains(step.id)).ToList();
                if (unlinkTargets.Any())
                {
                    options.Add(new FloatMenuOption("CQF_QuestBook_UnlinkStep".Translate(), () => OpenUnlinkMenu(clicked, unlinkTargets)));
                }
                options.Add(new FloatMenuOption("CQF_QuestBook_Delete".Translate(), () => DeleteStep(book, clicked)));
            }
            else
            {
                if (chapter != null)
                {
                    string chapterLabel = chapter.Label.Replace("{0}", (book.chapters.IndexOf(chapter) + 1).ToString());
                    options.Add(new FloatMenuOption("CQF_QuestBook_AddStepTo".Translate(chapterLabel), () => AddStep(chapter, createPosition)));
                }
            }
            if (options.Any())
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void AddStep(QuestBookChapter chapter, Vector2 position)
        {
            chapter.steps.Add(new QuestBookStep
            {
                id = chapter.id + "_step_" + (chapter.steps.Count + 1),
                labelKey = "CQF_QuestBook_Step".Translate().ToString(),
                position = position
            });
        }

        private void AddNextStep(QuestBookDef book, QuestBookStep source, Vector2 position)
        {
            QuestBookChapter chapter = book.chapters.FirstOrDefault(item => item.steps.Contains(source));
            if (chapter == null)
            {
                Log.Error("CQF task book selected step is not in a chapter.");
                return;
            }
            QuestBookStep next = new QuestBookStep
            {
                id = source.id + "_next_" + (chapter.steps.Count + 1),
                labelKey = "CQF_QuestBook_Step".Translate().ToString(),
                position = position
            };
            chapter.steps.Add(next);
            source.nextStepIds.Add(next.id);
        }

        private void BeginLink(QuestBookStep source)
        {
            linkingSource = source;
            SelectedStep = source;
            Messages.Message("CQF_QuestBook_SelectLinkTarget".Translate(), MessageTypeDefOf.NeutralEvent);
        }

        private void SelectLinkTarget(QuestBookStep target)
        {
            if (linkingSource == null)
            {
                return;
            }
            if (target == linkingSource)
            {
                Messages.Message("CQF_QuestBook_CannotLinkSelf".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }
            if (target.id.NullOrEmpty())
            {
                Messages.Message("CQF_QuestBook_LinkTargetMissingId".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }
            if (!linkingSource.nextStepIds.Contains(target.id))
            {
                linkingSource.nextStepIds.Add(target.id);
            }
            Messages.Message("CQF_QuestBook_LinkCreated".Translate(linkingSource.Label, target.Label), MessageTypeDefOf.PositiveEvent);
            SelectedStep = target;
            linkingSource = null;
        }

        private void OpenUnlinkMenu(QuestBookStep source, List<QuestBookStep> targets)
        {
            List<FloatMenuOption> options = targets.Select(target => new FloatMenuOption(GetStepDisplayName(target), () => source.nextStepIds.Remove(target.id))).ToList();
            if (options.Any())
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private void DeleteStep(QuestBookDef book, QuestBookStep step)
        {
            foreach (QuestBookChapter chapter in book.chapters)
            {
                chapter.steps.Remove(step);
            }
            foreach (QuestBookStep other in book.chapters.SelectMany(chapter => chapter.steps))
            {
                other.nextStepIds.Remove(step.id);
            }
            SelectedStep = null;
        }

        private string GetStepDisplayName(QuestBookStep step)
        {
            QuestBookChapter chapter = currentBook?.chapters.FirstOrDefault(item => item.steps.Contains(step));
            string chapterName = chapter == null ? string.Empty : chapter.Label.Replace("{0}", (currentBook.chapters.IndexOf(chapter) + 1).ToString());
            string stepName = step.Label;
            return chapterName.NullOrEmpty() ? stepName + " [" + step.id + "]" : chapterName + " / " + stepName + " [" + step.id + "]";
        }

        private Vector2 ToScreen(Vector2 position)
        {
            return new Vector2(20f, 20f) + pan + position * zoom;
        }

        private Vector2 FromScreen(Vector2 position)
        {
            return (position - new Vector2(20f, 20f) - pan) / zoom;
        }

        private const float NodeSize = 76f;
        private const float KeyboardInitialMoveDistance = 40f;
        private const float KeyboardPanSpeed = 480f;
        private const float FastKeyboardPanSpeed = 1200f;
        private const float DragThreshold = 1f;
        private const float ViewPanMargin = 180f;
        private static readonly Texture2D nodeFrame = ContentFinder<Texture2D>.Get("UI/QuestBook/NodeFrame", true);
        private static readonly Texture2D nodeFrameHighlight = ContentFinder<Texture2D>.Get("UI/QuestBook/NodeFrameHighlight", true);
        private static readonly Texture2D questBookArrow = ContentFinder<Texture2D>.Get("UI/QuestBook/QuestBookArrow", true);
        private Vector2 pan = Vector2.zero;
        private Vector2 lastMouse;
        private QuestBookStep draggingStep;
        private Vector2 dragOffset;
        private Vector2 dragStartPosition;
        private float zoom = 1f;
        private bool panning;
        private bool draggedStep;
        private bool panMoved;
        private bool keyboardPanning;
        private int panningButton = -1;
        private float lastKeyboardPanTime;
        private QuestBookStep pressedStep;
        private QuestBookDef currentBook;
        private QuestBookStep linkingSource;
    }
}
