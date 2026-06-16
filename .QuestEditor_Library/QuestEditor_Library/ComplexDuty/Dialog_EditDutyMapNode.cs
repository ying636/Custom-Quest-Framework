using System;
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
            CQFEditorTools.DrawActionList_UseWindow(ref y, 5f, this.node.enterActions, inRect, "CQF_EnterActions".Translate(), a => a.GetType().Name.Translate());
            CQFEditorTools.DrawActionList_UseWindow(ref y, 5f, this.node.exitActions, inRect, "CQF_ExitActions".Translate(), a => a.GetType().Name.Translate());
        }

        private readonly DutyMapNode node;
        private Vector2 scrollPos = Vector2.zero;
        private float height = 620f;
        private string radiusBuffer;
        private string wanderRadiusBuffer;
    }
}
