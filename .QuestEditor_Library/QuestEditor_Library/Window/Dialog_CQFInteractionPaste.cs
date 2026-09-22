using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Dialog_CQFInteractionPaste : Window
    {
        public Dialog_CQFInteractionPaste(InteractableThing owner, IEnumerable<InteractionOperation> operations,
            IEnumerable<InteractionDataDef> definitions = null)
            : this(new[] { owner }, operations, definitions)
        {
        }

        public Dialog_CQFInteractionPaste(IEnumerable<InteractableThing> owners, IEnumerable<InteractionOperation> operations,
            IEnumerable<InteractionDataDef> definitions = null)
        {
            this.owners = owners.Distinct().ToList();
            this.draft = new CQFInteractionDraft(operations, definitions);
            this.forcePause = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(760f, 620f);

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0f, 0f, inRect.width - 35f, 30f), "Paste".Translate() + " · "
                + "InteractionOperations".Translate() + ": " + this.draft.Count + " → " + this.owners.Count);
            TooltipHandler.TipRegion(new Rect(0f, 0f, inRect.width - 35f, 30f), string.Join("\n", this.owners.Select(o => o.LabelCap + " " + o.Position)));
            Widgets.CheckboxLabeled(new Rect(0f, 36f, inRect.width, 28f), "CQF_EditorReplace".Translate(), ref this.replace);
            Rect outRect = new Rect(0f, 76f, inRect.width, inRect.height - 136f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, this.contentHeight));
            Widgets.BeginScrollView(outRect, ref this.scroll, viewRect);
            float y = 0f;
            foreach (CQFConfigurationReference reference in this.draft.References)
            {
                Widgets.Label(new Rect(0f, y, viewRect.width, 26f), reference.Kind.Translate() + ": " + reference.Original);
                y += 29f;
                if (reference.Kind == "CQF_EditorTargetKey")
                {
                    using (new CQFEditorContext(this.owners.FirstOrDefault()))
                    {
                        CQFTargetSelectionSession.DrawField(ref y, viewRect, 0f, reference.Value, value => reference.Value = value);
                    }
                    y += 10f;
                }
                else
                {
                    reference.Value = Widgets.TextField(new Rect(0f, y, viewRect.width, 30f), reference.Value);
                    y += 43f;
                }
            }
            this.contentHeight = y;
            Widgets.EndScrollView();
            if (this.applied)
            {
                if (Widgets.ButtonText(new Rect(0f, inRect.height - 40f, 180f, 35f), "CQF_EditorUndoPaste".Translate()))
                {
                    this.Undo();
                }
            }
            else if (Widgets.ButtonText(new Rect(0f, inRect.height - 40f, 180f, 35f), "Paste".Translate()))
            {
                this.Apply();
            }
            if (Widgets.ButtonText(new Rect(inRect.width - 180f, inRect.height - 40f, 180f, 35f), "Close".Translate()))
            {
                this.Close();
            }
        }

        private void Apply()
        {
            try
            {
                if (this.draft.Count == 0 || this.owners.Count == 0 || this.owners.Any(o => o == null || o.Destroyed))
                {
                    throw new InvalidOperationException("CQF_EditorEmptyClipboard".Translate());
                }
                List<List<InteractionOperation>> copies = this.owners.Select(o => this.draft.CreateOperations()).ToList();
                this.previousOperations = this.owners.Select(o => o.operations.ToList()).ToList();
                this.previousDefinitions = this.owners.Select(o => o.operationDefs.ToList()).ToList();
                for (int i = 0; i < this.owners.Count; i++)
                {
                    InteractableThing owner = this.owners[i];
                    if (this.replace)
                    {
                        owner.operations.Clear();
                        owner.operationDefs.Clear();
                    }
                    owner.operations.AddRange(copies[i]);
                }
                this.applied = true;
                GUI.changed = true;
            }
            catch (Exception exception)
            {
                Log.Error("[CQF] Interaction paste failed: " + exception);
                Messages.Message("CQF_EditorPasteFailed".Translate() + ": " + exception.Message, MessageTypeDefOf.RejectInput, false);
            }
        }

        private void Undo()
        {
            for (int i = 0; i < this.owners.Count; i++)
            {
                if (this.owners[i] == null || this.owners[i].Destroyed)
                {
                    Messages.Message("CQF_EditorTargetUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                    continue;
                }
                this.owners[i].operations = this.previousOperations[i];
                this.owners[i].operationDefs = this.previousDefinitions[i];
            }
            this.applied = false;
            GUI.changed = true;
        }

        private readonly List<InteractableThing> owners;
        private readonly CQFInteractionDraft draft;
        private List<List<InteractionOperation>> previousOperations;
        private List<List<InteractionDataDef>> previousDefinitions;
        private Vector2 scroll;
        private float contentHeight;
        private bool replace;
        private bool applied;
    }
}
