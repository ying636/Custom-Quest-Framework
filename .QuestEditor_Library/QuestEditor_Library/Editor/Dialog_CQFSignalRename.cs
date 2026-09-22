using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Dialog_CQFSignalRename : Window
    {
        public Dialog_CQFSignalRename(InteractionOperation operation, string newName, List<CQFSignalEndpoint> references, List<CQFSignalReferenceChange> changes)
        {
            this.operation = operation;
            this.previousName = operation.interactionText;
            this.newName = newName;
            this.changes = changes;
            this.doCloseX = true;
            this.forcePause = true;
            this.syncReferences = changes.Count > 0;
            this.preview = "CQF_MapSignals_RenameImpact".Translate() + "\n" + this.previousName + " → " + newName
                + "\n\n" + "CQF_MapSignals_References".Translate() + ": " + references.Count + "\n"
                + string.Join("\n", references.Select(entry => entry.SourceLabel + " / " + entry.DisplaySignal).Distinct())
                + "\n\n" + "CQF_MapSignals_SyncReferences".Translate() + ": " + changes.Count + "\n"
                + string.Join("\n", changes.Select(change => change.PreviousSignal + " → " + change.NextSignal))
                + "\n\n" + "CQF_MapSignals_KnownOnly".Translate();
        }

        public override Vector2 InitialSize => new Vector2(700f, 580f);

        public override void DoWindowContents(Rect inRect)
        {
            Rect outRect = new Rect(0f, 0f, inRect.width, inRect.height - 90f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, Text.CalcHeight(this.preview, outRect.width - 20f)));
            Widgets.BeginScrollView(outRect, ref this.scroll, viewRect);
            Widgets.Label(viewRect, this.preview);
            Widgets.EndScrollView();
            if (this.changes.Count > 0)
            {
                Widgets.CheckboxLabeled(new Rect(0f, inRect.height - 80f, inRect.width, 30f), "CQF_MapSignals_SyncReferences".Translate(), ref this.syncReferences);
            }
            if (Widgets.ButtonText(new Rect(0f, inRect.height - 38f, inRect.width * 0.5f - 6f, 36f), "Confirm".Translate()))
            {
                if (this.operation.interactionText != this.previousName || this.syncReferences && this.changes.Any(change => !change.IsCurrent))
                {
                    Log.Error("CQF_MapSignals_ChangedReference: " + this.previousName);
                    Messages.Message("CQF_MapSignals_ChangedReference".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }
                if (this.syncReferences)
                {
                    foreach (CQFSignalReferenceChange change in this.changes)
                    {
                        change.Apply();
                    }
                }
                this.operation.interactionText = this.newName;
                CQFSignalEditor.InvalidateSummary(this.operation);
                this.Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.width * 0.5f + 6f, inRect.height - 38f, inRect.width * 0.5f - 6f, 36f), "Cancel".Translate()))
            {
                this.Close();
            }
        }

        private readonly InteractionOperation operation;
        private readonly string previousName;
        private readonly string newName;
        private readonly List<CQFSignalReferenceChange> changes;
        private readonly string preview;
        private bool syncReferences;
        private Vector2 scroll;
    }
}
