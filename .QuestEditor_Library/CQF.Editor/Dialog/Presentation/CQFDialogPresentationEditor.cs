using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public abstract class CQFDialogPresentationEditor
{
    public abstract string StyleId { get; }

    public abstract void DrawTree(CQFDialogPresentation presentation, ref float y, float x, float width);
    public abstract void DrawNode(CQFDialogPresentation presentation, CQFDialogNodePresentation node, ref float y, float x, float width);

    public static void DrawTreeSettings(DialogTreeDef tree, ref float y, float x, float width)
    {
        CQFDialogPresentation? active = tree.ActivePresentation;
        string caption = "CQF_DialogStyle_Style".Translate() + ": " + (active?.LabelKey ?? "CQF_DialogStyle_Classic").Translate();
        float height = Mathf.Max(36f, Text.CalcHeight(caption, width - 20f) + 12f);
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, height), caption))
        {
            List<FloatMenuOption> menu = new() { new("CQF_DialogStyle_Classic".Translate(), () => tree.dialogStyle = string.Empty) };
            foreach (Type type in typeof(CQFDialogPresentation).AllSubclassesNonAbstract())
            {
                CQFDialogPresentation candidate = (CQFDialogPresentation)(Activator.CreateInstance(type) ?? throw new InvalidOperationException("CQF cannot create dialog presentation: " + type));
                menu.Add(new FloatMenuOption(candidate.LabelKey.Translate(), () =>
                {
                    if (!tree.presentations.Any(item => item.Id == candidate.Id)) tree.presentations.Add(candidate);
                    tree.dialogStyle = candidate.Id;
                }));
            }
            Find.WindowStack.Add(new FloatMenu(menu));
        }
        y += height + 12f;
        if (active != null) GetEditor(active.Id).DrawTree(active, ref y, x, width);
    }

    public static bool DrawNodeSettings(DialogTreeDef tree, DialogNode node, ref float y, float x, float width)
    {
        CQFDialogPresentation? active = tree.ActivePresentation;
        if (active == null) return false;
        CQFDialogNodePresentation? settings = node.presentations.SingleOrDefault(item => item.StyleId == active.Id);
        if (settings == null)
        {
            settings = active.CreateNodePresentation(node);
            node.presentations.Add(settings);
        }
        GetEditor(active.Id).DrawNode(active, settings, ref y, x, width);
        return true;
    }

    private static CQFDialogPresentationEditor GetEditor(string id)
    {
        if (editors == null)
        {
            editors = new Dictionary<string, CQFDialogPresentationEditor>();
            foreach (Type type in typeof(CQFDialogPresentationEditor).AllSubclassesNonAbstract())
            {
                CQFDialogPresentationEditor editor = (CQFDialogPresentationEditor)(Activator.CreateInstance(type) ?? throw new InvalidOperationException("CQF cannot create dialog presentation editor: " + type));
                if (editors.ContainsKey(editor.StyleId)) throw new InvalidOperationException("CQF duplicate dialog presentation editor: " + editor.StyleId);
                editors.Add(editor.StyleId, editor);
            }
        }
        if (!editors.TryGetValue(id, out CQFDialogPresentationEditor? editorForStyle))
            throw new InvalidOperationException("CQF dialog presentation editor is missing: " + id);
        return editorForStyle;
    }

    private static Dictionary<string, CQFDialogPresentationEditor>? editors;
}
