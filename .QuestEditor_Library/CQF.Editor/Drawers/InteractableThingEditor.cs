using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.AI;
using System.Xml.Linq;
using System.Xml;

namespace QuestEditor_Library
{
    public static class InteractableThingEditor
    {
        public static void DrawTab_0(QuestEditor_Library.InteractableThing cqfReceiver)
        {
            using CQFUIScope scope = new CQFUIScope();
            Rect outRect = new Rect(8f, 36f, Mathf.Min(536f, CQFUIScope.ContentWidth - 16f), Mathf.Max(40f, Mathf.Min(566f, CQFUIScope.ContentHeight - 44f)));
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, cqfReceiver.height));
            Widgets.BeginScrollView(outRect, ref cqfReceiver.scrollPos, viewRect);
            using CQFUIScope contentScope = new CQFUIScope(viewRect.width);
            float y = 12f;
            Rect copyAllRect = cqfReceiver.DrawSectionHeader(ref y, viewRect.width, "InteractionOperations".Translate(), true);
            if (CQFUIStyle.ButtonImage(copyAllRect, TexButton.Copy))
            {
                CQFEditorTools.operations.Clear();
                CQFEditorTools.operationDefs.Clear();
                cqfReceiver.operations.ForEach(o => CQFEditorTools.operations.Add(o.Copy()));
                cqfReceiver.operationDefs.ForEach(o => CQFEditorTools.operationDefs.Add(o));
            }

            TooltipHandler.TipRegion(copyAllRect, "Copy".Translate());
            cqfReceiver.DrawOperationList(ref y, viewRect.width);
            y += 18f;
            cqfReceiver.DrawOperationDefList(ref y, viewRect.width);
            cqfReceiver.height = y + 10f;
            Widgets.EndScrollView();
        }

        public static void DrawOperationList_1(QuestEditor_Library.InteractableThing cqfReceiver, ref float y, float width)
        {
            float initY = y;
            Rect rect = new Rect(12f, y + 4f, Mathf.Max(40f, width - 100f), 32f);
            for (int i = 0; i < cqfReceiver.operations.Count; i++)
            {
                InteractionOperation o = cqfReceiver.operations[i];
                rect.y = y + 4f;
                if (CQFUIStyle.ButtonText(rect, o.interactionText, false))
                {
                    Find.WindowStack.Add(new Dialog_InteractionOption(o, cqfReceiver));
                }

                TooltipHandler.TipRegion(rect, "CQF_ClickToEdit".Translate());
                if (CQFUIStyle.ButtonImage(new Rect(width - 74f, y + 8f, 25f, 25f), TexButton.Copy))
                {
                    CQFEditorTools.operation = o.Copy();
                }

                TooltipHandler.TipRegion(new Rect(width - 74f, y + 8f, 25f, 25f), "Copy".Translate());
                Rect save = new Rect(width - 42f, y + 8f, 25f, 25f);
                if (CQFUIStyle.ButtonImage(save, ContentFinder<Texture2D>.Get("UI/Icon_MoveOut", true)))
                {
                    Find.WindowStack.Add(new Dialog_RenameForQE(name =>
                    {
                        LongEventHandler.QueueLongEvent(() =>
                        {
                            InteractionDataDef def = new InteractionDataDef();
                            def.defName = name;
                            def.label = o.interactionText;
                            def.interactions = new List<InteractionOperation>()
                            {
                                o
                            };
                            DefDatabase<InteractionDataDef>.Add(def);
                            string path = Path.Combine(CQFContentPaths.Quests, "Data", o.interactionText + ".xml");
                            XElement defs = new XElement("Defs");
                            XElement defXml = new XElement("QuestEditor_Library.InteractionDataDef");
                            XElement interactionDataDefXml = new XElement("interactions");
                            interactionDataDefXml.Add(o.SaveToXElement("li"));
                            defXml.Add(new XElement("defName", name));
                            defXml.Add(new XElement("label", o.interactionText));
                            defXml.Add(interactionDataDefXml);
                            defs.Add(defXml);
                            defs.Save(path);
                            Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                        }, "SavingAsDef".Translate(), true, e => Log.Message(e.Message));
                    }) { optionalTitle = "SetDefname".Translate() });
                }

                TooltipHandler.TipRegion(save, "SaveAsDef".Translate());
                y += 40f;
            };
            if (!cqfReceiver.operations.Any())
            {
                Widgets.Label(new Rect(12f, y + 4f, width - 24f, 28f), "CQF_NoInteractionOperations".Translate().Colorize(CQFUIStyle.Muted));
                y += 34f;
            }

            CQFUIStyle.DrawBox(new Rect(8f, initY, width - 16f, Mathf.Max(36f, y - initY)));
            y += 10f;
            float buttonWidth = Mathf.Min(120f, (width - 68f) / 2f);
            if (CQFUIStyle.ButtonText(new Rect(12f, y, buttonWidth, 32f), "Add".Translate()))
            {
                cqfReceiver.operations.Add(new InteractionOperation());
            }

            if (CQFUIStyle.ButtonText(new Rect(20f + buttonWidth, y, buttonWidth, 32f), "Remove".Translate()) && cqfReceiver.operations.Any())
            {
                CQFEditorTools.DrawFloatMenu(cqfReceiver.operations, o => cqfReceiver.operations.Remove(o), o => o.interactionText);
            }

            if (CQFUIStyle.ButtonImage(new Rect(28f + buttonWidth * 2f, y + 3f, 25f, 25f), TexButton.Paste) && CQFEditorTools.operation != null)
            {
                Find.WindowStack.Add(new Dialog_CQFInteractionPaste(cqfReceiver, new[] { CQFEditorTools.operation }));
            }

            TooltipHandler.TipRegion(new Rect(28f + buttonWidth * 2f, y + 3f, 25f, 25f), "Paste".Translate());
            y += 42f;
        }

        public static void DrawOperationDefList_2(QuestEditor_Library.InteractableThing cqfReceiver, ref float y, float width)
        {
            cqfReceiver.DrawSimpleSectionTitle(ref y, width, "InteractionDataDefs".Translate());
            Rect rect = new Rect(12f, y + 4f, width - 24f, 32f);
            foreach (InteractionDataDef def in cqfReceiver.operationDefs)
            {
                rect.y = y + 4f;
                Widgets.Label(rect, def.label ?? def.defName);
                y += 36f;
            }

            if (!cqfReceiver.operationDefs.Any())
            {
                Widgets.Label(new Rect(12f, y + 4f, width - 24f, 28f), "CQF_NoInteractionDefs".Translate().Colorize(CQFUIStyle.Muted));
                y += 34f;
            }

            y += 6f;
            float buttonWidth = Mathf.Min(120f, (width - 68f) / 2f);
            if (CQFUIStyle.ButtonText(new Rect(12f, y, buttonWidth, 32f), "Add".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<InteractionDataDef>.AllDefsListForReading, d => Find.WindowStack.Add(new Dialog_CQFInteractionPaste(cqfReceiver, Enumerable.Empty<InteractionOperation>(), new[] { d })), d => d.label);
            }

            if (CQFUIStyle.ButtonText(new Rect(20f + buttonWidth, y, buttonWidth, 32f), "Remove".Translate()) && cqfReceiver.operationDefs.Any())
            {
                CQFEditorTools.DrawFloatMenu(cqfReceiver.operationDefs, d => cqfReceiver.operationDefs.Remove(d), d => d.label);
            }

            y += 42f;
        }

        public static void PasteData_3(QuestEditor_Library.InteractableThing cqfReceiver)
        {
            Find.WindowStack.Add(new Dialog_CQFInteractionPaste(cqfReceiver, CQFEditorTools.operations, CQFEditorTools.operationDefs));
        }

        public static Rect DrawSectionHeader_4(QuestEditor_Library.InteractableThing cqfReceiver, ref float y, float width, string label, bool drawCopyButton = false)
        {
            using CQFUIScope scope = new CQFUIScope();
            Widgets.Label(new Rect(12f, y, width - (drawCopyButton ? 62f : 24f), 32f), label.Colorize(CQFUIStyle.Accent));
            Rect copyRect = Rect.zero;
            if (drawCopyButton)
            {
                copyRect = new Rect(width - 42f, y + 2f, 25f, 25f);
            }

            Text.Font = GameFont.Small;
            y += 32f;
            return copyRect;
        }

        public static void DrawSimpleSectionTitle_5(QuestEditor_Library.InteractableThing cqfReceiver, ref float y, float width, string label)
        {
            using CQFUIScope scope = new CQFUIScope();
            Widgets.Label(new Rect(12f, y, width - 24f, 32f), label.Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            y += 38f;
        }
    }
}
