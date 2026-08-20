using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    [StaticConstructorOnStartup]
    public class QuestEditor_Dialog : Page
    {
        public QuestEditor_Dialog() 
        {
            this.InitCurTree();
            this.preventCameraMotion = false;
            this.absorbInputAroundWindow = false;
        }
        public override string PageTitle => "DialogEditor".Translate().Colorize(ColorLibrary.SkyBlue);
        public DialogTreeDef CurTree
        {
            get
            {
                return QuestEditor_Dialog.tree;
            }
            set 
            {
                QuestEditor_Dialog.tree = value;
                this.InitCurTree();
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            if (Widgets.CloseButtonFor(inRect))
            {
                this.Close();
            }
            base.DrawPageTitle(inRect);
            this.DrawButton(inRect);
            DialogTreeDef tree = this.CurTree;
            Rect rect_Tree = new Rect(0f, 85f, this.width, this.height + 30f + (this.CurTree.idleNodes.Any() ? 40f * ((int)(this.CurTree.idleNodes.Count / 40f )+1) : 0f));
            CQFEditorTools.DrawLabelAndText_Line(45f, "TreeDefName".Translate(), ref tree.defName, 5f, 100f);
            CQFEditorTools.DrawLabelAndText_Line(45f, "DialogTitle".Translate(), ref tree.title, 200f, 100f);
            Widgets.BeginScrollView(new Rect(5f, 75f, inRect.width - 10f, inRect.height - 83f), ref this.scrollPos, rect_Tree);
            rect_Tree.height = this.height;
            Widgets.DrawBox(rect_Tree, 1, QuestEditor_Dialog.blueTex);
            Vector2 setoff = new Vector2(-10f,-10f);
            foreach (DialogNode node in this.CurTree.nodeMoulds.Values)
            {
                if (this.nodePoss.TryGetValue(node, out Vector2 nodeSpace))
                {
                    Rect nodeRect = new Rect(nodeSpace + setoff, QuestEditor_Dialog.nodeSize);
                    if (Widgets.ButtonImage(nodeRect, QuestEditor_Dialog.nodeTexture) && Find.WindowStack.Windows.ToList().Find(x => x.GetType() == typeof(Dialog_EditDialogNode)) == null)
                    {
                        if (Find.WindowStack.Windows.ToList().Find(x => x.GetType() == typeof(Dialog_EditDialogNode)) is Window window)
                        {
                            window.Close();
                        }
                        Find.WindowStack.Add(new Dialog_EditDialogNode(node, this));
                    }
                    TooltipHandler.TipRegion(nodeRect, new TipSignal(node.text + "\n" + node.DebugInformation(tree) /*+ "\n" + nodeRect.y*/));
                    node.options.ForEach(o =>
                    {
                        Rect optionRect = new Rect(this.optionPoss[o] + setoff, QuestEditor_Dialog.nodeSize);
                        if (Widgets.ButtonImage(optionRect, QuestEditor_Dialog.optionTexture) 
                            && Find.WindowStack.Windows.ToList().Find(x => x.GetType() == typeof(Dialog_EditDialogOption)) == null)
                        {
                            if (Find.WindowStack.Windows.ToList().Find(x => x.GetType() == typeof(Dialog_EditDialogOption)) is Window window)
                            {
                                window.Close();
                            }
                            Find.WindowStack.Add(new Dialog_EditDialogOption(this,o,node));
                        }
                        TooltipHandler.TipRegion(optionRect, new TipSignal(o.text + "\n" + o.DebugInformation(tree)));
                        Widgets.DrawLine(this.optionPoss[o], nodeSpace, ColorLibrary.SkyBlue, 1f);
                        o.results.ForEach(r => 
                        {
                            if (r.nextIndex != null && this.CurTree.nodeMoulds.TryGetValue(r.nextIndex.Value) is DialogNode optionNode)
                            { 
                                if (!node.subNodeIndexs.Contains(optionNode.index.Value))
                                {
                                    var offset = new Vector2(0, 3f);
                                    Widgets.DrawLine(this.optionPoss[o] + offset, this.nodePoss[optionNode] + offset,ColorLibrary.Yellow, 1f);
                                }
                                else
                                {
                                    Widgets.DrawLine(this.optionPoss[o], this.nodePoss[optionNode],ColorLibrary.SkyBlue, 1f);
                                }
                            }
                        });
                    });
                }
            }
            if (tree.idleNodes.Any())
            {
                float y = this.height + 100f;
                Widgets.DrawBox(new Rect(0f, y - 10f, inRect.width - 20f, ((int)(this.CurTree.idleNodes.Count / 40f) + 1) * 35f), 1, QuestEditor_Dialog.whiteTex);
                float xForIdle = 5f;
                int times = 0;
                foreach (DialogNode idleNode in tree.idleNodes)
                {
                    Rect nodeRect = new Rect(xForIdle, y, 20f, 20f);
                    if (Widgets.ButtonImage(nodeRect, QuestEditor_Dialog.nodeTexture) && Find.WindowStack.Windows.ToList().Find(x => x.GetType() == typeof(Dialog_EditDialogNode)) == null)
                    {
                        if (Find.WindowStack.Windows.ToList().Find(x => x.GetType() == typeof(Dialog_EditDialogNode)) is Window window) 
                        {
                            window.Close();
                        }
                        Find.WindowStack.Add(new Dialog_EditDialogNode(idleNode, this));
                    }
                    TooltipHandler.TipRegion(nodeRect, new TipSignal("IdleNode".Translate() + "\n" + idleNode.text + idleNode.DebugInformation(tree)));
                    xForIdle += 40f;
                    times++;
                    if (times > 40)
                    {
                        xForIdle = 5f;
                        y += 40f;
                    }
                }
            }
            Widgets.EndScrollView();
        }

        public void InitCurTree() 
        {
            this.nodePoss.Clear();
            this.optionPoss.Clear(); 
            this.InitRootNode();
            this.height = this.unidleNodeSpace > 600f ? this.unidleNodeSpace : 600f;    
        }

        public void InitRootNode() 
        {
            this.width = 950f;
            DialogNode primary = this.CurTree.nodeMoulds.First().Value; 
            this.unidleNodeSpace = primary.GetRequiredSpace(tree);
            float y = (this.unidleNodeSpace / 2) + 90f;
            float x = 20f;
            this.nodePoss.Add(primary,new Vector2(x,y));
            float yOption = 90f;
            primary.options.ForEach(o => this.AddOption(primary,o,x + 30f,ref yOption));
        }
        public void AddOption(DialogNode parentNode,DialogOption option,float x,ref float y) 
        {
            float halfSpace = option.GetRequiredSpace(this.CurTree) / 2f;   
            float y2 = y;
            y += halfSpace;
            this.optionPoss.Add(option,new Vector2(x,y));
            option.results.ForEach(r => 
            {
                if (r.nextIndex != null && 
                    this.CurTree.nodeMoulds.TryGetValue(r.nextIndex.Value) is DialogNode node && 
                parentNode.subNodeIndexs.Contains(node.index.Value) 
                && !this.nodePoss.ContainsKey(node) && node != parentNode) 
                {
                    this.AddNode(node,x + 30f,ref y2);
                }
            });
            y += halfSpace;
            this.width = x > width ? x : width;
        }
        public void AddNode(DialogNode node, float x, ref float y)
        {
            float halfSpace = node.GetRequiredSpace(this.CurTree) / 2f;  
            float y2 = y;
            y += halfSpace;
            this.nodePoss.Add(node, new Vector2(x, y)); 
            node.options.ForEach(r =>
            {
               this.AddOption(node,r, x + 30f,ref y2);
            });
            y += halfSpace;
            this.width = x > width ? x : width;
        }
        public void DrawButton(Rect inRect) 
        {
            if (Widgets.ButtonText(new Rect(800f, 30f, 100f, 38f), "LoadPremade".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogTreeDef>.AllDefsListForReading, (x) => 
                {
                    this.CurTree = x;
                    this.CurTree.Update();
                }, (x) => x?.defName);
            }
            if (Widgets.ButtonText(new Rect(690f, 30f, 100f, 38f), "Save".Translate()))
            {
                try
                {
                    string path = Path.Combine(Page_QuestEditor.Path, "DialogTree", this.CurTree.defName + ".xml");
                    XElement defs = new XElement("Defs");
                    XElement tree;
                    XElement language = new XElement("LanguageData");
                    bool autoCompileTextKey = CustomQuestFramework_ModSetting.setting?.autoCompileDialogTextKey ?? true;
                    if (autoCompileTextKey)
                    {
                        tree = this.BuildCompiledTreeXml(out language);
                    }
                    else
                    {
                        tree = this.CurTree.SaveToXElement("QuestEditor_Library.DialogTreeDef");
                    }
                    defs.Add(tree);
                    defs.Save(path);
                    if (autoCompileTextKey)
                    {
                        this.SaveCompiledLanguageFile(language);
                    }

                    CQFQuestDefBootstrap.HotLoadDialogTreeDef(this.CurTree);

                    Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                }
                catch (Exception e)
                {
                    Log.Error("Save error:" + e.ToString());
                }
            }
            if (Widgets.ButtonText(new Rect(580f, 30f, 100f, 38f), "Misc".Translate()))
            {
                Find.WindowStack.Add(new QuestEditor_DialogTreeMisc(this.CurTree));
            }    
            if (Widgets.ButtonText(new Rect(470f, 30f, 100f, 38f), "ResetBinding".Translate()))
            {
                Dialog_MessageBox dialog = new Dialog_MessageBox("ConfirmCreateNewDialogTree".Translate());
                dialog.buttonBText = "Cancel".Translate();
                dialog.buttonBAction = () => dialog.Close();
                dialog.buttonAText = "Confirm".Translate();
                dialog.buttonAAction = () =>
                {
                    this.CurTree = new DialogTreeDef();
                    dialog.Close();
                };
                Find.WindowStack.Add(dialog);
            }
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
                    this.CompileTextElement(extra, nodePrefix + "_Extra_" + extraIndex, language);
                    extraIndex++;
                }
            }
            XElement options = node.Element("options");
            if (options != null)
            {
                int optionIndex = 0;
                foreach (XElement option in options.Elements("li"))
                {
                    this.CompileTextElement(option.Element("text"), nodePrefix + "_Option_" + optionIndex, language);
                    optionIndex++;
                }
            }
        }

        private void SaveCompiledLanguageFile(XElement language)
        {
            string dialogDirectory = Path.Combine(Page_QuestEditor.Path, "DialogTree");
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

        public float width;
        public float height;
        public float unidleNodeSpace = 0f;
        public Vector2 scrollPos = Vector2.zero;
        public Vector2 nodeTreeScrollPos = Vector2.zero;
        public Dictionary<DialogNode, Vector2> nodePoss = new Dictionary<DialogNode, Vector2>();
        public Dictionary<DialogOption, Vector2> optionPoss = new Dictionary<DialogOption, Vector2>();
        public static DialogTreeDef tree = new DialogTreeDef();
        public static readonly Vector2 nodeSize = new Vector2(20f,20f);
        public static readonly Texture2D nodeTexture = ContentFinder<Texture2D>.Get("UI/Node");
        public static readonly Texture2D optionTexture = ContentFinder<Texture2D>.Get("UI/Option");
        public static readonly Texture2D blueTex = SolidColorMaterials.NewSolidColorTexture(ColorLibrary.SkyBlue);
        public static readonly Texture2D whiteTex = SolidColorMaterials.NewSolidColorTexture(Color.white);
    }
}
