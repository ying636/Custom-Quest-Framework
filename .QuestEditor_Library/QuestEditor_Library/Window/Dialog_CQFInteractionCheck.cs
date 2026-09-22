using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Dialog_CQFInteractionCheck : Window
    {
        public Dialog_CQFInteractionCheck(InteractionOperation operation, Thing owner)
        {
            this.operation = operation;
            this.owner = owner;
            this.forcePause = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(800f, 650f);

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 30f), "CQF_EditorCheckConditionsOnly".Translate());
            if (Widgets.ButtonText(new Rect(0f, 36f, 260f, 32f), this.pawn?.LabelShortCap ?? "CQF_EditorTestPawn".Translate()))
            {
                List<Pawn> pawns = this.owner?.Map?.mapPawns.AllPawnsSpawned.ToList() ?? new List<Pawn>();
                Find.WindowStack.Add(new Dialog_Select<Pawn>(new TextSelectDrawer<Pawn>(pawns, p => p.LabelShortCap, p => this.pawn = p), "CQF_EditorTestPawn".Translate()));
            }
            if (Widgets.ButtonText(new Rect(280f, 36f, 180f, 32f), "CQF_EditorCheck".Translate()))
            {
                this.Check();
            }
            Rect outRect = new Rect(0f, 84f, inRect.width, inRect.height - 84f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, this.height));
            Widgets.BeginScrollView(outRect, ref this.scroll, viewRect);
            float y = 0f;
            foreach (CQFEditorDiagnostic row in this.rows)
            {
                float rowHeight = Mathf.Max(32f, Text.CalcHeight(row.Text, viewRect.width - 12f) + 10f);
                Rect rect = new Rect(0f, y, viewRect.width, rowHeight);
                Widgets.Label(rect, row.Text);
                if (row.Editable != null)
                {
                    Widgets.DrawHighlightIfMouseover(rect);
                    if (Widgets.ButtonInvisible(rect))
                    {
                        using (new CQFEditorContext(this.owner))
                        {
                            Find.WindowStack.Add(new Dialog_EditIDrawable(row.Editable));
                        }
                    }
                }
                y += rowHeight + 5f;
            }
            this.height = y;
            Widgets.EndScrollView();
        }

        private void Check()
        {
            this.rows.Clear();
            if (this.owner == null || !this.owner.Spawned || this.pawn == null || !this.pawn.Spawned || this.pawn.Map != this.owner.Map)
            {
                this.rows.Add(new CQFEditorDiagnostic("CQF_EditorTargetUnavailable".Translate()));
                return;
            }
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>
            {
                { "CustomThing", this.owner },
                { "Trigger", this.pawn }
            };
            Quest quest = GameTools.GetQuestFromThing(this.owner);
            Rand.PushState();
            try
            {
                bool canStart = this.CheckConditions(this.operation.conditions, targets, quest, "InteractionConditions".Translate());
                bool requirements = true;
                string reason = null;
                foreach (CQFThingDefCount required in this.operation.requiredThings.OfType<CQFThingDefCount>())
                {
                    if (required.thing == null)
                    {
                        requirements = false;
                        reason = "CQF_EditorUnresolvedOrExternal".Translate();
                        break;
                    }
                    int count = this.pawn.inventory?.Count(required.thing) ?? 0;
                    if (count < required.count.min)
                    {
                        requirements = false;
                        reason = "NoRequiredThing".Translate(required.thing.label, count, required.count.min.ToString());
                        break;
                    }
                }
                this.rows.Add(new CQFEditorDiagnostic("InteractionOption_RequiredThing".Translate() + ": "
                    + (requirements ? "✓" : "✕") + " " + reason));
                bool selected = false;
                foreach (InteractionResult result in this.operation.results)
                {
                    if (!canStart || !requirements || (selected && this.operation.onlyGenerateSingleResult))
                    {
                        this.rows.Add(new CQFEditorDiagnostic(result.resultName + " · " + "CQF_EditorSkipped".Translate()));
                        continue;
                    }
                    bool matches = this.CheckConditions(result.conditions, targets, quest, result.resultName);
                    if (matches)
                    {
                        this.rows.Add(new CQFEditorDiagnostic("CQF_EditorActionReferencePreview".Translate()));
                        selected = true;
                    }
                    this.rows.Add(new CQFEditorDiagnostic(result.resultName + " · "
                        + (matches ? "CQF_EditorWouldExecute" : "CQF_EditorSkipped").Translate()));
                    if (matches)
                    {
                        HashSet<object> visited = new HashSet<object>();
                        foreach (CQFAction action in result.actions)
                        {
                            this.InspectAction(action, targets, quest, visited);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error("[CQF] Interaction check failed: " + exception);
                this.rows.Add(new CQFEditorDiagnostic("CQF_EditorCheckFailed".Translate() + ": " + exception.Message));
            }
            finally
            {
                Rand.PopState();
            }
        }

        private bool CheckConditions(IEnumerable<DialogCondition> conditions, Dictionary<string, TargetInfo> targets, Quest quest, string path)
        {
            foreach (DialogCondition condition in conditions)
            {
                bool passed;
                string reason;
                try
                {
                    passed = condition.Satisfied(targets, out reason, quest);
                }
                catch (Exception exception)
                {
                    Log.Error("[CQF] Condition check failed: " + condition.GetType().FullName + ": " + exception);
                    this.rows.Add(new CQFEditorDiagnostic(path + " / " + condition.GetType().Name.Translate()
                        + ": " + "CQF_EditorCheckFailed".Translate() + ": " + exception.Message, condition));
                    return false;
                }
                this.rows.Add(new CQFEditorDiagnostic(path + " / " + condition.GetType().Name.Translate() + ": "
                    + (passed ? "✓" : "✕") + " " + reason, condition));
                if (!passed)
                {
                    return false;
                }
            }
            return true;
        }

        private void InspectAction(CQFAction action, Dictionary<string, TargetInfo> targets, Quest quest, HashSet<object> visited)
        {
            if (action == null || !visited.Add(action))
            {
                return;
            }
            this.rows.Add(new CQFEditorDiagnostic(action.GetType().Name.Translate(), action));
            if (action is CQFAction_Target targeted)
            {
                foreach (string key in targeted.targetsText)
                {
                    TargetInfo target = GameTools.GetTarget(targets, quest, key);
                    List<TargetInfo> group = GameTools.GetTargetsFromGroup(quest, key);
                    string value = target.IsValid ? target.Thing?.LabelCap + " " + target.Cell
                        : group != null && group.Any() ? group.Count.ToString() : "CQF_EditorUnresolvedOrExternal".Translate().ToString();
                    this.rows.Add(new CQFEditorDiagnostic("  " + key + " → " + value, action));
                }
            }
            foreach (FieldInfo field in action.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.GetValue(action) is CQFAction child)
                {
                    this.InspectAction(child, targets, quest, visited);
                }
                else if (field.GetValue(action) is IEnumerable<CQFAction> children)
                {
                    foreach (CQFAction nested in children)
                    {
                        this.InspectAction(nested, targets, quest, visited);
                    }
                }
            }
        }

        private readonly InteractionOperation operation;
        private readonly Thing owner;
        private readonly List<CQFEditorDiagnostic> rows = new List<CQFEditorDiagnostic>();
        private Pawn pawn;
        private Vector2 scroll;
        private float height;
    }
}
