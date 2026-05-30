using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_DialogManager : Page
    {
        public QuestEditor_DialogManager()
        {
            this.preventCameraMotion = false;
            this.absorbInputAroundWindow = false;
            this.doCloseX = true;
        }
        public override string PageTitle => "DialogManager".Translate().Colorize(ColorLibrary.SkyBlue);
        public DialogManagerDef Manager => manager;
        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            CQFEditorTools.DrawLabelAndText_Line(45f, "DialogManagerDefName".Translate(), ref this.Manager.defName, 5f, 300f);
            this.DrawButton();
            float y = 55f;
            Widgets.BeginScrollView(new Rect(5f, 75f, inRect.width - 10f, inRect.height - 83f), ref this.scrollPos,new Rect(0f,75f,inRect.width - 50f,this.height));
            y += 35f;
            this.Manager.Draw(ref y);
            Rect button = new Rect(10f, y, 100f, 30f);
            if (Widgets.ButtonText(button, "Add".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogTreeDef>.AllDefsListForReading, (x) =>
                {
                    this.Manager.trees.Add(new DialogTreeAndConditions(x, new List<DialogCondition>()));
                }, (x) => x.defName);
            }
            button.x += 110f;
            if (Widgets.ButtonText(button, "Delete".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(this.Manager.trees, (x) =>
                {
                    this.Manager.trees.Remove(x);
                }, (x) => x.tree.defName);
            }
            Widgets.EndScrollView();
            this.height = y;
        }
        public void DrawButton()
        {
            if (Widgets.ButtonText(new Rect(890f, 30f,90f, 30f), "Misc".Translate()))
            {
                Find.WindowStack.Add(new Dialog_DialogManagerMisc(this.Manager));
            }
            if (Widgets.ButtonText(new Rect(780f, 30f,90f, 30f), "LoadPremade".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, (x) =>
                {
                    manager = x;
                }, (x) => x.defName);
            }
            if (Widgets.ButtonText(new Rect(670f, 30f,90f, 30f), "Save".Translate()))
            {
                try
                {
                    string path = Page_QuestEditor.Path + @"\DialogTree\" + this.Manager.defName + ".xml";
                    XElement defs = new XElement("Defs");
                    XElement tree = this.Manager.SaveToXElement("QuestEditor_Library.DialogManagerDef");
                    defs.Add(tree);
                    defs.Save(path);
                    CQFQuestDefBootstrap.HotLoadDialogManagerDef(this.Manager);
                    Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                }
                catch (Exception e)
                {
                    Log.Error("Save error:" + e.Message);
                }
            }
            if (Widgets.ButtonText(new Rect(560f, 30f,90f, 30f), "ResetBinding".Translate()))
            {
                Dialog_MessageBox dialog = new Dialog_MessageBox("ConfirmCreateNewDialogTree".Translate());
                dialog.buttonBText = "Cancel".Translate();
                dialog.buttonBAction = () => dialog.Close();
                dialog.buttonAText = "Confirm".Translate();
                dialog.buttonAAction = () =>
                {
                    manager = new DialogManagerDef();
                    dialog.Close();
                };
                Find.WindowStack.Add(dialog);
            }
            if (Widgets.ButtonText(new Rect(450f,30f,90f, 30f), "DialogEditor".Translate()))
            {
                Find.WindowStack.Add(new QuestEditor_Dialog());
            }
        }

        public float height = 0f;
        public Vector2 scrollPos = Vector2.zero;
        public static DialogManagerDef manager = new DialogManagerDef();
    }
}
