using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class Dialog_EditDutyMapNode : Window
    {
        public Dialog_EditDutyMapNode(DutyMapNode node)
        {
            this.node = node;
            this.doCloseX = true;
            this.draggable = true;
        }

        public override Vector2 InitialSize => new Vector2(520f, 680f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(5f, 5f, 420f, 30f), "CQF_DutyMapNodeEditor".Translate().Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            Rect view = new Rect(0f, 0f, inRect.width - 20f, this.height);
            Widgets.BeginScrollView(new Rect(0f, 40f, inRect.width, inRect.height - 45f), ref this.scrollPos, view);
            float y = 5f;
            this.DrawNode(ref y, view);
            Widgets.EndScrollView();
            this.height = y + 40f;
        }

        private void DrawNode(ref float y, Rect inRect)
        {
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_DutyMapNodeId".Translate(), ref this.node.nodeId, 5f, 120f);
            y += 30f;
            if (Widgets.ButtonText(new Rect(5f, y, 300f, 25f), "CQF_DutyType".Translate(CQFEditorTools.DutyLabel(this.node.duty) ?? "Null"), false))
            {
                CQFEditorTools.OpenDutySelect(d => this.node.duty = d);
            }
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_FocusTarget".Translate(), ref this.node.focusTarget, 5f, 120f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_FocusSecondTarget".Translate(), ref this.node.focusSecondTarget, 5f, 120f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_FocusThirdTarget".Translate(), ref this.node.focusThirdTarget, 5f, 120f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_Radius".Translate(), ref this.node.radius, ref this.radiusBuffer, 5f, 120f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_WanderRadius".Translate(), ref this.node.wanderRadius, ref this.wanderRadiusBuffer, 5f, 120f);
            y += 30f;
            if (Widgets.ButtonText(new Rect(5f, y, 300f, 25f), "CQF_LocomotionUrgency".Translate(this.node.locomotion.ToString()), false))
            {
                CQFEditorTools.DrawFloatMenu<LocomotionUrgency>(Enum.GetValues(typeof(LocomotionUrgency)).Cast<LocomotionUrgency>().ToList(), v => this.node.locomotion = v, v => v.ToString());
            }
            y += 30f;
            this.DrawActionSection(ref y, 5f, inRect.width - 20f, "CQF_EnterActions".Translate(), this.node.enterActions);
            this.DrawActionSection(ref y, 5f, inRect.width - 20f, "CQF_ExitActions".Translate(), this.node.exitActions);
        }

        private void DrawActionSection(ref float y, float x, float width, string label, List<CQFAction> actions)
        {
            this.DrawSectionHeader(ref y, x, width, label, () => this.OpenActionSelect(actions),
                () => CQFEditorTools.DrawFloatMenu(actions, action => actions.Remove(action), action => action.GetType().Name.Translate()));
            if (actions.Any())
            {
                foreach (CQFAction action in actions)
                {
                    Rect rowRect = new Rect(x + 6f, y, width - 12f, 28f);
                    if (Widgets.ButtonText(rowRect, action.GetType().Name.Translate(), false))
                    {
                        Find.WindowStack.Add(new Dialog_EditIDrawable(action));
                    }
                    y += 32f;
                }
            }
            else
            {
                Widgets.Label(new Rect(x + 10f, y + 2f, width - 20f, 25f), "CQF_DutyMapNoOptions".Translate().Colorize(Color.gray));
                y += 30f;
            }
            y += 8f;
        }

        private void DrawSectionHeader(ref float y, float x, float width, string label, Action addAction, Action removeAction)
        {
            Widgets.DrawHighlight(new Rect(x - 4f, y - 2f, width + 8f, 32f));
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, width - 90f, 30f), label.Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            Rect buttonRect = new Rect(x + width - 60f, y + 2f, 25f, 25f);
            if (Widgets.ButtonImage(buttonRect, TexButton.Plus))
            {
                addAction();
            }
            buttonRect.x += 30f;
            if (Widgets.ButtonImage(buttonRect, TexButton.Delete))
            {
                removeAction();
            }
            y += 38f;
        }

        private void OpenActionSelect(List<CQFAction> actions)
        {
            CQFEditorTools.DrawFloatMenu(typeof(CQFAction).AllSubclassesNonAbstract(), type =>
            {
                actions.Add((CQFAction)Activator.CreateInstance(type));
            }, type => type.Name.Translate());
        }

        private readonly DutyMapNode node;
        private Vector2 scrollPos = Vector2.zero;
        private float height = 620f;
        private string radiusBuffer;
        private string wanderRadiusBuffer;
    }
}
