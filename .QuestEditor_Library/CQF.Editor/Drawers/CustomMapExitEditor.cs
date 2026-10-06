using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public static class CustomMapExitEditor
    {
        public static void DrawTab_0(QuestEditor_Library.CustomMapExit cqfReceiver)
        {
            if (CQFAIButton.Draw(new Rect(500f, 0f, 30f, 30f))) CQFAIBridge.Open(CQFAIThingContext.Create(cqfReceiver));
            Rect outRect = new Rect(0f, 36f, 540f, 554f);
            float width = outRect.width - 40f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, cqfReceiver.height + 10f));
            Widgets.BeginScrollView(outRect, ref cqfReceiver.scrollPos, viewRect);
            float x = 10f;
            float y = 10f;
            cqfReceiver.DrawSectionHeader(ref y, x, width, "CQF_PortalSettingsSection".Translate(), "CQF_PortalSettingsSectionTip".Translate());
            CQFEditorTools.DrawLabelAndText_Line(y, "ExitName".Translate(), ref cqfReceiver.exitName, x + 8f, 150f);
            y += 30f;
            cqfReceiver.DrawActionSection(ref y, x, width);
            cqfReceiver.height = y + 10f;
            Widgets.EndScrollView();
        }

        public static void DrawActionSection_1(QuestEditor_Library.CustomMapExit cqfReceiver, ref float y, float x, float width)
        {
            cqfReceiver.DrawSectionHeader(ref y, x, width, "CQF_PortalEnterActions".Translate(), "CQF_PortalEnterActionsTip".Translate(), () => CQFEditorTools.OpenCQFActionSelect(type => cqfReceiver.enterActions.Add((CQFAction)Activator.CreateInstance(type))), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.enterActions, action => cqfReceiver.enterActions.Remove(action), action => action.GetType().Name.Translate()), cqfReceiver.enterActions.Any());
            if (cqfReceiver.enterActions.Any())
            {
                foreach (CQFAction action in cqfReceiver.enterActions)
                {
                    Rect rowRect = new Rect(x + 8f, y, width - 16f, 28f);
                    Widgets.DrawHighlightIfMouseover(rowRect);
                    if (Widgets.ButtonText(rowRect, action.GetType().Name.Translate(), false))
                    {
                        Find.WindowStack.Add(new Dialog_EditIDrawable(action));
                    }

                    y += 32f;
                }
            }
            else
            {
                Widgets.Label(new Rect(x + 8f, y + 4f, width - 16f, 25f), "CQF_PortalNoActions".Translate().Colorize(Color.gray));
                y += 32f;
            }

            y += 8f;
        }

        public static void DrawSectionHeader_2(QuestEditor_Library.CustomMapExit cqfReceiver, ref float y, float x, float width, string label, string tip = null, Action addAction = null, Action removeAction = null, bool canRemove = false)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Rect labelRect = new Rect(x + 8f, y + 4f, width - 84f, 25f);
            Widgets.Label(labelRect, label.Colorize(ColorLibrary.SkyBlue));
            if (!tip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(labelRect, tip);
            }

            if (addAction != null)
            {
                Rect buttonRect = new Rect(x + width - 66f, y + 2f, 25f, 25f);
                if (Widgets.ButtonImage(buttonRect, TexButton.Plus))
                {
                    addAction();
                }

                TooltipHandler.TipRegion(buttonRect, "Add".Translate());
                buttonRect.x += 30f;
                if (Widgets.ButtonImage(buttonRect, TexButton.Delete) && canRemove)
                {
                    removeAction?.Invoke();
                }

                TooltipHandler.TipRegion(buttonRect, "Remove".Translate());
            }

            y += 38f;
        }
    }
}
