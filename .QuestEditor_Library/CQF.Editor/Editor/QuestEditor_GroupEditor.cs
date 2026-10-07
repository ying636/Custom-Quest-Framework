using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_GroupEditor : Page, ICQFAIEditorHost
    {
        public override string PageTitle => "GroupEditor".Translate().Colorize(CQFUIStyle.Accent);
        public CQFAIEditorContext AIContext => new CQFAIEditorContext(data.defName, () => data, value =>
        {
            data = (GroupDataDef)value;
            data.lord.Data.lordData = data.lord;
        }, isValid: () => Find.WindowStack.Windows.Contains(this), owner: this);
        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            base.DrawPageTitle(inRect);
            if (Widgets.CloseButtonFor(inRect))
            {
                this.Close();
            }
            this.DrawMisc(inRect);
            Rect main = new Rect(0f, 88f, inRect.width, Mathf.Max(80f, inRect.height - 96f));
            bool columns = main.width >= 780f;
            float leftWidth = columns ? Mathf.Min(520f, main.width * 0.48f) : main.width;
            Rect left = new Rect(main.x, main.y, leftWidth, main.height);
            CQFUIStyle.DrawMenuSection(left);
            Rect viewport = left.ContractedBy(12f);
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(height, viewport.height));
            Widgets.BeginScrollView(viewport, ref scrollPos, content);
            using CQFUIScope cqfContentScope1 = new CQFUIScope(content.width);
            using (new CQFUIScope(content.width))
            {
                GroupDataDef current = QuestEditor_GroupEditor.data;
                float y = 4f;
                CQFEditorTools.DrawLabelAndText_Line(y, "LootBoxName".Translate(), ref current.defName, 0f, content.width - 176f);
                y += 36f;
                current.lord.Draw(ref y, content, 0f);
                if (!columns)
                {
                    y += 12f;
                    CQFEditorTools.DrawPawnDataList_UseWindow_UseIcon(ref y, 0f, current.pawns, content, "PawnSpawnDatas".Translate(), pawn => pawn.dataName);
                }
                height = y + 12f;
            }
            Widgets.EndScrollView();
            if (columns)
            {
                Rect right = new Rect(left.xMax + 12f, main.y, main.width - left.width - 12f, main.height);
                CQFUIStyle.DrawMenuSection(right);
                Rect pawnViewport = right.ContractedBy(12f);
                Rect pawnContent = new Rect(0f, 0f, pawnViewport.width - 20f, Mathf.Max(pawnHeight, pawnViewport.height));
                Widgets.BeginScrollView(pawnViewport, ref pawnScrollPos, pawnContent);
                using (new CQFUIScope(pawnContent.width))
                {
                    float y = 4f;
                    CQFEditorTools.DrawPawnDataList_UseWindow_UseIcon(ref y, 0f, data.pawns, pawnContent, "PawnSpawnDatas".Translate(), pawn => pawn.dataName);
                    pawnHeight = y + 12f;
                }
                Widgets.EndScrollView();
            }
        }

        public void DrawMisc(Rect inRect)
        {
            float y = 44f;
            float width = Mathf.Min(180f, (inRect.width - 16f) / 3f);
            float start = Mathf.Max(0f, inRect.width - width * 3f - 16f);
            if (CQFUIStyle.ButtonText(new Rect(start + (width + 8f) * 2f, y, width, 32f), "LoadPremade".Translate()))
            {
                List<GroupDataDef> groups = new List<GroupDataDef>();
                groups.AddRange(DefDatabase<GroupDataDef>.AllDefsListForReading);
                groups.AddRange(CQFEditorTools.GetObject<GroupDataDef>(Path.Combine(CQFContentPaths.Quests, "Group"), "//QuestEditor_Library.GroupDataDef"));
                CQFEditorTools.DrawFloatMenu<GroupDataDef>(groups, (x) =>
                {
                    QuestEditor_GroupEditor.data = x;
                    QuestEditor_GroupEditor.data.lord.Data.lordData = QuestEditor_GroupEditor.data.lord;
                }, (x) => x.defName);
            }
            if (CQFUIStyle.ButtonText(new Rect(start + width + 8f, y, width, 32f), "Save".Translate()))
            {
                try
                {
                    string path = Path.Combine(CQFContentPaths.Quests, "Group", QuestEditor_GroupEditor.data.defName + ".xml");
                    XElement defs = new XElement("Defs");
                    XElement tree = QuestEditor_GroupEditor.data.SaveToXElement("QuestEditor_Library.GroupDataDef");
                    defs.Add(tree);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    defs.Save(path);
                    if (!DefDatabase<GroupDataDef>.AllDefsListForReading.Exists(d => d.defName == QuestEditor_GroupEditor.data.defName))
                    {
                        DefDatabase<GroupDataDef>.Add(QuestEditor_GroupEditor.data);
                    }
                    Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                }
                catch (Exception e)
                {
                    Log.Error("Save error:" + e);
                }
            }
            if (CQFUIStyle.ButtonText(new Rect(start, y, width, 32f), "ResetBinding".Translate()))
            {
                Dialog_MessageBox dialog = new Dialog_MessageBox("ConfirmCreateNewDialogTree".Translate());
                dialog.buttonBText = "Cancel".Translate();
                dialog.buttonBAction = () => dialog.Close();
                dialog.buttonAText = "Confirm".Translate();
                dialog.buttonAAction = () =>
                {
                    QuestEditor_GroupEditor.data = new GroupDataDef();
                    dialog.Close();
                };
                Find.WindowStack.Add(dialog);
            }
        }

        public static GroupDataDef data = new GroupDataDef();
        public Vector2 scrollPos = Vector2.zero;
        private float height;
        private float pawnHeight;
        private Vector2 pawnScrollPos;
    }
}
