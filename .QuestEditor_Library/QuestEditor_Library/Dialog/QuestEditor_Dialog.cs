using System;
using System.Collections.Generic;
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
                    string path = Page_QuestEditor.Path + @"\DialogTree\" + this.CurTree.defName + ".xml";
                    XElement defs = new XElement("Defs");
                    XElement tree = this.CurTree.SaveToXElement("QuestEditor_Library.DialogTreeDef");
                    defs.Add(tree);
                    defs.Save(path);
                    string path_Text = Page_QuestEditor.Path + @"\DialogTree\" + this.CurTree.defName + "_Text.xml";
                    XElement defs_Text = new XElement("LanguageData");
                    defs_Text.Add(new XText("\n\n"));
                    List<string> traned = new List<string>();
                    if (!this.CurTree.title.NullOrEmpty())
                    {
                        string titleText = "TODO";
                        if (this.CurTree.title.CanTranslate())
                        {
                            titleText = this.CurTree.title.Translate().Resolve();
                            traned.Add(this.CurTree.title);
                        }
                        defs_Text.Add(new XElement(this.CurTree.title, titleText));
                    }
                    defs_Text.Add(new XText("\n\n"));
                    
                    this.CurTree.nodeMoulds.ToList().ForEach(m =>
                    {
                        if (!traned.Contains(m.Value.text))
                        {
                            string text = "TODO";
                            if (m.Value.text.CanTranslate())
                            {
                                text = m.Value.text.Translate().Resolve();;
                                traned.Add(m.Value.text);
                            }

                            defs_Text.Add(new XElement(m.Value.text, text));   
                        }
                        m.Value.extraText.ForEach(t =>
                        {
                            if (!traned.Contains(t))
                            {
                                string text = "TODO";
                                if (t.CanTranslate())
                                {
                                    text = t.Translate().Resolve();;
                                    traned.Add(m.Value.text);
                                }

                                defs_Text.Add(new XElement(t, text));   
                            }
                        });
                        m.Value.options.ForEach(t2 =>
                        {
                            if (!traned.Contains(t2.text))
                            {
                                string text = "TODO";
                                if (t2.text.CanTranslate())
                                {
                                    text = t2.text.Translate().Resolve();;
                                    traned.Add(t2.text);
                                }

                                defs_Text.Add(new XElement(t2.text, text));
                            }

                        });
                        defs_Text.Add(new XText("\n\n"));
                    });
                    defs_Text.Save(path_Text);

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
