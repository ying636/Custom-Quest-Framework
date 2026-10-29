using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFDialogPresentationPreview : IDisposable
{
    public bool Draw(Rect rect, DialogTreeDef tree, DialogNode node, Action<int?> navigate)
    {
        CQFDialogPresentation? active = tree.ActivePresentation;
        if (active == null) { Dispose(); return false; }
        if (!ReferenceEquals(presentation, active))
        {
            renderer?.Dispose();
            presentation = active;
            renderer = active.CreateRenderer();
        }
        if (!participantsInitialized)
        {
            participantsInitialized = true;
            interviewer = Find.CurrentMap?.mapPawns.FreeColonists.FirstOrDefault();
            interviewee = Find.CurrentMap?.mapPawns.FreeColonists.FirstOrDefault(pawn => pawn != interviewer) ?? interviewer;
        }
        using CQFUIScope scope = new(rect.width, rect.height);
        float half = (rect.width - 8f) * 0.5f;
        Participant(new Rect(rect.x, rect.y, half, 32f), interviewer, pawn => interviewer = pawn, "CQF_DialogStyle_Interviewer");
        Participant(new Rect(rect.x + half + 8f, rect.y, half, 32f), interviewee, pawn => interviewee = pawn, "CQF_DialogStyle_Interviewee");
        List<DialogElement_Option> options = new();
        foreach (DialogOption option in node.options)
        {
            string label = Display(option.text);
            if (option.results.Count == 0) options.Add(new DialogElement_Option(label, () => navigate(null)));
            foreach (DialogResult result in option.results)
            {
                string caption = option.results.Count > 1 ? label + " · " + Display(result.resultName) : label;
                options.Add(new DialogElement_Option(caption, () => navigate(result.nextIndex)));
            }
        }
        CQFDialogFrame frame = new(tree, node, Display(tree.title), new[] { new DialogElement_Text(Display(node.text)) }, options,
            interviewer, interviewee, () => navigate(null), isPreview: true);
        renderer!.Draw(new Rect(rect.x, rect.y + 40f, rect.width, Mathf.Max(1f, rect.height - 40f)), frame);
        return true;
    }

    public void Dispose()
    {
        renderer?.Dispose();
        renderer = null;
        presentation = null;
    }

    private static string Display(string value) => value.CanTranslate() ? value.Translate().ToString() : value;

    private static void Participant(Rect rect, Pawn? pawn, Action<Pawn?> selected, string key)
    {
        if (!CQFUIStyle.ButtonText(rect, pawn?.LabelShort ?? key.Translate().ToString())) return;
        List<FloatMenuOption> menu = new() { new("DialogImage_None".Translate(), () => selected(null)) };
        if (Find.CurrentMap != null)
            menu.AddRange(Find.CurrentMap.mapPawns.AllPawnsSpawned.Select(item => new FloatMenuOption(item.LabelShort, () => selected(item))));
        Find.WindowStack.Add(new FloatMenu(menu));
    }

    private CQFDialogPresentation? presentation;
    private CQFDialogRenderer? renderer;
    private Pawn? interviewer;
    private Pawn? interviewee;
    private bool participantsInitialized;
}
