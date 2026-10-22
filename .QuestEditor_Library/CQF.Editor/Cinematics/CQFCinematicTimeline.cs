using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicTimeline
{
    public CQFCinematicStep? Selected => selected;
    public Action<CQFCinematicStep?>? SelectionChanged { get; set; }
    public Action<CQFCinematicStep>? StepAdded { get; set; }

    public void Reset()
    {
        selected = null;
        dragging = null;
        scroll = Vector2.zero;
        SelectionChanged?.Invoke(null);
    }

    public void Draw(Rect rect, CQFCinematicDef definition, Func<CQFCinematicStep, string>? summary = null)
    {
        using CQFUIScope scope = new(rect.width, rect.height);
        CQFUIStyle.DrawMenuSection(rect);
        GUI.BeginGroup(rect);
        rows.Clear();
        Append(definition.steps, 0, 0f, false);
        float total = Mathf.Max(1f, rows.Count == 0 ? 1f : rows.Max(row => row.End));
        float labelWidth = Mathf.Min(310f, rect.width * 0.33f);
        float axisWidth = Mathf.Max(40f, rect.width - labelWidth - 28f);
        Rect axis = new(labelWidth, 0f, axisWidth, 23f);
        TooltipHandler.TipRegion(axis, "CQF_Cinematic_TimingEstimate".Translate());
        for (int index = 0; index <= 4; index++)
        {
            float position = index / 4f;
            Widgets.Label(new Rect(axis.x + position * Mathf.Max(0f, axis.width - 46f), axis.y, 50f, 24f), (position * total).ToString("0.0"));
        }
        Rect viewport = new(4f, 25f, rect.width - 8f, Mathf.Max(20f, rect.height - 29f));
        Rect content = new(0f, 0f, viewport.width - 18f, Mathf.Max(viewport.height, rows.Count * 36f + 4f));
        Widgets.BeginScrollView(viewport, ref scroll, content);
        for (int index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            Rect rowRect = new(0f, index * 36f, content.width, 32f);
            Widgets.DrawBoxSolid(rowRect, row.Step == selected ? CQFUIStyle.Header : index % 2 == 0 ? CQFUIStyle.Card : CQFUIStyle.Panel);
            string typeKey = "CQF_Cinematic_Step_" + row.Step.GetType().Name.Replace("CQFCinematicStep_", string.Empty);
            string detail = summary?.Invoke(row.Step) ?? string.Empty;
            string caption = typeKey.Translate() + (detail.NullOrEmpty() ? string.Empty : " · " + detail);
            Rect labelRect = new(6f + row.Depth * 14f, rowRect.y + 3f, Mathf.Max(25f, labelWidth - row.Depth * 14f - 14f), 26f);
            Widgets.LabelEllipses(labelRect, caption);
            Rect bar = new(labelWidth + row.Start / total * axisWidth, rowRect.y + 7f, Mathf.Max(5f, (row.End - row.Start) / total * axisWidth), 19f);
            bar.width = Mathf.Min(bar.width, Mathf.Max(5f, content.width - bar.x - 4f));
            Widgets.DrawBoxSolid(bar, row.Step == selected ? CQFUIStyle.Accent : CQFUIStyle.Link);
            TooltipHandler.TipRegion(rowRect, caption + "\n" + "CQF_Cinematic_TimingEstimate".Translate() + ": " + row.Start.ToString("0.0") + "–" + row.End.ToString("0.0"));
            HandleRowInput(rowRect, row.Step, row.Owner);
            if (dragging != null && dragMoved && row.Owner == draggingOwner && rowRect.Contains(Event.current.mousePosition))
                Widgets.DrawBoxSolid(new Rect(0f, rowRect.y, rowRect.width, 2f), CQFUIStyle.Accent);
        }
        Widgets.EndScrollView();
        if (Event.current.rawType == EventType.MouseUp)
        {
            dragging = null;
            draggingOwner = null;
            dragMoved = false;
        }
        GUI.EndGroup();
    }

    public void OpenAddMenu(CQFCinematicDef definition)
    {
        List<FloatMenuOption> options = typeof(CQFCinematicStep).AllSubclassesNonAbstract().OrderBy(type => type.Name)
            .Select(type => new FloatMenuOption(("CQF_Cinematic_Step_" + type.Name.Replace("CQFCinematicStep_", string.Empty)).Translate(), () =>
                Add(definition, (CQFCinematicStep)Activator.CreateInstance(type)!))).ToList();
        Find.WindowStack.Add(new FloatMenu(options));
    }

    public void Add(CQFCinematicDef definition, CQFCinematicStep step)
    {
        List<CQFCinematicStep>? group = Children(selected);
        List<CQFCinematicStep> owner = group ?? (selected == null ? definition.steps : FindOwner(definition.steps, selected) ?? definition.steps);
        int insertion = group == null && selected != null && owner.Contains(selected) ? owner.IndexOf(selected) + 1 : owner.Count;
        owner.Insert(insertion, step);
        selected = step;
        StepAdded?.Invoke(step);
        SelectionChanged?.Invoke(step);
    }

    public void CopySelected(List<CQFCinematicStep> steps)
    {
        if (selected == null) return;
        List<CQFCinematicStep>? owner = FindOwner(steps, selected);
        if (owner == null) return;
        try
        {
            CQFCinematicStep copy = selected.Copy();
            owner.Insert(owner.IndexOf(selected) + 1, copy);
            selected = copy;
            StepAdded?.Invoke(copy);
            SelectionChanged?.Invoke(copy);
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Cinematic step copy failed: " + error);
            Messages.Message("CQF_Cinematic_CopyFailed".Translate() + ": " + error.Message, MessageTypeDefOf.RejectInput);
        }
    }

    public void RemoveSelected(List<CQFCinematicStep> steps)
    {
        if (selected == null) return;
        FindOwner(steps, selected)?.Remove(selected);
        selected = null;
        SelectionChanged?.Invoke(null);
    }

    private void Append(List<CQFCinematicStep> steps, int depth, float start, bool parallel)
    {
        if (depth > 32) return;
        float next = start;
        foreach (CQFCinematicStep step in steps)
        {
            float length = Estimate(step);
            rows.Add((step, steps, depth, next, next + length));
            List<CQFCinematicStep>? children = Children(step);
            if (children != null) Append(children, depth + 1, next, step is CQFCinematicStep_Parallel);
            if (!parallel) next += length;
        }
    }

    private float Estimate(CQFCinematicStep step, int depth = 0)
    {
        if (depth > 32) return 0f;
        List<CQFCinematicStep>? children = Children(step);
        if (children == null || children.Count == 0)
            return float.IsNaN(step.duration) || float.IsInfinity(step.duration) ? 0f : Mathf.Max(0f, step.duration);
        return step is CQFCinematicStep_Parallel ? children.Max(child => Estimate(child, depth + 1)) : children.Sum(child => Estimate(child, depth + 1));
    }

    private List<CQFCinematicStep>? Children(CQFCinematicStep? step)
    {
        return step switch { CQFCinematicStep_Sequence sequence => sequence.steps, CQFCinematicStep_Parallel parallel => parallel.steps, _ => null };
    }

    private List<CQFCinematicStep>? FindOwner(List<CQFCinematicStep> steps, CQFCinematicStep step, int depth = 0)
    {
        if (steps.Contains(step)) return steps;
        if (depth > 32) return null;
        foreach (CQFCinematicStep candidate in steps)
        {
            List<CQFCinematicStep>? children = Children(candidate);
            if (children != null && FindOwner(children, step, depth + 1) is { } owner) return owner;
        }
        return null;
    }

    private void HandleRowInput(Rect rect, CQFCinematicStep step, List<CQFCinematicStep> owner)
    {
        if (Find.WindowStack.MouseObscuredNow) return;
        Event input = Event.current;
        if (input.type == EventType.MouseDown && input.button == 0 && rect.Contains(input.mousePosition))
        {
            selected = step;
            SelectionChanged?.Invoke(step);
            dragging = step;
            draggingOwner = owner;
            dragOrigin = input.mousePosition;
            dragMoved = false;
            input.Use();
        }
        else if (input.type == EventType.MouseDrag && input.button == 0 && dragging != null)
        {
            dragMoved |= Vector2.Distance(dragOrigin, input.mousePosition) > 4f;
            input.Use();
        }
        else if (input.type == EventType.MouseUp && dragging != null && rect.Contains(input.mousePosition))
        {
            if (dragMoved && draggingOwner == owner)
            {
                int destination = owner.IndexOf(step);
                owner.Remove(dragging);
                owner.Insert(Mathf.Clamp(destination, 0, owner.Count), dragging);
            }
            else if (dragMoved && draggingOwner != owner)
                Messages.Message("CQF_Cinematic_ReorderSameGroup".Translate(), MessageTypeDefOf.CautionInput);
            dragging = null;
            draggingOwner = null;
            input.Use();
        }
    }

    private readonly List<(CQFCinematicStep Step, List<CQFCinematicStep> Owner, int Depth, float Start, float End)> rows = new();
    private CQFCinematicStep? selected;
    private CQFCinematicStep? dragging;
    private List<CQFCinematicStep>? draggingOwner;
    private Vector2 scroll;
    private Vector2 dragOrigin;
    private bool dragMoved;
}
