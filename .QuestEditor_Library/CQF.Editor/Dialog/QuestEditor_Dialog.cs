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
    public partial class QuestEditor_Dialog : Page, ICQFAIEditorHost
    {
        public QuestEditor_Dialog()
        {
            this.canvas = new CQFDialogNodeCanvas(this);
            this.details = new CQFDialogDetailsPanel(this);
            this.preview = new CQFDialogPreviewPanel(this);
            this.preventCameraMotion = true;
            this.absorbInputAroundWindow = true;
            this.doCloseX = true;
            this.InitCurTree();
            this.session.Reset(this.CurTree);
        }

        public override string PageTitle => "DialogEditor".Translate().Colorize(CQFEditorPalette.Accent);
        public override Vector2 InitialSize => new Vector2(Mathf.Min(UI.screenWidth - 40f, 1520f), Mathf.Min(UI.screenHeight - 60f, 940f));
        public int? SelectedNodeIndex => this.canvas.SelectedIndex;
        public CQFDialogEditSession Session => this.session;
        public DialogNode? SelectedNode => this.selectedNode;
        public DialogOption? CurrentOption => this.selectedOption;
        public DialogResult? CurrentResult => this.selectedResult;
        public CQFAIEditorContext AIContext => new CQFAIEditorContext(this.CurTree.defName, () => this.CurTree, value =>
        {
            int? nodeId = this.selectedNode?.index;
            int? optionId = this.CurTree.optionMoulds.Where(pair => pair.Value == this.selectedOption).Select(pair => (int?)pair.Key).SingleOrDefault();
            int resultIndex = this.selectedOption != null && this.selectedResult != null ? this.selectedOption.results.IndexOf(this.selectedResult) : -1;
            this.CloseEditors();
            this.RecordChanges();
            tree = (DialogTreeDef)value;
            this.InitCurTree();
            DialogNode? node = nodeId.HasValue && tree.nodeMoulds.TryGetValue(nodeId.Value, out DialogNode current) ? current : null;
            if (optionId.HasValue && tree.optionMoulds.TryGetValue(optionId.Value, out DialogOption option))
            {
                if (resultIndex >= 0 && resultIndex < option.results.Count) this.SelectResult(node, option, option.results[resultIndex], false);
                else this.SelectOption(node, option, false);
            }
            else if (node != null) this.SelectNode(node, false);
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
                this.showPreview = false;
                this.selectedNode = null;
                this.selectedOption = null;
                this.selectedResult = null;
                this.InitCurTree();
                this.session.Reset(value);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            bool previousWrap = Text.WordWrap;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            try
            {
                Text.Font = GameFont.Medium;
                float titleWidth = Mathf.Min(260f, inRect.width);
                Widgets.Label(new Rect(0f, 0f, titleWidth, 35f), this.PageTitle.Truncate(titleWidth));
                Text.Font = GameFont.Small;
                Rect treeInfo = new Rect(270f, 7f, Mathf.Max(0f, inRect.width - 310f), 26f);
                string treeTitle = this.CurTree.title.CanTranslate() ? this.CurTree.title.Translate().ToString() : this.CurTree.title;
                Widgets.Label(treeInfo, treeTitle.Truncate(treeInfo.width));
                TooltipHandler.TipRegion(treeInfo, this.CurTree.defName + "\n" + this.CurTree.title);
                this.DrawButton(inRect);
                bool sidePanel = this.showInspector || this.showPreview;
                CQFDialogEditorLayout layout = new CQFDialogEditorLayout(inRect, sidePanel);
                Rect canvasRect = layout.Canvas;
                this.canvas.InputBlockedRect = layout.Overlay ? layout.Panel : (Rect?)null;
                this.canvas.HighlightedNodes = this.showPreview && this.preview.CurrentIndex.HasValue ? new HashSet<int> { this.preview.CurrentIndex.Value } : new HashSet<int>();
                this.canvas.Draw(canvasRect);
                if (sidePanel)
                {
                    Rect inspectorRect = layout.Panel;
                    Widgets.DrawBoxSolid(inspectorRect, CQFEditorPalette.Panel);
                    GUI.BeginGroup(inspectorRect.ContractedBy(6f));
                    try
                    {
                        Rect panel = new Rect(0f, 0f, inspectorRect.width - 12f, canvasRect.height - 12f);
                        if (this.showPreview) this.preview.Draw(panel);
                        else this.DrawInspector(panel);
                    }
                    finally { GUI.EndGroup(); }
                }
                if (!this.canvas.IsInteracting && UnityEvent.current.type != EventType.Repaint && UnityEvent.current.type != EventType.Layout)
                {
                    this.RecordChanges();
                }
            }
            finally { Text.Font = previousFont; Text.Anchor = previousAnchor; Text.WordWrap = previousWrap; }
        }

        public override void PostClose()
        {
            this.CloseEditors();
            base.PostClose();
        }

        public void InitCurTree()
        {
            this.CurTree.Update();
            bool hasPositions = this.CurTree.nodeMoulds.Values.Any(node => node.editorPositionSet)
                && (this.CurTree.optionMoulds.Count == 0 || this.CurTree.optionMoulds.Values.Any(option => option.editorPositionSet));
            this.canvas.EnsurePositions();
            if (this.selectedNode != null && !this.CurTree.nodeMoulds.Values.Contains(this.selectedNode))
            {
                this.selectedNode = null;
                this.selectedOption = null;
                this.selectedResult = null;
            }
            else if (this.selectedOption != null && !this.CurTree.optionMoulds.Values.Contains(this.selectedOption))
            {
                this.selectedOption = null;
                this.selectedResult = null;
            }
            else if (this.selectedOption != null && this.selectedResult != null && !this.selectedOption.results.Contains(this.selectedResult))
            {
                this.SelectOption(this.selectedNode, this.selectedOption);
            }
            if (!hasPositions)
            {
                this.canvas.Arrange();
            }
            this.canvas.Select(this.selectedNode, this.selectedOption);
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
            this.canvas.Select(node, null);
            if (showProperties) { this.showInspector = true; this.showPreview = false; }
            if (this.selectedNode != node || this.selectedOption != null)
            {
                this.selectedNode = node;
                this.selectedOption = null;
                this.selectedResult = null;
                this.details.Reset();
            }
        }

        public void CloseSidePanel() { this.showInspector = false; this.showPreview = false; }

        public void SelectOption(DialogNode? node, DialogOption option, bool showProperties = true)
        {
            this.canvas.Select(node, option);
            if (showProperties) { this.showInspector = true; this.showPreview = false; }
            if (this.selectedOption != option) this.details.Reset();
            this.selectedNode = node;
            this.selectedOption = option;
            this.selectedResult = null;
        }

        public void SelectResult(DialogNode? node, DialogOption option, DialogResult result, bool showProperties = true)
        {
            this.canvas.Select(node, option);
            if (showProperties) { this.showInspector = true; this.showPreview = false; }
            if (this.selectedResult != result) this.details.Reset();
            this.selectedNode = node;
            this.selectedOption = option;
            this.selectedResult = result;
        }

        public void AddOption(DialogNode node)
        {
            this.CreateOption(node, this.canvas.GetOptionPosition(node));
        }

        public void RemoveNode(DialogNode node)
        {
            if (node.index == 0)
            {
                Messages.Message("CQF_DialogGraph_KeepEntry".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            foreach (DialogResult result in this.CurTree.optionMoulds.Values.SelectMany(option => option.results))
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
            this.InitCurTree();
        }

        public void ApplyDraft(DialogTreeDef draft)
        {
            this.RecordChanges();
            this.CloseEditors();
            tree = draft;
            this.canvas.Reset();
            this.showInspector = false;
            this.showPreview = false;
            this.selectedNode = null;
            this.selectedOption = null;
            this.selectedResult = null;
            this.InitCurTree();
        }

        public void DrawButton(Rect inRect)
        {
            bool compact = inRect.width < 560f;
            float x = 0f;
            if (CQFAIIconButton.DrawText(new Rect(x, 44f, 72f, 32f), "CQF_DialogGraph_File".Translate())) this.ShowFileMenu();
            x += 78f;
            string viewTip = string.Join(" · ", new[] { "CQF_DialogGraph_Undo", "CQF_DialogGraph_Redo", "CQF_DialogGraph_Arrange", "CQF_DialogGraph_Fit", "CQF_DialogGraph_Find" }.Select(key => key.Translate().ToString()));
            if (CQFAIIconButton.DrawText(new Rect(x, 44f, 72f, 32f), "CQF_DialogGraph_View".Translate(), tip: viewTip)) this.ShowViewMenu();
            x += 82f;
            void Image(Texture2D texture, string key, Action action, bool available = true)
            {
                Rect button = new Rect(x, 44f, 32f, 32f);
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && available;
                string tip = key == "CQF_DialogGraph_Preview" ? key.Translate() + "\n" + "CQF_DialogGraph_PreviewHint".Translate() : key.Translate().ToString();
                bool clicked = CQFAIIconButton.DrawFramedImage(button, texture, tip);
                GUI.enabled = enabled;
                x += 36f;
                if (clicked) action();
            }
            Image(ContentFinder<Texture2D>.Get("UI/Icon_Edit"), "CQF_DialogGraph_TreeSettings", () =>
                Find.WindowStack.Add(new QuestEditor_DialogTreeMisc(this.CurTree, this.RecordChanges)));
            if (!compact)
            {
                Image(TexUI.ArrowTexLeft, "CQF_DialogGraph_Undo", () => this.RestoreHistory(false), this.session.CanUndo);
                Image(TexUI.ArrowTexRight, "CQF_DialogGraph_Redo", () => this.RestoreHistory(true), this.session.CanRedo);
            }
            Image(TexButton.Play, "CQF_DialogGraph_Preview", () =>
            {
                this.showPreview = !this.showPreview;
                if (this.showPreview) this.preview.Start(this.SelectedNodeIndex ?? 0);
            });
            if (!compact)
            {
                Image(TexButton.Save, "Save", this.SaveTree);
            }
            if (CQFAIButton.Draw(new Rect(x, 44f, 32f, 32f), true)) CQFAIBridge.Open(this.AIContext);
            x += 42f;
            Rect summary = new Rect(x, 49f, Mathf.Max(0f, inRect.width - x), 22f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(summary, "CQF_DialogGraph_Summary".Translate(this.CurTree.nodeMoulds.Count, this.CurTree.optionMoulds.Count).ToString().Truncate(summary.width));
            Text.Font = GameFont.Small;
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
                new FloatMenuOption("Save".Translate(), this.SaveTree)
            }));
        }

        private void ShowViewMenu()
        {
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_DialogGraph_Undo".Translate(), this.session.CanUndo ? () => this.RestoreHistory(false) : (Action?)null),
                new FloatMenuOption("CQF_DialogGraph_Redo".Translate(), this.session.CanRedo ? () => this.RestoreHistory(true) : (Action?)null),
                new FloatMenuOption("CQF_DialogGraph_Arrange".Translate(), this.canvas.Arrange),
                new FloatMenuOption("CQF_DialogGraph_Fit".Translate(), this.canvas.Fit),
                new FloatMenuOption("CQF_DialogGraph_Find".Translate(), () => Find.WindowStack.Add(new FloatMenu(
                    this.CurTree.nodeMoulds.OrderBy(pair => pair.Key).Select(pair => new FloatMenuOption(
                        (pair.Value.text.CanTranslate() ? pair.Value.text.Translate().ToString() : pair.Value.text).Replace('\r', ' ').Replace('\n', ' '), () => this.canvas.Focus(pair.Key))).ToList())))
            }));
        }

        private void DrawInspector(Rect rect)
        {
            this.details.Draw(rect);
        }

        private void RestoreHistory(bool redo)
        {
            this.CloseEditors();
            tree = redo ? this.session.Redo(this.CurTree) : this.session.Undo(this.CurTree);
            this.selectedNode = null;
            this.selectedOption = null;
            this.selectedResult = null;
            this.canvas.Reset();
            this.showInspector = false;
            this.showPreview = false;
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
                if (compile) new CQFContentTextExport(this.CurTree.defName).Save(document, typeof(DialogTreeDef), path, additionalTranslations: language);
                else new XElement("Defs", document).Save(path);
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
            foreach (XElement entry in result.Element("optionMoulds")?.Elements("li") ?? Enumerable.Empty<XElement>())
                this.CompileTextElement(entry.Element("value")?.Element("text"), this.MakeTextKey("Option_" + entry.Element("key")?.Value + "_Text"), language);
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

        private void CompileTextElement(XElement? element, string key, XElement language)
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
        public static readonly Texture2D blueTex = SolidColorMaterials.NewSolidColorTexture(CQFEditorPalette.Accent);
        private readonly CQFDialogEditSession session = new CQFDialogEditSession();
        private readonly CQFDialogNodeCanvas canvas;
        private readonly CQFDialogDetailsPanel details;
        private readonly CQFDialogPreviewPanel preview;
        private DialogNode? selectedNode;
        private DialogOption? selectedOption;
        private DialogResult? selectedResult;
        private bool initialized;
        private bool showInspector;
        private bool showPreview;
        private string? lastHistoryError;
    }
}
