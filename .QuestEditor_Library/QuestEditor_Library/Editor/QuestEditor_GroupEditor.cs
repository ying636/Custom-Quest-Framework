using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_GroupEditor : Page
    {
        public override string PageTitle => "GroupEditor".Translate().Colorize(ColorLibrary.SkyBlue);
        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            if (Widgets.CloseButtonFor(inRect))
            {
                this.Close();
            }
            GroupDataDef data = QuestEditor_GroupEditor.data;
            this.DrawMisc();
            float y = 50f;
            float x = 5f;
            Widgets.BeginScrollView(new Rect(4f, 40f, inRect.width - 8f, inRect.height - 83f), ref this.scrollPos, new Rect(0f, 40f, inRect.width - 32f, height));
            CQFEditorTools.DrawLabelAndText_Line(y, "LootBoxName".Translate(), ref data.defName, x, 150f);
            y += 30f;
            data.lord.Draw(ref y, inRect, x);
            float y2 = y;
            y += 4f;
            CQFEditorTools.DrawPawnDataList_UseWindow_UseIcon(ref y, x + 3f, data.pawns, inRect, "PawnSpawnDatas".Translate().Colorize(ColorLibrary.SkyBlue), p => p.dataName);
            Widgets.DrawBox(new Rect(x - 5f,y2,inRect.width - 35f,y - y2),1,QuestEditor_Dialog.blueTex);
            Widgets.EndScrollView();
            height = y;
        }

        public void DrawMisc() 
        {
            float y = 20f;
            if (Widgets.ButtonText(new Rect(780f, y, 90f, 30f), "LoadPremade".Translate()))
            {
                List<GroupDataDef> groups = new List<GroupDataDef>();
                groups.AddRange(DefDatabase<GroupDataDef>.AllDefsListForReading);
                groups.AddRange(CQFEditorTools.GetObject<GroupDataDef>(Page_QuestEditor.Path + @"\Group\", "//QuestEditor_Library.GroupDataDef"));
                CQFEditorTools.DrawFloatMenu<GroupDataDef>(groups, (x) =>
                {
                    QuestEditor_GroupEditor.data = x;
                    QuestEditor_GroupEditor.data.lord.Data.lordData = QuestEditor_GroupEditor.data.lord;
                }, (x) => x.defName);
            }
            if (Widgets.ButtonText(new Rect(670f, y, 90f, 30f), "Save".Translate()))
            {
                try
                {
                    string path = Page_QuestEditor.Path + @"\Group\" + QuestEditor_GroupEditor.data.defName + ".xml";
                    XElement defs = new XElement("Defs");
                    XElement tree = QuestEditor_GroupEditor.data.SaveToXElement("QuestEditor_Library.GroupDataDef");
                    defs.Add(tree);
                    defs.Save(path);
                    if (!DefDatabase<GroupDataDef>.AllDefsListForReading.Exists(d => d.defName == QuestEditor_GroupEditor.data.defName)) 
                    {
                        DefDatabase<GroupDataDef>.Add(QuestEditor_GroupEditor.data);
                    }
                    Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                }
                catch (Exception e)
                {
                    Log.Error("Save error:" + e.Message);
                }
            }
            if (Widgets.ButtonText(new Rect(560f, y, 90f, 30f), "ResetBinding".Translate()))
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
        float height;
    }
}
