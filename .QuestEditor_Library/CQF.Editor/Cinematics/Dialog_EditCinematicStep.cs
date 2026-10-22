using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Dialog_EditCinematicStep : Window
{
    public Dialog_EditCinematicStep(object value)
    {
        this.value = value;
        fields = value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => field.DeclaringType != typeof(Def) && field.Name is not ("bindings" or "roles") && !field.IsInitOnly && !field.IsDefined(typeof(UnsavedAttribute)))
            .OrderBy(field => field.DeclaringType == typeof(CQFCinematicStep) ? 0 : 1).ThenBy(field => field.MetadataToken).ToArray();
        doCloseX = true;
        absorbInputAroundWindow = true;
        resizeable = true;
        draggable = true;
    }

    public override Vector2 InitialSize => new(Mathf.Min(UI.screenWidth - 40f, 820f), Mathf.Min(UI.screenHeight - 60f, 760f));

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        windowRect.width = Mathf.Clamp(windowRect.width, Mathf.Min(480f, UI.screenWidth - 20f), UI.screenWidth - 20f);
        windowRect.height = Mathf.Clamp(windowRect.height, Mathf.Min(300f, UI.screenHeight - 20f), UI.screenHeight - 20f);
        windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, UI.screenWidth - windowRect.width));
        windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, UI.screenHeight - windowRect.height));
    }

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width, inRect.height);
        Widgets.LabelEllipses(new Rect(0f, 0f, inRect.width - 30f, 32f), "CQF_Cinematic_Parameters".Translate().Colorize(CQFUIStyle.Accent));
        Rect viewport = new(0f, 40f, inRect.width, inRect.height - 44f);
        Rect content = new(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, height));
        Widgets.BeginScrollView(viewport, ref scroll, content);
        float y = 0f;
        foreach (FieldInfo field in fields)
        {
            if (field.FieldType == typeof(List<CQFCinematicStep>)) continue;
            DrawField(field, ref y, content);
        }
        height = y + 12f;
        Widgets.EndScrollView();
    }

    private void DrawField(FieldInfo field, ref float y, Rect content)
    {
        string key = "CQF_Cinematic_Field_" + field.Name;
        string label = key.Translate();
        object? current = field.GetValue(value);
        float labelWidth = Mathf.Min(280f, content.width * 0.48f);
        Rect labelRect = new(4f, y, labelWidth - 8f, 28f);
        Rect fieldRect = new(labelWidth + 4f, y, content.width - labelWidth - 8f, 28f);
        TooltipHandler.TipRegion(labelRect, label);
        if (field.FieldType == typeof(List<CQFAction>))
        {
            List<CQFAction> actions = (List<CQFAction>?)current ?? new();
            field.SetValue(value, actions);
            CQFActionListEditor.Draw(ref y, 4f, content.width - 8f, content, label, actions);
            y += 8f;
            return;
        }
        if (field.FieldType == typeof(List<DialogCondition>))
        {
            List<DialogCondition> conditions = (List<DialogCondition>?)current ?? new();
            field.SetValue(value, conditions);
            CQFConditionListEditor.Draw(ref y, 4f, content.width - 8f, content, label, conditions);
            y += 8f;
            return;
        }
        if (field.FieldType == typeof(bool))
        {
            bool edited = current is true;
            Widgets.CheckboxLabeled(new Rect(4f, y, content.width - 8f, 28f), label.Truncate(Mathf.Max(20f, content.width - 46f)), ref edited);
            field.SetValue(value, edited);
        }
        else if (field.FieldType == typeof(string))
        {
            string text = (string?)current ?? string.Empty;
            if (field.Name.IndexOf("signal", StringComparison.OrdinalIgnoreCase) >= 0)
                CQFSignalEditor.DrawBookField(labelRect, fieldRect, label, text, edited => field.SetValue(value, edited));
            else if (field.Name.IndexOf("target", StringComparison.OrdinalIgnoreCase) >= 0 || field.Name.EndsWith("Key", StringComparison.Ordinal) && field.Name != "textKey" || field.Name is "actor" or "destination" or "interviewerText" or "intervieeText")
                CQFTargetKeyEditor.DrawBookField(labelRect, fieldRect, label, text, edited => field.SetValue(value, edited));
            else
            {
                Widgets.LabelEllipses(labelRect, label);
                field.SetValue(value, Widgets.TextField(fieldRect, text));
            }
        }
        else if (field.FieldType == typeof(float))
        {
            Widgets.LabelEllipses(labelRect, label);
            float edited = current is float number ? number : 0f;
            string buffer = GetBuffer(field, edited.ToString(CultureInfo.InvariantCulture));
            Widgets.TextFieldNumeric(fieldRect, ref edited, ref buffer, -1000000f, 1000000f);
            buffers[field.Name] = buffer;
            field.SetValue(value, edited);
        }
        else if (field.FieldType == typeof(int))
        {
            Widgets.LabelEllipses(labelRect, label);
            int edited = current is int number ? number : 0;
            string buffer = GetBuffer(field, edited.ToString(CultureInfo.InvariantCulture));
            Widgets.TextFieldNumeric(fieldRect, ref edited, ref buffer, -1000000f, 1000000f);
            buffers[field.Name] = buffer;
            field.SetValue(value, edited);
        }
        else if (field.FieldType.IsEnum)
        {
            Widgets.LabelEllipses(labelRect, label);
            string enumKey = "CQF_Cinematic_" + field.FieldType.Name.Replace("CQFCinematic", string.Empty) + "_" + current;
            if (CQFUIStyle.ButtonText(fieldRect, enumKey.Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(Enum.GetValues(field.FieldType).Cast<object>().Select(item => new FloatMenuOption(("CQF_Cinematic_" + field.FieldType.Name.Replace("CQFCinematic", string.Empty) + "_" + item).Translate(), () => field.SetValue(value, item))).ToList()));
            }
        }
        else if (typeof(Def).IsAssignableFrom(field.FieldType))
        {
            Widgets.LabelEllipses(labelRect, label);
            if (CQFUIStyle.ButtonText(fieldRect, (current as Def)?.defName ?? "CQF_Cinematic_None".Translate().ToString()))
            {
                List<FloatMenuOption> choices = GenDefDatabase.GetAllDefsInDatabaseForDef(field.FieldType)
                    .OrderBy(definition => definition.defName).Select(definition => new FloatMenuOption(definition.defName, () => field.SetValue(value, definition))).ToList();
                choices.Add(new FloatMenuOption("CQF_Cinematic_None".Translate(), () => field.SetValue(value, null)));
                Find.WindowStack.Add(new FloatMenu(choices));
            }
        }
        else if (field.FieldType == typeof(Rot4))
        {
            Widgets.LabelEllipses(labelRect, label);
            Rot4 rotation = current is Rot4 present ? present : Rot4.South;
            Rot4[] rotations = { Rot4.North, Rot4.East, Rot4.South, Rot4.West };
            string[] names = { "North", "East", "South", "West" };
            int selected = Array.IndexOf(rotations, rotation);
            if (CQFUIStyle.ButtonText(fieldRect, ("CQF_Cinematic_Rotation_" + names[Mathf.Clamp(selected, 0, 3)]).Translate()))
                Find.WindowStack.Add(new FloatMenu(rotations.Select((item, index) => new FloatMenuOption(("CQF_Cinematic_Rotation_" + names[index]).Translate(), () => field.SetValue(value, item))).ToList()));
        }
        else if (field.FieldType == typeof(Color) || field.FieldType == typeof(Rect))
        {
            Widgets.LabelEllipses(labelRect, label);
            Color color = current is Color present ? present : Color.white;
            Rect rectangle = current is Rect presentRect ? presentRect : new Rect(0f, 0f, 1f, 1f);
            float width = (fieldRect.width - 12f) / 4f;
            float[] components = field.FieldType == typeof(Color) ? new[] { color.r, color.g, color.b, color.a } : new[] { rectangle.x, rectangle.y, rectangle.width, rectangle.height };
            for (int index = 0; index < 4; index++)
            {
                string bufferKey = field.Name + index;
                string buffer = buffers.TryGetValue(bufferKey, out string? saved) ? saved : components[index].ToString(CultureInfo.InvariantCulture);
                Rect componentRect = new(fieldRect.x + index * (width + 4f), y, width, 28f);
                Widgets.TextFieldNumeric(componentRect, ref components[index], ref buffer, 0f, 1f);
                string componentKey = "CQF_Cinematic_" + (field.FieldType == typeof(Color) ? new[] { "Red", "Green", "Blue", "Alpha" }[index] : new[] { "X", "Y", "Width", "Height" }[index]);
                TooltipHandler.TipRegion(componentRect, componentKey.Translate());
                buffers[bufferKey] = buffer;
            }
            field.SetValue(value, field.FieldType == typeof(Color) ? (object)new Color(components[0], components[1], components[2], components[3]) : new Rect(components[0], components[1], components[2], components[3]));
        }
        else if (field.FieldType == typeof(Vector2) || field.FieldType == typeof(Vector3) || field.FieldType == typeof(IntVec3))
        {
            Widgets.LabelEllipses(labelRect, label);
            string previous = GetBuffer(field, current?.ToString() ?? string.Empty);
            string edited = Widgets.TextField(fieldRect, previous);
            buffers[field.Name] = edited;
            if (edited != previous)
            {
                try
                {
                    object parsed = ParseHelper.FromString(edited, field.FieldType);
                    field.SetValue(value, parsed);
                    invalidFields.Remove(field.Name);
                }
                catch (Exception error)
                {
                    invalidFields[field.Name] = error.Message;
                }
            }
            if (invalidFields.TryGetValue(field.Name, out string? issue))
            {
                TooltipHandler.TipRegion(fieldRect, "CQF_Cinematic_InvalidValue".Translate() + ": " + issue);
                CQFUIStyle.DrawBox(fieldRect, 2);
            }
        }
        else
        {
            Widgets.LabelEllipses(labelRect, label);
            Widgets.LabelEllipses(fieldRect, "CQF_Cinematic_UnsupportedField".Translate() + ": " + field.FieldType.Name);
        }
        y += 38f;
    }

    private string GetBuffer(FieldInfo field, string fallback)
    {
        return buffers.TryGetValue(field.Name, out string? buffer) ? buffer : fallback;
    }

    private readonly object value;
    private readonly FieldInfo[] fields;
    private readonly Dictionary<string, string> buffers = new();
    private readonly Dictionary<string, string> invalidFields = new();
    private Vector2 scroll;
    private float height;
}
