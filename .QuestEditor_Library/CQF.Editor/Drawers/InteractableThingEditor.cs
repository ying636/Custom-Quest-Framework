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
            if (CQFAIButton.Draw(new Rect(500f, 0f, 30f, 30f))) CQFAIBridge.Open(CQFAIThingContext.Create(cqfReceiver));
            Rect outRect = new Rect(8f, 36f, 536f, 566f);
            Rect viewRect = new Rect(0f, 0f, 516f, cqfReceiver.height);
            Widgets.BeginScrollView(outRect, ref cqfReceiver.scrollPos, viewRect);
            float y = 12f;
            Rect copyAllRect = cqfReceiver.DrawSectionHeader(ref y, viewRect.width, "InteractionOperations".Translate(), true);
            if (Widgets.ButtonImage(copyAllRect, TexButton.Copy))
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
            Rect rect = new Rect(18f, y + 6f, 340f, 30f);
            for (int i = 0; i < cqfReceiver.operations.Count; i++)
            {
                InteractionOperation o = cqfReceiver.operations[i];
                rect.y = y + 6f;
                if (Widgets.ButtonText(rect, o.interactionText, false))
                {
                    Find.WindowStack.Add(new Dialog_InteractionOption(o, cqfReceiver));
                }

                TooltipHandler.TipRegion(rect, "CQF_ClickToEdit".Translate());
                if (Widgets.ButtonImage(new Rect(426f, y + 8f, 25f, 25f), TexButton.Copy))
                {
                    CQFEditorTools.operation = o.Copy();
                }

                TooltipHandler.TipRegion(new Rect(426f, y + 8f, 25f, 25f), "Copy".Translate());
                Rect save = new Rect(456f, y + 8f, 25f, 25f);
                if (Widgets.ButtonImage(save, ContentFinder<Texture2D>.Get("UI/Icon_MoveOut", true)))
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
                Widgets.Label(new Rect(16f, y + 4f, 420f, 25f), "CQF_NoInteractionOperations".Translate().Colorize(Color.gray));
                y += 34f;
            }

            Widgets.DrawBox(new Rect(10f, initY, width - 42f, Mathf.Max(42f, y - initY)), 1, QuestEditor_Dialog.blueTex);
            y += 10f;
            if (Widgets.ButtonText(new Rect(15f, y, 120f, 32f), "Add".Translate()))
            {
                cqfReceiver.operations.Add(new InteractionOperation());
            }

            if (Widgets.ButtonText(new Rect(155f, y, 120f, 32f), "Remove".Translate()) && cqfReceiver.operations.Any())
            {
                CQFEditorTools.DrawFloatMenu(cqfReceiver.operations, o => cqfReceiver.operations.Remove(o), o => o.interactionText);
            }

            if (Widgets.ButtonImage(new Rect(295f, y + 3f, 25f, 25f), TexButton.Paste) && CQFEditorTools.operation != null)
            {
                Find.WindowStack.Add(new Dialog_CQFInteractionPaste(cqfReceiver, new[] { CQFEditorTools.operation }));
            }

            TooltipHandler.TipRegion(new Rect(295f, y + 3f, 25f, 25f), "Paste".Translate());
            y += 42f;
        }

        public static void DrawOperationDefList_2(QuestEditor_Library.InteractableThing cqfReceiver, ref float y, float width)
        {
            cqfReceiver.DrawSimpleSectionTitle(ref y, width, "InteractionDataDefs".Translate());
            Rect rect = new Rect(18f, y + 6f, width - 90f, 28f);
            foreach (InteractionDataDef def in cqfReceiver.operationDefs)
            {
                rect.y = y + 6f;
                Widgets.Label(rect, def.label ?? def.defName);
                y += 36f;
            }

            if (!cqfReceiver.operationDefs.Any())
            {
                Widgets.Label(new Rect(16f, y + 4f, 420f, 25f), "CQF_NoInteractionDefs".Translate().Colorize(Color.gray));
                y += 34f;
            }

            y += 6f;
            if (Widgets.ButtonText(new Rect(15f, y, 120f, 32f), "Add".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<InteractionDataDef>.AllDefsListForReading, d => Find.WindowStack.Add(new Dialog_CQFInteractionPaste(cqfReceiver, Enumerable.Empty<InteractionOperation>(), new[] { d })), d => d.label);
            }

            if (Widgets.ButtonText(new Rect(155f, y, 120f, 32f), "Remove".Translate()) && cqfReceiver.operationDefs.Any())
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
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(15f, y, width - 35f, 30f), label.Colorize(ColorLibrary.SkyBlue));
            Rect copyRect = Rect.zero;
            if (drawCopyButton)
            {
                float labelWidth = Text.CalcSize(label).x;
                copyRect = new Rect(Mathf.Min(15f + labelWidth + 12f, width - 58f), y + 2f, 25f, 25f);
            }

            Text.Font = GameFont.Small;
            y += 32f;
            return copyRect;
        }

        public static void DrawSimpleSectionTitle_5(QuestEditor_Library.InteractableThing cqfReceiver, ref float y, float width, string label)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(15f, y, width - 35f, 30f), label.Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            y += 38f;
        }
    }
}
