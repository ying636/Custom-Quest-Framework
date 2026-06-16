using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class QuestEditor_DutyDefEditor : Page
    {
        public QuestEditor_DutyDefEditor()
        {
            this.preventCameraMotion = false;
            this.absorbInputAroundWindow = false;
            this.doCloseX = true;
            this.EnsureDefaultThinkNodes();
        }

        public override string PageTitle => "CQF_DutyDefEditor".Translate().Colorize(ColorLibrary.SkyBlue);

        private DutyDef CurDef => QuestEditor_DutyDefEditor.curDef;

        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            this.DrawButtons(inRect);
            float y = 45f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_DefName".Translate(), ref this.CurDef.defName, 5f, 100f);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_DutyLabel".Translate(), ref this.CurDef.label, 310f, 120f);
            y += 34f;
            Rect outRect = new Rect(5f, y, inRect.width - 10f, inRect.height - y - 8f);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 30f, this.height);
            float contentY = 4f;
            Widgets.BeginScrollView(outRect, ref this.scrollPos, viewRect);
            this.DrawDuty(ref contentY, viewRect);
            this.DrawDraggingNodePreview();
            Widgets.EndScrollView();
            this.height = contentY + 40f;
            if (UnityEngine.Event.current.type == EventType.MouseUp && this.draggingNode != null)
            {
                this.ClearDraggingNode();
            }
        }

        private void DrawButtons(Rect inRect)
        {
            float x = inRect.width - 450f;
            if (Widgets.ButtonText(new Rect(x, 30f, 100f, 38f), "LoadPremade".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DutyDef>.AllDefsListForReading, def =>
                {
                    QuestEditor_DutyDefEditor.curDef = this.CopyDutyDef(def);
                    this.EnsureDefaultThinkNodes();
                    this.fieldBuffers.Clear();
                }, def => def.defName);
            }
            x += 110f;
            if (Widgets.ButtonText(new Rect(x, 30f, 100f, 38f), "Save".Translate()))
            {
                this.Save();
            }
            x += 110f;
            if (Widgets.ButtonText(new Rect(x, 30f, 100f, 38f), "ResetBinding".Translate()))
            {
                QuestEditor_DutyDefEditor.curDef = new DutyDef();
                this.EnsureDefaultThinkNodes();
                this.fieldBuffers.Clear();
            }
            x += 110f;
            if (Widgets.ButtonText(new Rect(x, 30f, 100f, 38f), "Misc".Translate()))
            {
                Find.WindowStack.Add(new Dialog_DutyDefMisc(this.CurDef));
            }
        }

        private void EnsureDefaultThinkNodes()
        {
            if (this.CurDef.thinkNode == null)
            {
                this.CurDef.thinkNode = new ThinkNode_Priority();
            }
            if (this.CurDef.constantThinkNode == null)
            {
                this.CurDef.constantThinkNode = new ThinkNode_Priority();
            }
        }

        private void DrawDuty(ref float y, Rect inRect)
        {
            this.DrawThinkNodeRoot(ref y, inRect, "CQF_DutyThinkNode".Translate(), "CQF_DutyThinkNode_Tip".Translate(), () => this.CurDef.thinkNode, value => this.CurDef.thinkNode = value, "thinkNode");
            y += 24f;
            this.DrawThinkNodeRoot(ref y, inRect, "CQF_DutyConstantThinkNode".Translate(), "CQF_DutyConstantThinkNode_Tip".Translate(), () => this.CurDef.constantThinkNode, value => this.CurDef.constantThinkNode = value, "constantThinkNode");
        }

        private void DrawThinkNodeRoot(ref float y, Rect inRect, string title, string tip, Func<ThinkNode> getNode, Action<ThinkNode> setNode, string key)
        {
            Rect titleRect = new Rect(5f, y, 600f, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, title.Colorize(ColorLibrary.PaleBlue));
            Text.Font = GameFont.Small;
            TooltipHandler.TipRegion(titleRect, tip);
            y += 38f;
            ThinkNode node = getNode();
            if (node == null)
            {
                if (Widgets.ButtonText(new Rect(25f, y, 180f, 32f), "CQF_DutySelectThinkNode".Translate(), false))
                {
                    this.OpenThinkNodeTypeSelect(type => setNode(this.MakeThinkNode(type)));
                }
                y += 42f;
                return;
            }
            this.DrawThinkNode(ref y, inRect, node, setNode, null, -1, key, 0);
        }

        private void DrawThinkNode(ref float y, Rect inRect, ThinkNode node, Action<ThinkNode> setNode, List<ThinkNode> parentList, int index, string key, int depth)
        {
            float x = 16f + depth * 24f;
            float width = inRect.width - x - 35f;
            Rect boxRect = new Rect(x, y, width, this.nodeHeights.TryGetValue(this.GetNodeHeightKey(key), out float nodeHeight) ? nodeHeight : 54f);
            Color cardColor = depth % 2 == 0 ? new Color(0.09f, 0.11f, 0.13f, 0.72f) : new Color(0.11f, 0.13f, 0.15f, 0.72f);
            Widgets.DrawBoxSolid(boxRect, cardColor);
            Widgets.DrawBoxSolid(new Rect(boxRect.x, boxRect.y, 4f, boxRect.height), this.NodeAccentColor(depth));
            Widgets.DrawHighlightIfMouseover(boxRect);

            Rect header = new Rect(x + 12f, y + 8f, width - 24f, 34f);
            Widgets.DrawBoxSolid(new Rect(header.x, header.y, header.width, header.height), new Color(0.16f, 0.18f, 0.2f, 0.68f));
            string foldKey = key + ".fold";
            bool foldout = !this.foldouts.Contains(foldKey);
            Rect foldRect = new Rect(header.x + 4f, header.y + 3f, 28f, 28f);
            if (Widgets.ButtonText(foldRect, foldout ? "-" : "+", false))
            {
                if (foldout)
                {
                    this.foldouts.Add(foldKey);
                }
                else
                {
                    this.foldouts.Remove(foldKey);
                }
            }
            Rect dragRect = new Rect(header.x + 40f, header.y + 4f, Mathf.Max(180f, header.width - 250f), 26f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(dragRect, this.ThinkNodeLabel(node).Colorize(new Color(0.78f, 0.86f, 1f)));
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(dragRect, "CQF_DutyDragNodeTip".Translate());
            this.HandleNodeDrag(node, parentList, index, dragRect);
            Rect changeRect = new Rect(header.xMax - (parentList == null ? 96f : 174f), header.y + 4f, 88f, 26f);
            if (Widgets.ButtonText(changeRect, "CQF_DutyChangeNode".Translate(), false))
            {
                ThinkNode capturedNode = node;
                this.OpenThinkNodeTypeSelect(type => setNode(this.ReplaceThinkNode(capturedNode, type)));
            }
            if (parentList != null && Widgets.ButtonText(new Rect(header.xMax - 78f, header.y + 4f, 66f, 26f), "Delete".Translate(), false))
            {
                parentList.RemoveAt(index);
                return;
            }
            y += 50f;

            if (!foldout)
            {
                this.nodeHeights[this.GetNodeHeightKey(key)] = 54f;
                return;
            }

            float startY = y - 50f;
            float contentX = x + 18f;
            float contentWidth = width - 34f;
            this.DrawThinkNodeFields(ref y, node, key, contentX, contentWidth);
            if (node != null)
            {
                Rect childHeader = new Rect(contentX, y + 4f, contentWidth, 32f);
                Widgets.DrawBoxSolid(childHeader, new Color(0.08f, 0.12f, 0.15f, 0.56f));
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(childHeader.x + 10f, childHeader.y, childHeader.width - 132f, childHeader.height), "CQF_DutySubNodes".Translate().Colorize(ColorLibrary.PaleBlue));
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.ButtonText(new Rect(childHeader.xMax - 118f, childHeader.y + 3f, 108f, 26f), "CQF_DutyAddSubNode".Translate(), false))
                {
                    ThinkNode capturedNode = node;
                    this.OpenThinkNodeTypeSelect(type =>
                    {
                        ThinkNode child = this.MakeThinkNode(type);
                        capturedNode.subNodes.Add(child);
                        child.parent = capturedNode;
                    });
                }
                this.HandleNodeDrop(node, childHeader);
                y += 46f;
                float childStartY = y;
                if (node.subNodes.Any())
                {
                    Widgets.DrawBoxSolid(new Rect(contentX + 8f, childStartY - 3f, 2f, Mathf.Max(34f, this.EstimateChildrenHeight(node, key))), this.NodeAccentColor(depth + 1));
                }
                for (int i = 0; i < node.subNodes.Count; i++)
                {
                    int capturedIndex = i;
                    ThinkNode child = node.subNodes[capturedIndex];
                    this.DrawThinkNode(ref y, inRect, child, value => node.subNodes[capturedIndex] = value, node.subNodes, capturedIndex, key + "." + capturedIndex, depth + 1);
                    y += 8f;
                }
                if (!node.subNodes.Any())
                {
                    Rect emptyRect = new Rect(contentX + 10f, y, contentWidth - 20f, 34f);
                    Widgets.DrawBoxSolid(emptyRect, new Color(0.09f, 0.1f, 0.11f, 0.52f));
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(new Rect(emptyRect.x + 10f, emptyRect.y, emptyRect.width - 20f, emptyRect.height), "CQF_DutyNoSubNodes".Translate().Colorize(Color.gray));
                    Text.Anchor = TextAnchor.UpperLeft;
                    this.HandleNodeDrop(node, emptyRect);
                    y += 44f;
                }
            }
            this.nodeHeights[this.GetNodeHeightKey(key)] = y - startY + 16f;
        }

        private void HandleNodeDrag(ThinkNode node, List<ThinkNode> parentList, int index, Rect rect)
        {
            UnityEngine.Event ev = UnityEngine.Event.current;
            if (parentList == null || !rect.Contains(ev.mousePosition))
            {
                return;
            }
            if (ev.type == EventType.MouseDown && ev.button == 0)
            {
                this.draggingNode = node;
                this.draggingSource = parentList;
                this.draggingIndex = index;
                ev.Use();
            }
        }

        private void HandleNodeDrop(ThinkNode targetNode, Rect rect)
        {
            UnityEngine.Event ev = UnityEngine.Event.current;
            if (this.draggingNode != null && targetNode != this.draggingNode && !this.IsNodeChildOf(targetNode, this.draggingNode))
            {
                if (rect.Contains(ev.mousePosition))
                {
                    Widgets.DrawBox(rect, 2, QuestEditor_Dialog.blueTex);
                }
            }
            if (this.draggingNode == null || ev.type != EventType.MouseUp || ev.button != 0 || !rect.Contains(ev.mousePosition))
            {
                return;
            }
            if (targetNode != this.draggingNode && !this.IsNodeChildOf(targetNode, this.draggingNode))
            {
                if (this.draggingSource != null && this.draggingIndex >= 0 && this.draggingIndex < this.draggingSource.Count)
                {
                    this.draggingSource.RemoveAt(this.draggingIndex);
                }
                targetNode.subNodes.Add(this.draggingNode);
                this.draggingNode.parent = targetNode;
            }
            this.ClearDraggingNode();
            ev.Use();
        }

        private bool IsNodeChildOf(ThinkNode node, ThinkNode possibleParent)
        {
            if (node == null || possibleParent == null || possibleParent.subNodes == null)
            {
                return false;
            }
            foreach (ThinkNode child in possibleParent.subNodes)
            {
                if (child == node || this.IsNodeChildOf(node, child))
                {
                    return true;
                }
            }
            return false;
        }

        private void ClearDraggingNode()
        {
            this.draggingNode = null;
            this.draggingSource = null;
            this.draggingIndex = -1;
        }

        private void DrawDraggingNodePreview()
        {
            if (this.draggingNode == null)
            {
                return;
            }
            Vector2 mousePosition = UnityEngine.Event.current.mousePosition;
            Rect previewRect = new Rect(mousePosition.x + 16f, mousePosition.y + 12f, 260f, 38f);
            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.86f);
            Widgets.DrawBoxSolid(previewRect, new Color(0.08f, 0.11f, 0.14f, 0.9f));
            Widgets.DrawBoxSolid(new Rect(previewRect.x, previewRect.y, 4f, previewRect.height), new Color(0.33f, 0.55f, 0.95f, 0.95f));
            Widgets.DrawBox(previewRect, 1, QuestEditor_Dialog.blueTex);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(previewRect.x + 14f, previewRect.y, previewRect.width - 22f, previewRect.height), this.ThinkNodeLabel(this.draggingNode).Colorize(new Color(0.78f, 0.86f, 1f)));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = oldColor;
        }

        private void DrawThinkNodeFields(ref float y, ThinkNode node, string key, float x, float width)
        {
            List<FieldInfo> fields = this.GetEditableFields(node.GetType()).ToList();
            if (!fields.Any())
            {
                return;
            }
            Rect fieldsRect = new Rect(x, y, width, fields.Count * 32f + 8f);
            Widgets.DrawBoxSolid(fieldsRect, new Color(0.06f, 0.07f, 0.08f, 0.34f));
            y += 4f;
            foreach (FieldInfo field in this.GetEditableFields(node.GetType()))
            {
                this.DrawField(ref y, node, field, key + "." + field.Name, x + 8f, width - 16f);
            }
            y += 10f;
        }

        private void DrawField(ref float y, object owner, FieldInfo field, string key, float x, float width)
        {
            string label = this.FieldLabel(field);
            this.DrawValue(ref y, label, field.FieldType, field.GetValue(owner), value => field.SetValue(owner, value), key, x, width);
        }

        private void DrawValue(ref float y, string label, Type fieldType, object value, Action<object> setValue, string key, float x, float width)
        {
            Rect rowRect = new Rect(x, y, width, 28f);
            Widgets.DrawHighlightIfMouseover(rowRect);
            Rect labelRect = new Rect(rowRect.x + 4f, rowRect.y + 4f, Mathf.Min(210f, rowRect.width * 0.38f), 24f);
            Rect controlRect = new Rect(labelRect.xMax + 12f, rowRect.y + 2f, rowRect.xMax - labelRect.xMax - 16f, 24f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label.Colorize(new Color(0.78f, 0.8f, 0.82f)));
            Text.Anchor = TextAnchor.UpperLeft;
            Type nullableType = Nullable.GetUnderlyingType(fieldType);
            if (nullableType != null)
            {
                this.DrawNullableValue(ref y, label, fieldType, nullableType, value, setValue, key, x, width, controlRect);
                return;
            }
            if (fieldType == typeof(string))
            {
                string text = value as string ?? string.Empty;
                text = Widgets.TextField(controlRect, text);
                setValue(text);
                y += 32f;
                return;
            }
            if (fieldType == typeof(bool))
            {
                bool boolValue = value is bool b && b;
                Widgets.Checkbox(new Vector2(controlRect.x, controlRect.y + 2f), ref boolValue, 20f);
                setValue(boolValue);
                y += 32f;
                return;
            }
            if (fieldType == typeof(int))
            {
                int intValue = value is int i ? i : 0;
                string buffer = this.GetBuffer(key, intValue.ToString());
                buffer = Widgets.TextField(new Rect(controlRect.x, controlRect.y, Mathf.Min(120f, controlRect.width), controlRect.height), buffer);
                if (int.TryParse(buffer, out int parsed))
                {
                    intValue = parsed;
                }
                this.fieldBuffers[key] = buffer;
                setValue(intValue);
                y += 32f;
                return;
            }
            if (fieldType == typeof(long))
            {
                long longValue = value is long l ? l : 0L;
                string buffer = this.GetBuffer(key, longValue.ToString());
                buffer = Widgets.TextField(new Rect(controlRect.x, controlRect.y, Mathf.Min(140f, controlRect.width), controlRect.height), buffer);
                if (long.TryParse(buffer, out long parsed))
                {
                    longValue = parsed;
                }
                this.fieldBuffers[key] = buffer;
                setValue(longValue);
                y += 32f;
                return;
            }
            if (fieldType == typeof(float))
            {
                float floatValue = value is float f ? f : 0f;
                string buffer = this.GetBuffer(key, floatValue.ToString());
                buffer = Widgets.TextField(new Rect(controlRect.x, controlRect.y, Mathf.Min(120f, controlRect.width), controlRect.height), buffer);
                if (float.TryParse(buffer, out float parsed))
                {
                    floatValue = parsed;
                }
                this.fieldBuffers[key] = buffer;
                setValue(floatValue);
                y += 32f;
                return;
            }
            if (fieldType == typeof(double))
            {
                double doubleValue = value is double d ? d : 0d;
                string buffer = this.GetBuffer(key, doubleValue.ToString());
                buffer = Widgets.TextField(new Rect(controlRect.x, controlRect.y, Mathf.Min(120f, controlRect.width), controlRect.height), buffer);
                if (double.TryParse(buffer, out double parsed))
                {
                    doubleValue = parsed;
                }
                this.fieldBuffers[key] = buffer;
                setValue(doubleValue);
                y += 32f;
                return;
            }
            if (fieldType.IsEnum)
            {
                if (Widgets.ButtonText(controlRect, this.EnumLabel(value), false))
                {
                    List<object> values = Enum.GetValues(fieldType).Cast<object>().ToList();
                    CQFEditorTools.DrawFloatMenu(values, setValue, this.EnumLabel);
                }
                y += 32f;
                return;
            }
            if (fieldType == typeof(DutyDef))
            {
                DutyDef duty = value as DutyDef;
                if (Widgets.ButtonText(controlRect, CQFEditorTools.DutyLabel(duty) ?? "Null".Translate().ToString(), false))
                {
                    CQFEditorTools.OpenDutySelect(d => setValue(d));
                }
                y += 32f;
                return;
            }
            if (typeof(Def).IsAssignableFrom(fieldType))
            {
                Def defValue = value as Def;
                if (Widgets.ButtonText(controlRect, defValue?.defName ?? "Null".Translate().ToString(), false))
                {
                    this.OpenDefSelect(fieldType, setValue);
                }
                y += 32f;
                return;
            }
            if (fieldType == typeof(IntRange))
            {
                IntRange range = value is IntRange intRange ? intRange : new IntRange(0, 0);
                string minBuffer = this.GetBuffer(key + ".min", range.min.ToString());
                string maxBuffer = this.GetBuffer(key + ".max", range.max.ToString());
                Rect minRect = new Rect(controlRect.x, controlRect.y, 70f, controlRect.height);
                Rect maxRect = new Rect(minRect.xMax + 8f, controlRect.y, 70f, controlRect.height);
                minBuffer = Widgets.TextField(minRect, minBuffer);
                maxBuffer = Widgets.TextField(maxRect, maxBuffer);
                if (int.TryParse(minBuffer, out int min))
                {
                    range.min = min;
                }
                if (int.TryParse(maxBuffer, out int max))
                {
                    range.max = max;
                }
                this.fieldBuffers[key + ".min"] = minBuffer;
                this.fieldBuffers[key + ".max"] = maxBuffer;
                setValue(range);
                y += 32f;
                return;
            }
            if (fieldType == typeof(FloatRange))
            {
                FloatRange range = value is FloatRange floatRange ? floatRange : new FloatRange(0f, 0f);
                string minBuffer = this.GetBuffer(key + ".min", range.min.ToString());
                string maxBuffer = this.GetBuffer(key + ".max", range.max.ToString());
                Rect minRect = new Rect(controlRect.x, controlRect.y, 70f, controlRect.height);
                Rect maxRect = new Rect(minRect.xMax + 8f, controlRect.y, 70f, controlRect.height);
                minBuffer = Widgets.TextField(minRect, minBuffer);
                maxBuffer = Widgets.TextField(maxRect, maxBuffer);
                if (float.TryParse(minBuffer, out float min))
                {
                    range.min = min;
                }
                if (float.TryParse(maxBuffer, out float max))
                {
                    range.max = max;
                }
                this.fieldBuffers[key + ".min"] = minBuffer;
                this.fieldBuffers[key + ".max"] = maxBuffer;
                setValue(range);
                y += 32f;
                return;
            }
            if (fieldType == typeof(IntVec2))
            {
                IntVec2 vector = value is IntVec2 intVec2 ? intVec2 : IntVec2.Zero;
                this.DrawIntComponents(controlRect, key, vector.x, vector.z, (newX, newZ) => setValue(new IntVec2(newX, newZ)));
                y += 32f;
                return;
            }
            if (fieldType == typeof(IntVec3))
            {
                IntVec3 vector = value is IntVec3 intVec3 ? intVec3 : IntVec3.Zero;
                this.DrawIntComponents(controlRect, key, vector.x, vector.y, vector.z, (newX, newY, newZ) => setValue(new IntVec3(newX, newY, newZ)));
                y += 32f;
                return;
            }
            if (fieldType == typeof(Vector2))
            {
                Vector2 vector = value is Vector2 vector2 ? vector2 : Vector2.zero;
                this.DrawFloatComponents(controlRect, key, vector.x, vector.y, (newX, newY) => setValue(new Vector2(newX, newY)));
                y += 32f;
                return;
            }
            if (fieldType == typeof(Vector3))
            {
                Vector3 vector = value is Vector3 vector3 ? vector3 : Vector3.zero;
                this.DrawFloatComponents(controlRect, key, vector.x, vector.y, vector.z, (newX, newY, newZ) => setValue(new Vector3(newX, newY, newZ)));
                y += 32f;
                return;
            }
            if (fieldType == typeof(Rot4))
            {
                Rot4 rot = value is Rot4 rot4 ? rot4 : Rot4.North;
                if (Widgets.ButtonText(controlRect, rot.ToString(), false))
                {
                    CQFEditorTools.DrawFloatMenu(new List<Rot4> { Rot4.North, Rot4.East, Rot4.South, Rot4.West }, rotValue => setValue(rotValue), rotValue => rotValue.ToString());
                }
                y += 32f;
                return;
            }
            if (typeof(IList).IsAssignableFrom(fieldType) && fieldType.IsGenericType)
            {
                this.DrawListValue(ref y, label, fieldType, value, setValue, key, x, width);
                return;
            }
            if (this.CanEditNestedObject(fieldType, value))
            {
                this.DrawNestedObject(ref y, label, fieldType, value, setValue, key, x, width);
                return;
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(controlRect, "CQF_DutyUnsupportedField".Translate(label, fieldType.Name).Colorize(Color.gray));
            Text.Anchor = TextAnchor.UpperLeft;
            y += 32f;
        }

        private void DrawNullableValue(ref float y, string label, Type fieldType, Type nullableType, object value, Action<object> setValue, string key, float x, float width, Rect controlRect)
        {
            bool hasValue = value != null;
            Rect nullRect = new Rect(controlRect.x, controlRect.y + 2f, 24f, 20f);
            Widgets.Checkbox(nullRect.position, ref hasValue, 20f);
            Widgets.Label(new Rect(nullRect.xMax + 4f, controlRect.y + 3f, 70f, 22f), "Null".Translate());
            if (!hasValue)
            {
                setValue(null);
                y += 32f;
                return;
            }
            object innerValue = value ?? this.MakeDefaultValue(nullableType);
            this.DrawValue(ref y, label, nullableType, innerValue, inner => setValue(Activator.CreateInstance(fieldType, inner)), key + ".value", x, width);
        }

        private void DrawListValue(ref float y, string label, Type fieldType, object value, Action<object> setValue, string key, float x, float width)
        {
            IList list = value as IList;
            if (list == null)
            {
                list = (IList)Activator.CreateInstance(fieldType);
                setValue(list);
            }
            Type elementType = fieldType.GenericTypeArguments[0];
            Rect headerRect = new Rect(x, y, width, 28f);
            Widgets.DrawBoxSolid(headerRect, new Color(0.04f, 0.05f, 0.06f, 0.38f));
            Widgets.Label(new Rect(headerRect.x + 6f, headerRect.y + 4f, headerRect.width - 170f, 24f), label);
            if (Widgets.ButtonText(new Rect(headerRect.xMax - 152f, headerRect.y + 2f, 70f, 24f), "Add".Translate(), false))
            {
                list.Add(this.MakeDefaultValue(elementType));
            }
            if (list.Count > 0 && Widgets.ButtonText(new Rect(headerRect.xMax - 76f, headerRect.y + 2f, 70f, 24f), "Delete".Translate(), false))
            {
                list.RemoveAt(list.Count - 1);
            }
            y += 32f;
            for (int i = 0; i < list.Count; i++)
            {
                int index = i;
                object element = list[index];
                this.DrawValue(ref y, index.ToString(), elementType, element, newValue => list[index] = newValue, key + "." + index, x + 18f, width - 18f);
            }
            y += 4f;
        }

        private void DrawNestedObject(ref float y, string label, Type fieldType, object value, Action<object> setValue, string key, float x, float width)
        {
            if (value == null)
            {
                value = Activator.CreateInstance(fieldType);
                setValue(value);
            }
            bool open = this.foldouts.Contains(key);
            Rect headerRect = new Rect(x, y, width, 28f);
            if (Widgets.ButtonText(headerRect, (open ? "- " : "+ ") + label + " (" + fieldType.Name + ")", false))
            {
                if (open)
                {
                    this.foldouts.Remove(key);
                }
                else
                {
                    this.foldouts.Add(key);
                }
            }
            y += 32f;
            if (!this.foldouts.Contains(key))
            {
                return;
            }
            foreach (FieldInfo field in this.GetEditableFields(fieldType))
            {
                this.DrawField(ref y, value, field, key + "." + field.Name, x + 18f, width - 18f);
            }
        }

        private void DrawIntComponents(Rect rect, string key, int x, int z, Action<int, int> setValue)
        {
            string bufferX = this.GetBuffer(key + ".x", x.ToString());
            string bufferZ = this.GetBuffer(key + ".z", z.ToString());
            Rect xRect = new Rect(rect.x, rect.y, 54f, rect.height);
            Rect zRect = new Rect(xRect.xMax + 8f, rect.y, 54f, rect.height);
            bufferX = Widgets.TextField(xRect, bufferX);
            bufferZ = Widgets.TextField(zRect, bufferZ);
            if (int.TryParse(bufferX, out int newX) && int.TryParse(bufferZ, out int newZ))
            {
                setValue(newX, newZ);
            }
            this.fieldBuffers[key + ".x"] = bufferX;
            this.fieldBuffers[key + ".z"] = bufferZ;
        }

        private void DrawIntComponents(Rect rect, string key, int x, int y, int z, Action<int, int, int> setValue)
        {
            string bufferX = this.GetBuffer(key + ".x", x.ToString());
            string bufferY = this.GetBuffer(key + ".y", y.ToString());
            string bufferZ = this.GetBuffer(key + ".z", z.ToString());
            Rect xRect = new Rect(rect.x, rect.y, 54f, rect.height);
            Rect yRect = new Rect(xRect.xMax + 8f, rect.y, 54f, rect.height);
            Rect zRect = new Rect(yRect.xMax + 8f, rect.y, 54f, rect.height);
            bufferX = Widgets.TextField(xRect, bufferX);
            bufferY = Widgets.TextField(yRect, bufferY);
            bufferZ = Widgets.TextField(zRect, bufferZ);
            if (int.TryParse(bufferX, out int newX) && int.TryParse(bufferY, out int newY) && int.TryParse(bufferZ, out int newZ))
            {
                setValue(newX, newY, newZ);
            }
            this.fieldBuffers[key + ".x"] = bufferX;
            this.fieldBuffers[key + ".y"] = bufferY;
            this.fieldBuffers[key + ".z"] = bufferZ;
        }

        private void DrawFloatComponents(Rect rect, string key, float x, float y, Action<float, float> setValue)
        {
            string bufferX = this.GetBuffer(key + ".x", x.ToString());
            string bufferY = this.GetBuffer(key + ".y", y.ToString());
            Rect xRect = new Rect(rect.x, rect.y, 64f, rect.height);
            Rect yRect = new Rect(xRect.xMax + 8f, rect.y, 64f, rect.height);
            bufferX = Widgets.TextField(xRect, bufferX);
            bufferY = Widgets.TextField(yRect, bufferY);
            if (float.TryParse(bufferX, out float newX) && float.TryParse(bufferY, out float newY))
            {
                setValue(newX, newY);
            }
            this.fieldBuffers[key + ".x"] = bufferX;
            this.fieldBuffers[key + ".y"] = bufferY;
        }

        private void DrawFloatComponents(Rect rect, string key, float x, float y, float z, Action<float, float, float> setValue)
        {
            string bufferX = this.GetBuffer(key + ".x", x.ToString());
            string bufferY = this.GetBuffer(key + ".y", y.ToString());
            string bufferZ = this.GetBuffer(key + ".z", z.ToString());
            Rect xRect = new Rect(rect.x, rect.y, 64f, rect.height);
            Rect yRect = new Rect(xRect.xMax + 8f, rect.y, 64f, rect.height);
            Rect zRect = new Rect(yRect.xMax + 8f, rect.y, 64f, rect.height);
            bufferX = Widgets.TextField(xRect, bufferX);
            bufferY = Widgets.TextField(yRect, bufferY);
            bufferZ = Widgets.TextField(zRect, bufferZ);
            if (float.TryParse(bufferX, out float newX) && float.TryParse(bufferY, out float newY) && float.TryParse(bufferZ, out float newZ))
            {
                setValue(newX, newY, newZ);
            }
            this.fieldBuffers[key + ".x"] = bufferX;
            this.fieldBuffers[key + ".y"] = bufferY;
            this.fieldBuffers[key + ".z"] = bufferZ;
        }

        private object MakeDefaultValue(Type type)
        {
            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
            {
                return null;
            }
            if (type == typeof(string))
            {
                return string.Empty;
            }
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            if (typeof(Def).IsAssignableFrom(type))
            {
                return null;
            }
            return type.GetConstructor(Type.EmptyTypes) != null ? Activator.CreateInstance(type) : null;
        }

        private bool CanEditNestedObject(Type type, object value)
        {
            return value != null
                   && !type.IsPrimitive
                   && !type.IsEnum
                   && type != typeof(string)
                   && !typeof(Def).IsAssignableFrom(type)
                   && type.Namespace != null
                   && (type.Namespace.StartsWith("Verse") || type.Namespace.StartsWith("RimWorld") || type.Namespace.StartsWith("QuestEditor_Library"));
        }

        private Color NodeAccentColor(int depth)
        {
            switch (depth % 4)
            {
                case 0:
                    return new Color(0.33f, 0.55f, 0.95f, 0.9f);
                case 1:
                    return new Color(0.38f, 0.76f, 0.84f, 0.85f);
                case 2:
                    return new Color(0.6f, 0.72f, 0.42f, 0.85f);
                default:
                    return new Color(0.68f, 0.52f, 0.86f, 0.85f);
            }
        }

        private float EstimateChildrenHeight(ThinkNode node, string key)
        {
            if (node == null || node.subNodes == null || !node.subNodes.Any())
            {
                return 34f;
            }
            float result = 0f;
            for (int i = 0; i < node.subNodes.Count; i++)
            {
                result += this.nodeHeights.TryGetValue(this.GetNodeHeightKey(key + "." + i), out float height) ? height : 54f;
                result += 8f;
            }
            return result;
        }

        private string NullableBoolLabel(bool? value)
        {
            if (!value.HasValue)
            {
                return "Null".Translate();
            }
            return value.Value ? "True".Translate() : "False".Translate();
        }

        private void Save()
        {
            try
            {
                if (this.CurDef.defName.NullOrEmpty())
                {
                    Messages.Message("NoName".Translate(), MessageTypeDefOf.CautionInput);
                    return;
                }
                this.PrepareThinkNodeTree(this.CurDef.thinkNode, null);
                this.PrepareThinkNodeTree(this.CurDef.constantThinkNode, null);
                string directory = Page_QuestEditor.Path + @"\Duty";
                Directory.CreateDirectory(directory);
                string path = directory + @"\" + this.CurDef.defName + ".xml";
                XElement defs = new XElement("Defs");
                defs.Add(this.SaveToXElement());
                defs.Save(path);
                CQFQuestDefBootstrap.HotLoadDutyDef(this.CurDef);
                Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception e)
            {
                Log.Error("Save duty def error:" + e);
            }
        }

        private XElement SaveToXElement()
        {
            XElement result = new XElement("DutyDef");
            result.Add(new XElement("defName", this.CurDef.defName));
            if (!this.CurDef.label.NullOrEmpty())
            {
                result.Add(new XElement("label", this.CurDef.label));
            }
            if (!this.CurDef.description.NullOrEmpty())
            {
                result.Add(new XElement("description", this.CurDef.description));
            }
            result.Add(new XElement("alwaysShowWeapon", this.CurDef.alwaysShowWeapon));
            result.Add(new XElement("hook", this.CurDef.hook));
            result.Add(new XElement("socialModeMax", this.CurDef.socialModeMax));
            if (this.CurDef.threatDisabled)
            {
                result.Add(new XElement("threatDisabled", this.CurDef.threatDisabled));
            }
            if (this.CurDef.ritualSpectateTarget)
            {
                result.Add(new XElement("ritualSpectateTarget", this.CurDef.ritualSpectateTarget));
            }
            if (this.CurDef.forceFaceUpPosture)
            {
                result.Add(new XElement("forceFaceUpPosture", this.CurDef.forceFaceUpPosture));
            }
            if (this.CurDef.drawBodyOverride.HasValue)
            {
                result.Add(new XElement("drawBodyOverride", this.CurDef.drawBodyOverride.Value));
            }
            this.AddThinkNodeXml(result, this.CurDef.thinkNode, "thinkNode");
            this.AddThinkNodeXml(result, this.CurDef.constantThinkNode, "constantThinkNode");
            result.Add(new XElement("modExtensions", new XElement("li", new XAttribute("Class", typeof(ModExtension_CustomDuty).FullName))));
            return result;
        }

        private void AddThinkNodeXml(XElement result, ThinkNode node, string nodeName)
        {
            if (node == null)
            {
                return;
            }
            result.Add(DirectXmlSaver.XElementFromObject(node, typeof(ThinkNode), nodeName));
        }

        private void OpenThinkNodeTypeSelect(Action<Type> acceptAction)
        {
            List<string> typeFilterOrder = this.MakeThinkNodeModFilterOrder();
            Dictionary<string, Func<Type, bool>> typeFilters = this.MakeThinkNodeModFilters(typeFilterOrder);
            Find.WindowStack.Add(new Dialog_Select<Type>(this.ThinkNodeTypes, null, this.ThinkNodeTypeLabel, "CQF_DutySelectThinkNode".Translate(), acceptAction, null, null, this.ThinkNodeTypeTip, type => this.ThinkNodeTypePriority(type), null, type => type.Name, typeFilters));
        }

        private ThinkNode MakeThinkNode(Type type)
        {
            return (ThinkNode)Activator.CreateInstance(type);
        }

        private DutyDef CopyDutyDef(DutyDef source)
        {
            return new DutyDef
            {
                defName = source.defName,
                label = source.label,
                description = source.description,
                thinkNode = source.thinkNode?.DeepCopy(false),
                constantThinkNode = source.constantThinkNode?.DeepCopy(false),
                alwaysShowWeapon = source.alwaysShowWeapon,
                hook = source.hook,
                socialModeMax = source.socialModeMax,
                threatDisabled = source.threatDisabled,
                ritualSpectateTarget = source.ritualSpectateTarget,
                forceFaceUpPosture = source.forceFaceUpPosture,
                drawBodyOverride = source.drawBodyOverride
            };
        }

        private ThinkNode ReplaceThinkNode(ThinkNode oldNode, Type type)
        {
            ThinkNode newNode = this.MakeThinkNode(type);
            if (oldNode?.subNodes != null)
            {
                newNode.subNodes.AddRange(oldNode.subNodes);
            }
            return newNode;
        }

        private void OpenDefSelect(Type defType, Action<Def> acceptAction)
        {
            object defs = typeof(DefDatabase<>).MakeGenericType(defType).GetProperty("AllDefsListForReading", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            List<Def> defList = new List<Def>();
            foreach (Def def in (System.Collections.IEnumerable)defs)
            {
                defList.Add(def);
            }
            CQFEditorTools.DrawFloatMenu(defList, acceptAction, def => def.label ?? def.defName, new List<FloatMenuOption>
            {
                new FloatMenuOption("Null".Translate(), () => acceptAction(null))
            });
        }

        private void PrepareThinkNodeTree(ThinkNode node, ThinkNode parent)
        {
            if (node == null)
            {
                return;
            }
            node.parent = parent;
            if (node.subNodes != null)
            {
                foreach (ThinkNode child in node.subNodes)
                {
                    this.PrepareThinkNodeTree(child, node);
                }
            }
            node.ResolveSubnodesAndRecur();
            node.ResolveReferences();
        }

        private List<FieldInfo> GetEditableFields(Type type)
        {
            if (this.fieldCache.TryGetValue(type, out List<FieldInfo> fields))
            {
                return fields;
            }
            fields = new List<FieldInfo>();
            for (Type current = type; current != null && typeof(ThinkNode).IsAssignableFrom(current); current = current.BaseType)
            {
                fields.AddRange(current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(field => !Attribute.IsDefined(field, typeof(UnsavedAttribute))
                                    && !field.IsInitOnly
                                    && field.Name != "subNodes"
                                    && !field.Name.Contains("k__BackingField")));
            }
            fields.Reverse();
            this.fieldCache[type] = fields;
            return fields;
        }

        private string FieldLabel(FieldInfo field)
        {
            string key = "CQF_DutyField_" + field.Name;
            return key.CanTranslate() ? key.Translate().ToString() : field.Name;
        }

        private string FieldTip(FieldInfo field)
        {
            string key = "CQF_DutyFieldTip_" + field.Name;
            return key.CanTranslate() ? key.Translate().ToString() : null;
        }

        private string EnumLabel(object value)
        {
            if (value == null)
            {
                return "Null".Translate().ToString();
            }
            Type type = value.GetType();
            string valueText = value.ToString();
            string key = "CQF_DutyEnum_" + type.Name + "_" + valueText;
            if (key.CanTranslate())
            {
                return key.Translate().ToString();
            }
            return valueText.CanTranslate() ? valueText.Translate().ToString() : valueText;
        }

        private string ThinkNodeLabel(ThinkNode node)
        {
            return this.ThinkNodeTypeLabel(node.GetType());
        }

        private string ThinkNodeTypeLabel(Type type)
        {
            string key = "CQF_DutyNode_" + type.Name;
            if (key.CanTranslate())
            {
                return key.Translate().ToString();
            }
            return type.Name.CanTranslate() ? type.Name.Translate().ToString() : type.Name;
        }

        private string ThinkNodeTypeTip(Type type)
        {
            string modName = this.ThinkNodeModName(type);
            return "CQF_DutyThinkNodeMod".Translate(modName, type.FullName);
        }

        private int ThinkNodeTypePriority(Type type)
        {
            if (type == typeof(ThinkNode_Priority))
            {
                return -300;
            }
            if (ModTypeUtility.IsCQFType(type))
            {
                return -200;
            }
            if (typeof(ThinkNode_JobGiver).IsAssignableFrom(type))
            {
                return -50;
            }
            return 0;
        }

        private Dictionary<string, Func<Type, bool>> MakeThinkNodeModFilters(List<string> modNames)
        {
            Dictionary<string, Func<Type, bool>> result = new Dictionary<string, Func<Type, bool>>();
            foreach (string modName in modNames)
            {
                string capturedName = modName;
                result[capturedName] = type => this.ThinkNodeModName(type) == capturedName;
            }
            return result;
        }

        private List<string> MakeThinkNodeModFilterOrder()
        {
            return this.ThinkNodeTypes
                .Select(type => this.ThinkNodeModName(type))
                .Distinct()
                .OrderBy(name => this.ThinkNodeTypes.Any(type => this.ThinkNodeModName(type) == name && ModTypeUtility.IsCQFType(type)) ? 0 : name == VanillaModName ? 1 : 2)
                .ThenBy(name => name)
                .ToList();
        }

        private string ThinkNodeModName(Type type)
        {
            if (type.Assembly == typeof(ThinkNode).Assembly)
            {
                return VanillaModName;
            }
            return ModTypeUtility.GetModName(type);
        }

        private string GetBuffer(string key, string fallback)
        {
            if (!this.fieldBuffers.ContainsKey(key))
            {
                this.fieldBuffers[key] = fallback;
            }
            return this.fieldBuffers[key];
        }

        private string GetNodeHeightKey(string key)
        {
            return "NodeHeight." + key;
        }

        private List<Type> ThinkNodeTypes
        {
            get
            {
                if (QuestEditor_DutyDefEditor.thinkNodeTypes == null)
                {
                    QuestEditor_DutyDefEditor.thinkNodeTypes = typeof(ThinkNode).AllSubclassesNonAbstract()
                        .Where(type => type.GetConstructor(Type.EmptyTypes) != null)
                        .OrderBy(type => this.ThinkNodeTypePriority(type))
                        .ThenBy(type => type.Name)
                        .ToList();
                }
                return QuestEditor_DutyDefEditor.thinkNodeTypes;
            }
        }

        private Vector2 scrollPos = Vector2.zero;
        private float height = 720f;
        private ThinkNode draggingNode;
        private List<ThinkNode> draggingSource;
        private int draggingIndex = -1;
        private readonly Dictionary<string, string> fieldBuffers = new Dictionary<string, string>();
        private readonly Dictionary<string, float> nodeHeights = new Dictionary<string, float>();
        private readonly HashSet<string> foldouts = new HashSet<string>();
        private readonly Dictionary<Type, List<FieldInfo>> fieldCache = new Dictionary<Type, List<FieldInfo>>();
        private static DutyDef curDef = new DutyDef();
        private static List<Type> thinkNodeTypes;
        private static readonly string VanillaModName = "CQF_DutyThinkNodeCategory_Vanilla".Translate().ToString();
    }

    public class Dialog_DutyDefMisc : Window
    {
        public Dialog_DutyDefMisc(DutyDef def)
        {
            this.def = def;
            this.forcePause = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(720f, 430f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "CQF_DutyMiscSettings".Translate().Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            float y = 45f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_DutyDescription".Translate(), ref this.def.description, 5f, 260f);
            y += 38f;
            Widgets.CheckboxLabeled(new Rect(5f, y, 300f, 28f), "CQF_DutyAlwaysShowWeapon".Translate(), ref this.def.alwaysShowWeapon);
            Widgets.CheckboxLabeled(new Rect(340f, y, 300f, 28f), "CQF_DutyThreatDisabled".Translate(), ref this.def.threatDisabled);
            y += 38f;
            Widgets.CheckboxLabeled(new Rect(5f, y, 300f, 28f), "CQF_DutyRitualSpectateTarget".Translate(), ref this.def.ritualSpectateTarget);
            Widgets.CheckboxLabeled(new Rect(340f, y, 300f, 28f), "CQF_DutyForceFaceUpPosture".Translate(), ref this.def.forceFaceUpPosture);
            y += 45f;
            if (Widgets.ButtonText(new Rect(5f, y, 300f, 32f), "CQF_DutyHook".Translate(this.EnumLabel(this.def.hook)), false))
            {
                CQFEditorTools.DrawFloatMenu<ThinkTreeDutyHook>(Enum.GetValues(typeof(ThinkTreeDutyHook)).Cast<ThinkTreeDutyHook>().ToList(), value => this.def.hook = value, value => this.EnumLabel(value));
            }
            if (Widgets.ButtonText(new Rect(340f, y, 300f, 32f), "CQF_DutySocialModeMax".Translate(this.EnumLabel(this.def.socialModeMax)), false))
            {
                CQFEditorTools.DrawFloatMenu<RandomSocialMode>(Enum.GetValues(typeof(RandomSocialMode)).Cast<RandomSocialMode>().ToList(), value => this.def.socialModeMax = value, value => this.EnumLabel(value));
            }
            y += 45f;
            if (Widgets.ButtonText(new Rect(5f, y, 300f, 32f), "CQF_DutyDrawBodyOverride".Translate() + ": " + this.NullableBoolLabel(this.def.drawBodyOverride), false))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption("Null".Translate(), () => this.def.drawBodyOverride = null),
                    new FloatMenuOption("True".Translate(), () => this.def.drawBodyOverride = true),
                    new FloatMenuOption("False".Translate(), () => this.def.drawBodyOverride = false)
                }));
            }
        }

        private string EnumLabel(object value)
        {
            if (value == null)
            {
                return "Null".Translate().ToString();
            }
            Type type = value.GetType();
            string valueText = value.ToString();
            string key = "CQF_DutyEnum_" + type.Name + "_" + valueText;
            if (key.CanTranslate())
            {
                return key.Translate().ToString();
            }
            return valueText.CanTranslate() ? valueText.Translate().ToString() : valueText;
        }

        private string NullableBoolLabel(bool? value)
        {
            if (!value.HasValue)
            {
                return "Null".Translate();
            }
            return value.Value ? "True".Translate() : "False".Translate();
        }

        private readonly DutyDef def;
    }
}
