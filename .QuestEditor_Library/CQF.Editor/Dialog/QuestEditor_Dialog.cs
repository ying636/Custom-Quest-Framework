using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using UnityEvent = UnityEngine.Event;

namespace QuestEditor_Library
{
    [StaticConstructorOnStartup]
    public class QuestEditor_Dialog : Page, ICQFAIEditorHost
    {
        public QuestEditor_Dialog()
        {
            this.canvas = new CQFDialogNodeCanvas(this);
            this.preventCameraMotion = true;
            this.absorbInputAroundWindow = true;
            this.doCloseX = true;
            this.InitCurTree();
            this.session.Reset(this.CurTree);
        }

        public override string PageTitle => "DialogEditor".Translate().Colorize(ColorLibrary.SkyBlue);
        public override Vector2 InitialSize => new Vector2(Mathf.Min(UI.screenWidth - 40f, 1520f), Mathf.Min(UI.screenHeight - 60f, 940f));
        public int? SelectedNodeIndex => this.canvas.SelectedIndex;
        public CQFDialogEditSession Session => this.session;
        public CQFAIEditorContext AIContext => new CQFAIEditorContext(this.CurTree.defName, () => this.CurTree, value =>
        {
            this.CloseEditors();
            this.RecordChanges();
            tree = (DialogTreeDef)value;
            this.InitCurTree();
            this.session.Observe(tree);
        }, value => new CQFDialogPatch(this.session).Validate((DialogTreeDef)value), () => Find.WindowStack.Windows.Contains(this), this);
        public DialogTreeDef CurTree
        {
            get => tree;
            set
            {
                this.CloseEditors();
                tree = value;
                this.canvas.Reset();
                this.showInspector = false;
                this.inspector = null;
                this.selectedNode = null;
                this.selectedOption = null;
                this.selectedResult = null;
                this.InitCurTree();
                this.session.Reset(value);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, 260f, 35f), this.PageTitle);
            Text.Font = GameFont.Small;
            Rect treeInfo = new Rect(270f, 7f, Mathf.Max(0f, inRect.width - 310f), 26f);
            Widgets.Label(treeInfo, this.CurTree.defName + "  ·  " + this.CurTree.title);
            TooltipHandler.TipRegion(treeInfo, this.CurTree.defName + "\n" + this.CurTree.title);
            this.DrawButton(inRect);
            float inspectorWidth = this.showInspector ? Mathf.Min(380f, inRect.width * 0.36f) : 0f;
            Rect canvasRect = new Rect(0f, 84f, inRect.width - (this.showInspector ? inspectorWidth + 10f : 0f), inRect.height - 84f);
            this.canvas.Draw(canvasRect);
            if (this.showInspector)
            {
                Rect inspectorRect = new Rect(canvasRect.xMax + 10f, 84f, inspectorWidth, canvasRect.height);
                Widgets.DrawBoxSolid(inspectorRect, new Color(0.1f, 0.14f, 0.12f));
                GUI.BeginGroup(inspectorRect.ContractedBy(6f));
                this.DrawInspector(new Rect(0f, 0f, inspectorWidth - 12f, canvasRect.height - 12f));
                GUI.EndGroup();
            }
            if (UnityEvent.current.type != EventType.Repaint && UnityEvent.current.type != EventType.Layout)
            {
                this.RecordChanges();
            }
        }

        public override void PostClose()
        {
            this.CloseEditors();
            base.PostClose();
        }

        public void InitCurTree()
        {
            this.CurTree.Update();
            bool hasPositions = this.CurTree.nodeMoulds.Values.Any(node => node.editorPositionSet);
            this.canvas.EnsurePositions();
            if (this.selectedNode != null && !this.CurTree.nodeMoulds.Values.Contains(this.selectedNode))
            {
                this.selectedNode = null;
                this.selectedOption = null;
                this.selectedResult = null;
                this.inspector = null;
            }
            else if (this.selectedNode != null && this.selectedOption != null && !this.selectedNode.options.Contains(this.selectedOption))
            {
                this.SelectNode(this.selectedNode);
            }
            else if (this.selectedNode != null && this.selectedOption != null && this.selectedResult != null && !this.selectedOption.results.Contains(this.selectedResult))
            {
                this.SelectOption(this.selectedNode, this.selectedOption);
            }
            if (!hasPositions)
            {
                this.canvas.Arrange();
            }
            if (this.initialized)
            {
                this.RecordChanges();
            }
            this.initialized = true;
        }

        public void RecordChanges()
        {
            if (this.initialized)
            {
                try
                {
                    this.session.Observe(this.CurTree);
                    this.lastHistoryError = null;
                }
                catch (Exception error)
                {
                    if (this.lastHistoryError != error.Message)
                    {
                        this.lastHistoryError = error.Message;
                        this.ReportError(error, "history");
                    }
                }
            }
        }

        public void SelectNode(DialogNode node, bool showProperties = true)
        {
            if (this.selectedNode != node || this.selectedOption != null)
            {
                if (showProperties) this.showInspector = true;
                this.selectedNode = node;
                this.selectedOption = null;
                this.selectedResult = null;
                this.inspector = new Dialog_EditDialogNode(node, this);
            }
        }

        public void SelectOption(DialogNode node, DialogOption option)
        {
            this.showInspector = true;
            this.selectedNode = node;
            this.selectedOption = option;
            this.selectedResult = null;
            this.inspector = new Dialog_EditDialogOption(this, option, node);
        }

        public void SelectResult(DialogNode node, DialogOption option, DialogResult result)
        {
            this.showInspector = true;
            this.selectedNode = node;
            this.selectedOption = option;
            this.selectedResult = result;
            this.inspector = new Dialog_EditDialogResult(this, result, option, node);
        }

        public void AddOption(DialogNode node)
        {
            DialogOption option = new DialogOption { text = "CQF_Dialog_Node_" + node.index + "_Option_" + node.options.Count };
            node.options.Add(option);
            this.SelectOption(node, option);
            this.RecordChanges();
        }

        public void RemoveNode(DialogNode node)
        {
            if (node.index == 0)
            {
                Messages.Message("CQF_DialogGraph_KeepEntry".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            foreach (DialogResult result in this.CurTree.nodeMoulds.Values.SelectMany(current => current.options).SelectMany(option => option.results))
            {
                if (result.nextIndex == node.index)
                {
                    result.nextIndex = null;
                }
            }
            this.CurTree.nodeMoulds.Remove(node.index.GetValueOrDefault());
            this.selectedNode = null;
            this.selectedOption = null;
            this.selectedResult = null;
            this.inspector = null;
            this.InitCurTree();
        }

        public void ApplyDraft(DialogTreeDef draft)
        {
            this.RecordChanges();
            this.CloseEditors();
            tree = draft;
            this.canvas.Reset();
            this.showInspector = false;
            this.inspector = null;
            this.selectedNode = null;
            this.selectedOption = null;
            this.selectedResult = null;
            this.InitCurTree();
        }

        public void DrawButton(Rect inRect)
        {
            string[] labels = { "CQF_DialogGraph_File", "CQF_DialogGraph_View", "CQF_DialogGraph_Undo", "CQF_DialogGraph_Redo",
                this.showInspector ? "CQF_DialogGraph_HideInspector" : "CQF_DialogGraph_ShowInspector", "CQF_DialogGraph_Preview", "Save" };
            float buttonWidth = Mathf.Min(140f, (inRect.width - 76f) / labels.Length);
            for (int i = 0; i < labels.Length; i++)
            {
                bool enabled = GUI.enabled;
                if (i == 2) GUI.enabled = enabled && this.session.CanUndo;
                if (i == 3) GUI.enabled = enabled && this.session.CanRedo;
                bool clicked = Widgets.ButtonText(new Rect(i * (buttonWidth + 4f), 45f, buttonWidth, 32f), labels[i].Translate());
                GUI.enabled = enabled;
                if (!clicked) continue;
                switch (i)
                {
                    case 0:
                        this.ShowFileMenu();
                        break;
                    case 1:
                        this.ShowViewMenu();
                        break;
                    case 2:
                        this.RestoreHistory(false);
                        break;
                    case 3:
                        this.RestoreHistory(true);
                        break;
                    case 4:
                        this.showInspector = !this.showInspector;
                        break;
                    case 5:
                        Find.WindowStack.Add(new CQFDialogPreviewWindow(this.session.Copy(this.CurTree), this.SelectedNodeIndex ?? 0));
                        break;
                    case 6:
                        this.SaveTree();
                        break;
                }
            }
            if (CQFAIButton.Draw(new Rect(labels.Length * (buttonWidth + 4f), 45f, 32f, 32f))) CQFAIBridge.Open(this.AIContext);
        }

        private void ShowFileMenu()
        {
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("LoadPremade".Translate(), () => CQFEditorTools.DrawFloatMenu(
                    DefDatabase<DialogTreeDef>.AllDefsListForReading, value => this.CurTree = value, value => value.defName)),
                new FloatMenuOption("ResetBinding".Translate(), () => Find.WindowStack.Add(new Dialog_MessageBox(
                    "ConfirmCreateNewDialogTree".Translate(), "Confirm".Translate(),
                    () => this.CurTree = new DialogTreeDef { defName = "CQF_DialogTree_" + Guid.NewGuid().ToString("N").Substring(0, 8) }, "Cancel".Translate()))),
                new FloatMenuOption("CQF_DialogGraph_TreeSettings".Translate(), () => Find.WindowStack.Add(new QuestEditor_DialogTreeMisc(this.CurTree))),
                new FloatMenuOption("CQF_DialogGraph_Check".Translate(), this.CheckTree)
            }));
        }

        private void ShowViewMenu()
        {
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_DialogGraph_Arrange".Translate(), this.canvas.Arrange),
                new FloatMenuOption("CQF_DialogGraph_Fit".Translate(), this.canvas.Fit),
                new FloatMenuOption("CQF_DialogGraph_Find".Translate(), () => Find.WindowStack.Add(new FloatMenu(
                    this.CurTree.nodeMoulds.OrderBy(pair => pair.Key).Select(pair => new FloatMenuOption(
                        "#" + pair.Key + " " + pair.Value.text, () => this.canvas.Focus(pair.Key))).ToList())))
            }));
        }

        private void CheckTree()
        {
            try
            {
                new CQFDialogPatch(this.session).Validate(this.CurTree);
                Messages.Message("CQF_DialogGraph_Valid".Translate(this.CurTree.nodeMoulds.Count, this.CurTree.idleNodes.Count), MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception error)
            {
                this.ReportError(error, "validation");
            }
        }

        private void DrawInspector(Rect rect)
        {
            if (this.selectedNode == null)
            {
                Widgets.Label(new Rect(8f, 8f, rect.width - 16f, 100f), "CQF_DialogGraph_SelectHint".Translate());
                return;
            }
            Widgets.Label(new Rect(6f, 5f, rect.width - 116f, 28f), "CQF_DialogGraph_Node".Translate(this.selectedNode.index.GetValueOrDefault()));
            if (Widgets.ButtonText(new Rect(rect.width - 106f, 5f, 100f, 26f), "CQF_DialogGraph_Expand".Translate()))
            {
                if (this.selectedResult != null && this.selectedOption != null) Find.WindowStack.Add(new Dialog_EditDialogResult(this, this.selectedResult, this.selectedOption, this.selectedNode));
                else if (this.selectedOption != null) Find.WindowStack.Add(new Dialog_EditDialogOption(this, this.selectedOption, this.selectedNode));
                else Find.WindowStack.Add(new Dialog_EditDialogNode(this.selectedNode, this));
            }
            float buttonWidth = (rect.width - 16f) / 3f;
            if (Widgets.ButtonText(new Rect(4f, 36f, buttonWidth, 26f), "DialogText".Translate())) this.SelectNode(this.selectedNode);
            if (Widgets.ButtonText(new Rect(buttonWidth + 8f, 36f, buttonWidth, 26f), "Add".Translate())) this.AddOption(this.selectedNode);
            if (Widgets.ButtonText(new Rect(buttonWidth * 2f + 12f, 36f, buttonWidth, 26f), "Remove".Translate()))
            {
                if (this.selectedResult != null && this.selectedOption != null)
                {
                    this.selectedOption.results.Remove(this.selectedResult);
                    this.SelectOption(this.selectedNode, this.selectedOption);
                    this.InitCurTree();
                }
                else if (this.selectedOption != null)
                {
                    this.selectedNode.options.Remove(this.selectedOption);
                    this.SelectNode(this.selectedNode);
                    this.InitCurTree();
                }
                else
                {
                    this.RemoveNode(this.selectedNode);
                }
            }
            if (this.inspector != null)
            {
                Rect contents = new Rect(0f, 70f, rect.width, rect.height - 70f);
                GUI.BeginGroup(contents);
                this.inspector.DoWindowContents(new Rect(Vector2.zero, contents.size));
                GUI.EndGroup();
            }
        }

        private void RestoreHistory(bool redo)
        {
            this.CloseEditors();
            tree = redo ? this.session.Redo(this.CurTree) : this.session.Undo(this.CurTree);
            this.inspector = null;
            this.selectedNode = null;
            this.selectedOption = null;
            this.selectedResult = null;
            this.canvas.Reset();
            this.showInspector = false;
            this.InitCurTree();
        }

        private void CloseEditors()
        {
            foreach (Window window in Find.WindowStack.Windows.ToList())
            {
                if (window is Dialog_EditDialogNode nodeEditor && nodeEditor.parent == this
                    || window is Dialog_EditDialogOption optionEditor && optionEditor.parent == this
                    || window is Dialog_EditDialogResult resultEditor && resultEditor.parent == this
                    || window is QuestEditor_DialogTreeMisc miscEditor && miscEditor.def == this.CurTree
                    || window is Dialog_EditExtraText extraEditor && this.CurTree.nodeMoulds.Values.Contains(extraEditor.node))
                {
                    window.Close();
                }
            }
        }

        private void SaveTree()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.CurTree.defName) || this.CurTree.defName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    throw new InvalidOperationException("CQF_DialogGraph_InvalidName".Translate());
                }
                XmlConvert.VerifyNCName(this.CurTree.defName);
                new CQFDialogPatch(this.session).Validate(this.CurTree);
                string directory = Path.Combine(CQFContentPaths.Quests, "DialogTree");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, this.CurTree.defName + ".xml");
                bool compile = CustomQuestFramework_ModSetting.setting?.autoCompileDialogTextKey ?? true;
                XElement language = new XElement("LanguageData");
                XElement document = compile ? this.BuildCompiledTreeXml(out language) : this.CurTree.SaveToXElement("QuestEditor_Library.DialogTreeDef");
                new XElement("Defs", document).Save(path);
                if (compile) this.SaveCompiledLanguageFile(language);
                CQFQuestDefBootstrap.HotLoadDialogTreeDef(this.CurTree);
                Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception error)
            {
                this.ReportError(error, "save");
            }
        }

        private void ReportError(Exception error, string context)
        {
            string key = error.Message.Split(new[] { ':', ' ' }, 2)[0];
            string message = key.CanTranslate() ? key.Translate() + error.Message.Substring(key.Length) : error.Message;
            Log.Error("CQF dialog " + context + ": " + error);
            Messages.Message("CQF_DialogGraph_Error".Translate(message), MessageTypeDefOf.RejectInput);
        }

        private XElement BuildCompiledTreeXml(out XElement language)
        {
            XElement result = new XElement(this.CurTree.SaveToXElement("QuestEditor_Library.DialogTreeDef"));
            language = new XElement("LanguageData");
            this.CompileTextElement(result.Element("title"), this.MakeTextKey("Title"), language);
            this.CompileTextElement(result.Element("dialogReportKey"), this.MakeTextKey("Report"), language);
            XElement idleNodes = result.Element("idleNodes");
            if (idleNodes != null)
            {
                foreach (XElement idleNode in idleNodes.Elements("li"))
                {
                    this.CompileNodeElement(idleNode, language);
                }
            }
            XElement nodeMoulds = result.Element("nodeMoulds");
            if (nodeMoulds != null)
            {
                foreach (XElement nodeEntry in nodeMoulds.Elements("li"))
                {
                    XElement nodeKey = nodeEntry.Element("key");
                    XElement nodeValue = nodeEntry.Element("value");
                    if (nodeKey == null || nodeValue == null || !int.TryParse(nodeKey.Value, out int nodeIndex))
                    {
                        continue;
                    }
                    this.CompileNodeElement(nodeValue, language, nodeIndex);
                }
            }
            return result;
        }

        private void CompileNodeElement(XElement node, XElement language, int? nodeIndex = null)
        {
            if (node == null)
            {
                return;
            }
            int index = nodeIndex ?? (int.TryParse(node.Element("index")?.Value, out int parsedIndex) ? parsedIndex : -1);
            if (index < 0)
            {
                return;
            }
            string nodePrefix = this.MakeTextKey("Node_" + index);
            this.CompileTextElement(node.Element("text"), nodePrefix + "_Text", language);
            XElement extraText = node.Element("extraText");
            if (extraText != null)
            {
                int extraIndex = 0;
                foreach (XElement extra in extraText.Elements("li"))
                {
                    this.CompileTextElement(extra, nodePrefix + "_Extra_" + extraIndex + "_Text", language);
                    extraIndex++;
                }
            }
            XElement options = node.Element("options");
            if (options != null)
            {
                int optionIndex = 0;
                foreach (XElement option in options.Elements("li"))
                {
                    this.CompileTextElement(option.Element("text"), nodePrefix + "_Option_" + optionIndex + "_Text", language);
                    optionIndex++;
                }
            }
        }

        private void SaveCompiledLanguageFile(XElement language)
        {
            string dialogDirectory = Path.Combine(CQFContentPaths.Quests, "DialogTree");
            Directory.CreateDirectory(dialogDirectory);
            language.Save(Path.Combine(dialogDirectory, this.CurTree.defName + "_Text.xml"));
        }

        private void CompileTextElement(XElement element, string key, XElement language)
        {
            if (element == null || element.Value.NullOrEmpty())
            {
                return;
            }
            string source = element.Value;
            if (source.CanTranslate())
            {
                return;
            }
            element.Value = key;
            if (language.Element(key) == null)
            {
                language.Add(new XElement(key, source));
            }
        }

        private string MakeTextKey(string suffix)
        {
            string defName = this.CurTree.defName.NullOrEmpty() ? "Unnamed" : this.CurTree.defName;
            string safeName = new string(defName.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            return "CQF_Dialog_" + safeName + "_" + suffix;
        }

        public static DialogTreeDef tree = new DialogTreeDef { defName = "CQF_DialogTree" };
        public static readonly Vector2 nodeSize = new Vector2(20f, 20f);
        public static readonly Texture2D nodeTexture = ContentFinder<Texture2D>.Get("UI/Node");
        public static readonly Texture2D optionTexture = ContentFinder<Texture2D>.Get("UI/Option");
        public static readonly Texture2D whiteTex = SolidColorMaterials.NewSolidColorTexture(Color.white);
        public static readonly Texture2D blueTex = SolidColorMaterials.NewSolidColorTexture(ColorLibrary.SkyBlue);
        private readonly CQFDialogEditSession session = new CQFDialogEditSession();
        private readonly CQFDialogNodeCanvas canvas;
        private Window? inspector;
        private DialogNode? selectedNode;
        private DialogOption? selectedOption;
        private DialogResult? selectedResult;
        private bool initialized;
        private bool showInspector;
        private string? lastHistoryError;
    }
}
