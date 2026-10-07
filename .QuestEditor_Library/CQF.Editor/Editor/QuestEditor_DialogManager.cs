using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_DialogManager : Page, ICQFAIEditorHost
    {
        public QuestEditor_DialogManager()
        {
            this.preventCameraMotion = false;
            this.absorbInputAroundWindow = false;
            this.doCloseX = true;
        }
        public override string PageTitle => "DialogManager".Translate().Colorize(CQFUIStyle.Accent);
        public DialogManagerDef Manager => manager;
        public CQFAIEditorContext AIContext => new CQFAIEditorContext(this.Manager.defName, () => this.Manager,
            value => { QuestEditor_DialogManager.manager = (DialogManagerDef)value; }, isValid: () => Find.WindowStack.Windows.Contains(this), owner: this);
        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            base.DrawPageTitle(inRect);
            this.DrawButton();
            Widgets.Label(new Rect(0f, 86f, inRect.width, 24f), "DialogManagerDefName".Translate().Colorize(CQFUIStyle.Accent));
            this.Manager.defName = Widgets.TextField(new Rect(0f, 112f, inRect.width, 32f), this.Manager.defName ?? string.Empty);
            Rect viewport = new Rect(0f, 156f, inRect.width, inRect.height - 156f);
            float width = viewport.width - 20f;
            float y = 0f;
            Widgets.BeginScrollView(viewport, ref this.scrollPos, new Rect(0f, 0f, width, Mathf.Max(viewport.height, this.height)));
            DialogManagerDefEditor.DrawEntries(this.Manager, ref y, width);
            Rect button = new Rect(0f, y, width, 32f);
            if (CQFUIStyle.ButtonText(button, "Add".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogTreeDef>.AllDefsListForReading, (x) =>
                {
                    this.Manager.trees.Add(new DialogTreeAndConditions(x, new List<DialogCondition>()));
                }, (x) => x.defName);
            }
            Widgets.EndScrollView();
            this.height = y + 44f;
        }
        public void DrawButton()
        {
            if (CQFUIStyle.ButtonImage(new Rect(408f, 44f, 28f, 28f), ContentFinder<Texture2D>.Get("UI/Icon_Edit"), tooltip: "Misc".Translate()))
            {
                Find.WindowStack.Add(new Dialog_DialogManagerMisc(this.Manager));
            }
            if (CQFUIStyle.ButtonText(new Rect(104f, 42f, 96f, 32f), "LoadPremade".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, (x) =>
                {
                    manager = x;
                }, (x) => x.defName);
            }
            if (CQFUIStyle.ButtonImage(new Rect(372f, 44f, 28f, 28f), TexButton.Save, tooltip: "Save".Translate()))
            {
                try
                {
                    string path = Path.Combine(CQFContentPaths.Quests, "DialogTree", this.Manager.defName + ".xml");
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                    XElement defs = new XElement("Defs");
                    XElement tree = this.Manager.SaveToXElement("QuestEditor_Library.DialogManagerDef");
                    defs.Add(tree);
                    defs.Save(path);
                    CQFQuestDefBootstrap.HotLoadDialogManagerDef(this.Manager);
                    Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                }
                catch (Exception e)
                {
                    Log.Error("Save error:" + e);
                }
            }
            if (CQFUIStyle.ButtonText(new Rect(0f, 42f, 96f, 32f), "ResetBinding".Translate()))
            {
                Dialog_MessageBox dialog = new Dialog_MessageBox("ConfirmCreateNewDialogTree".Translate());
                dialog.buttonBText = "Cancel".Translate();
                dialog.buttonBAction = () => dialog.Close();
                dialog.buttonAText = "Confirm".Translate();
                dialog.buttonAAction = () =>
                {
                    manager = new DialogManagerDef { defName = "CQF_DialogManager_" + Guid.NewGuid().ToString("N").Substring(0, 8) };
                    dialog.Close();
                };
                Find.WindowStack.Add(dialog);
            }
            if (CQFUIStyle.ButtonText(new Rect(208f, 42f, 152f, 32f), "DialogEditor".Translate()))
            {
                Find.WindowStack.Add(new QuestEditor_Dialog());
            }
        }

        public float height = 0f;
        public Vector2 scrollPos = Vector2.zero;
        public static DialogManagerDef manager = new DialogManagerDef();
    }
}
