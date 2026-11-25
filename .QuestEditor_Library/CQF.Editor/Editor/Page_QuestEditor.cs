using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using System.IO;
using System.Xml;
using RimWorld.QuestGen;
using System.Xml.Linq;
using Verse.Grammar;
using System.Reflection;
using System.Text;

namespace QuestEditor_Library
{
    [StaticConstructorOnStartup]
    public class Page_QuestEditor : Page, ICQFAIEditorHost
    {
        public static List<Type> UseableNodes => typeof(QuestNode).AllSubclassesNonAbstract().OrderBy((x) => x.Name.CanTranslate() ? -1f : 0f).ToList(); /*new List<Type>() {typeof(QuestNode_RandomCustomMap) , typeof(QuestNode_Sequence) , typeof(QuestNode_Letter) , typeof(QuestNode_Signal) , typeof(QuestNode_End) , typeof(QuestNode_IsNull),typeof(QuestNode_Set) };*/
        public override string PageTitle => "QuestEditor".Translate();
        public static ModMetaData ModData
        {
            get
            {
                if (modData == null)
                {
                    modData = ModLister.AllInstalledMods.ToList().Find((x) => x.PackageIdPlayerFacing == "HaiLuan.CustomQuestFramework");
                }
                return modData;
            }
        }
        public static string Path
        {
            get
            {
                string questPath = System.IO.Path.Combine(ModData.RootDir.FullName, "Quests");
                if (!Directory.Exists(questPath))
                {
                    Directory.CreateDirectory(questPath);
                }
                return questPath;
            }
        }
        public static string RulePath => System.IO.Path.Combine(Page_QuestEditor.questPath, "Rule");
        public QuestScriptDef CurQuestData
        {
            get
            {
                return Page_QuestEditor.curQuestData;
            }
            set
            {
                Page_QuestEditor.curQuestData = value;
            }
        }
        public CQFAIEditorContext? AIContext => this.CurQuestData == null ? null : new CQFAIEditorContext(this.CurQuestData.defName, () => this.CurQuestData,
            value =>
            {
                this.CurQuestData = (QuestScriptDef)value;
                Page_QuestEditor.buffers.Clear();
                Page_QuestEditor.buffers["GenerationChance"] = (100f * this.CurQuestData.rootSelectionWeight).ToString();
            }, isValid: () => Find.WindowStack.Windows.Contains(this), owner: this);

        public static string GetBuffer(string name)
        {
            if (!Page_QuestEditor.buffers.ContainsKey(name))
            {
                Page_QuestEditor.buffers.Add(name,"");
            }
            return Page_QuestEditor.buffers[name];
        }
        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            base.DrawPageTitle(inRect);
            base.DoBottomButtons(inRect,null, "SaveToFile".Translate(),() =>
            {
                if (this.CurQuestData == null || string.IsNullOrWhiteSpace(this.CurQuestData.defName))
                {
                    Messages.Message("NoName".Translate(),MessageTypeDefOf.CautionInput);
                    return;
                }

                LongEventHandler.QueueLongEvent(() =>
                {
                    QuestScriptDef def = this.CurQuestData;
                    if (def == null || string.IsNullOrWhiteSpace(def.defName))
                    {
                        Messages.Message("NoName".Translate(), MessageTypeDefOf.CautionInput);
                        return;
                    }
                    def.isRootSpecial = true;
                    XDocument mapXml = new XDocument();
                    XElement defs = new XElement("Defs");
                    XElement root = new XElement("QuestScriptDef");
                    foreach (FieldInfo field in typeof(QuestScriptDef).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {

                        if (field.Name == "root")
                        {
                            root.Add(QE_SaveToolbase.SaveToXml(field, def));
                            continue;
                        }
                        if (field.GetValue(def) is object ob && !Attribute.IsDefined(field, typeof(UnsavedAttribute)))
                        {
                            XElement node = CQFSerialization.XElementFromObject(ob, field.FieldType, field.Name, null,false);
                            root.Add(node);
                        }
                    }
                    defs.Add(root);
                    mapXml.Add(defs);
                    string path = Page_QuestEditor.questPath + @"\" + def.defName + ".xml";
                    mapXml.Save(path);
                    Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                    if (!DefDatabase<QuestScriptDef>.AllDefsListForReading.Exists(d => d.defName == def.defName))
                    {
                        DefDatabase<QuestScriptDef>.Add(def);
                    }
                }, "SaveToFile".Translate(), true, (Exception x) => { Log.Message("SaveError:" + x.ToString()); });
            },false);
            if (Page_QuestEditor.questPath == null)
            {
                Page_QuestEditor.GetQuestPath();
            }
            this.DrawCurQuest(inRect);
        }

        private void DrawCurQuest(Rect inRect)
        {
            if (this.CurQuestData == null)
            {
                this.CurQuestData = new QuestScriptDef();
                this.CurQuestData.defName = "";
                this.CurQuestData.root = new QuestNode_Sequence();
            }
            Widgets.DrawLightHighlight(new Rect(0f, 40f, inRect.width - 16f, inRect.height - 83f));
            Rect viewport = new Rect(4f, 40f, inRect.width - 8f, inRect.height - 83f);
            Rect content = new Rect(0f, 40f, viewport.width - 20f, Mathf.Max(viewport.height, Page_QuestEditor.drawHeight.TryGetValue("Main", out float oldHeight) ? oldHeight : 0f));
            Widgets.BeginScrollView(viewport, ref this.scrollPos, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            float y = 50f;
            this.DrawButtonToOtherTools(content);
            CQFEditorTools.DrawFieldAndText(ref y, "DefName", ref this.CurQuestData.defName);
            y += 30f;
            Rect textRect = new Rect(0f, y, 350f, 20f);
            string nameRule = "QuestNameRulePack".Translate();
            Widgets.Label(textRect, nameRule);
            y += 25f;
            if (this.CurQuestData.questNameRules != null)
            {
                Widgets.Label(new Rect(170f, y + 19f, 350f, 25f), "CurRuleName".Translate(Page_QuestEditor.questNameRules?.ruleName).Colorize(CQFUIStyle.Muted));
            }
            Rect tip = new Rect(Text.CalcSize(nameRule).x + 5f, textRect.y, 25f, 25f);
            CQFUIStyle.ButtonImage(tip, CQFEditorTools.TipIcon, tooltip: "RuleTip".Translate());
            y += 2f;
            Page_QuestEditor.DrawSelectRule(ref y, (node) => { RuleData rule = DirectXmlToObject.ObjectFromXml<RuleData>(node, false); this.CurQuestData.questNameRules = rule.GetRulePack(); Page_QuestEditor.questNameRules = rule; });
            textRect.y += 70f;
            Widgets.Label(textRect, "QuestDescriptionRulePack".Translate());
            y += 25f;
            if (this.CurQuestData.questDescriptionRules != null)
            {
                Widgets.Label(new Rect(170f, y + 15f, 350f, 20f), "CurRuleName".Translate(Page_QuestEditor.questDescriptionRules?.ruleName).Colorize(CQFUIStyle.Muted));
            }
            y += 2f;
            Page_QuestEditor.DrawSelectRule(ref y, (node) => { RuleData rule = DirectXmlToObject.ObjectFromXml<RuleData>(node, false); this.CurQuestData.questDescriptionRules = rule.GetRulePack(); Page_QuestEditor.questDescriptionRules = rule; });
            string text = "AutoAccept".Translate();
            Rect rect = new Rect(0f,y, Text.CalcSize(text).x + 30f,25f);
            Widgets.CheckboxLabeled(rect,text, ref this.CurQuestData.autoAccept);
            rect.x += rect.width + 10f;
            text = "sendAvailableLetter".Translate();
            rect.width = Text.CalcSize(text).x + 40f;
            Widgets.CheckboxLabeled(rect, text, ref this.CurQuestData.sendAvailableLetter);
            string buffer = Page_QuestEditor.GetBuffer("GenerationChance");
            CQFEditorTools.DrawLabelAndText_Line(y, "GenerationChance".Translate(), ref this.CurQuestData.rootSelectionWeight, ref buffer, rect.x + rect.width + 10f);
            Page_QuestEditor.buffers["GenerationChance"] = buffer;
            if (!this.CurQuestData.autoAccept)
            {
                y += 30f;
                string buffera = Page_QuestEditor.GetBuffer("ExpireDaysRangea");
                string bufferi = Page_QuestEditor.GetBuffer("ExpireDaysRangei");
                CQFEditorTools.DrawFloatRange(ref y, "CQFExpireDaysRange".Translate(),ref this.CurQuestData.expireDaysRange,ref buffera, ref bufferi);
                Page_QuestEditor.buffers["ExpireDaysRangea"] = buffera;
                Page_QuestEditor.buffers["ExpireDaysRangei"] = bufferi;
            }
            y += 30f;
            QuestNode_Sequence root = (QuestNode_Sequence)this.CurQuestData.root;
            Page_QuestEditor.DrawQuestNodeData(root,ref y,content);
            Widgets.EndScrollView();
            Page_QuestEditor.drawHeight.SetOrAdd("Main", y);
        }

        public static void DrawQuestNodeData(QuestNode node, ref float y, Rect inRect, float x = 15f)
        {
            string nodeKey = "Node" + node.GetHashCode();
            Page_QuestEditor.drawHeight.TryGetValue(nodeKey, out float bottom);
            Rect panel = new Rect(x - 15f, y, Mathf.Max(40f, inRect.width - x - 8f), Mathf.Max(40f, bottom - y));
            CQFUIStyle.DrawBox(panel, 1, CQFUIStyle.AccentTexture);
            float fieldWidth = Mathf.Max(1f, panel.xMax - x - 12f);
            y += 10f;
            Text.Font = GameFont.Medium;
            Rect rectTitle = new Rect(x, y + 10f, fieldWidth, 36f);
            Widgets.Label(rectTitle, node.GetType().Name.Translate().Colorize(CQFUIStyle.Accent));
            if ((node.GetType().Name + "_Tip").CanTranslate())
            {
                TooltipHandler.TipRegion(rectTitle, (node.GetType().Name + "_Tip").Translate());
            }
            Text.Font = GameFont.Small;
            y += 50f;
            bool custom = true;
            if (node is IDrawable drawable)
            {
                drawable.Draw(ref y, inRect, x);
                custom = false;
            }
            if (node is QuestNode_Sequence sequence)
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(x, y + 15f, Mathf.Max(40f, inRect.width - x - 12f), 36f), "ChildNodes".Translate().Colorize(CQFUIStyle.Accent));
                Text.Font = GameFont.Small;
                y += 50f;
                foreach (QuestNode n in sequence.nodes)
                {
                    Widgets.Label(new Rect(x, y, 150f, 20f), n.GetType().Name.Translate());
                    y += 25f;
                    if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 30f), "OpenNode".Translate()))
                    {
                        Find.WindowStack.Add(new Dialog_EditQuestNode() { node = n });
                    }
                    y += 40f;
                }
                y += 20f;
                if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 30f), "AddNewNode".Translate()))
                {
                    Page_QuestEditor.OpenQuestNodeSelect(d => sequence.nodes.Add((QuestNode)Activator.CreateInstance(d)));
                }
                if (CQFUIStyle.ButtonText(new Rect(x + 200f, y, 150f, 30f), "DeleteNode".Translate()))
                {
                    CQFEditorTools.DrawFloatMenu(sequence.nodes, (n2) => sequence.nodes.Remove(n2), (n2) => n2.GetType().Name.Translate());
                }
                y += 60f;
                custom = false;
            }
            if (node is QuestNode_IsNull nodeNull)
            {
                CQFEditorTools.DrawLabelAndText_SlateRef_Line(y, "ObjectKeyword".Translate(), ref nodeNull.value, x);
                y += 30f;
                if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 38f), "SelectTrueNode".Translate()))
                {
                    Page_QuestEditor.OpenQuestNodeSelect(d => nodeNull.node = (QuestNode)Activator.CreateInstance(d));
                }
                y += 45f;
                if (nodeNull.node != null)
                {
                    Page_QuestEditor.DrawQuestNodeData(nodeNull.node, ref y, inRect, x + 20f);
                    y += 20f;
                }
                if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 38f), "SelectFalseNode".Translate()))
                {
                    Page_QuestEditor.OpenQuestNodeSelect(d => nodeNull.elseNode = (QuestNode)Activator.CreateInstance(d));
                }
                y += 45f;
                if (nodeNull.elseNode != null)
                {
                    Page_QuestEditor.DrawQuestNodeData(nodeNull.elseNode, ref y, inRect, x + 20f);
                    y += 20f;
                }
                y += 20f;
                custom = false;
            }
            if (node is QuestNode_End nodeEnd)
            {
                if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 38f), "outcome".
                        Translate(nodeEnd.outcome.ToString().Translate()),false))
                {
                    CQFEditorTools.DrawFloatMenu<QuestEndOutcome>(new List<QuestEndOutcome>() { QuestEndOutcome.Fail,
                        QuestEndOutcome.Success }, (o) => nodeEnd.outcome = o, (o) => o.ToString().Translate());
                }
                y += 45f;
                Page_QuestEditor.DrawQuestSignalField(ref y, x, fieldWidth, "InSignal".Translate(), nodeEnd.inSignal.ToString(), value => nodeEnd.inSignal = new SlateRef<string>(value));
                custom = false;
            }
            if (node is QuestNode_Signal signal)
            {
                Page_QuestEditor.DrawQuestSignalField(ref y, x, fieldWidth, "InSignal".Translate(), signal.inSignal.ToString(), value => signal.inSignal = new SlateRef<string>(value));
                Page_QuestEditor.DrawQuestSignalField(ref y, x, fieldWidth, "inSignalDisable".Translate(), signal.inSignalDisable.ToString(), value => signal.inSignalDisable = new SlateRef<string>(value));
                Widgets.Label(new Rect(x, y, 150f, 25f), "SignalNode".Translate());
                y += 30f;
                if (signal.node != null)
                {
                    if (CQFUIStyle.ButtonText(new Rect(Mathf.Max(x, inRect.width - 142f), y, 130f, 30f), "Delete".Translate()))
                    {
                        signal.node = null;
                    }
                }
                if (signal.node != null)
                {
                    y += 38f;
                    Page_QuestEditor.DrawQuestNodeData(signal.node, ref y, inRect, x + 5f);
                }
                y += 5f;
                if (signal.node == null)
                {
                    if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 30f), "SelectNode".Translate()))
                    {
                        Page_QuestEditor.OpenQuestNodeSelect(d => signal.node = (QuestNode)Activator.CreateInstance(d));
                    }
                    y += 55f;
                }

                Page_QuestEditor.DrawQuestSignalField(ref y, x, fieldWidth, "outSignals".Translate(), signal.outSignals.ToString(), value => signal.outSignals = new SlateRef<IEnumerable<string>>(value));
                custom = false;
                y += 10f;
            }
            if (custom)
            {
                foreach (FieldInfo field in node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    object value = field.GetValue(node);
                    string key = node.GetHashCode() + field.Name;
                    bool signalField = field.Name.IndexOf("signal", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (signalField && field.FieldType == typeof(string))
                    {
                        Page_QuestEditor.DrawQuestSignalField(ref y, x, fieldWidth, field.Name.Translate(), value as string, selected => field.SetValue(node, selected));
                        continue;
                    }
                    if (signalField
                        && (field.FieldType == typeof(List<string>) || field.FieldType == typeof(IEnumerable<string>) || field.FieldType == typeof(string[])))
                    {
                        CQFSignalEditor.DrawBookList(ref y, field.Name.Translate(), value as IEnumerable<string>, selected =>
                        {
                            field.SetValue(node, field.FieldType == typeof(string[]) ? (object)selected.ToArray() : selected);
                        }, x, fieldWidth);
                        continue;
                    }
                    if (value == null)
                    {
                        continue;
                    }
                    if (value is QuestNode var_node)
                    {
                        if (var_node == null)
                        {
                            if (CQFUIStyle.ButtonText(new Rect(x, y, 130f, 30f), "SelectNode".Translate()))
                            {
                                Page_QuestEditor.OpenQuestNodeSelect(d => field.SetValue(node, (QuestNode)Activator.CreateInstance(d)));
                            }
                            y += 45f;
                        }
                        else
                        {
                            if (CQFUIStyle.ButtonText(new Rect(Mathf.Max(x, inRect.width - 162f), y, 150f, 30f), "Delete".Translate()))
                            {
                                field.SetValue(node, null);
                            }
                        }
                        if (var_node != null)
                        {
                            y += 38f;
                            Page_QuestEditor.DrawQuestNodeData(var_node, ref y, inRect, x + 20f);
                            y += 20f;
                        }
                    }
                    if (value is List<QuestNode> nodes)
                    {
                        Widgets.Label(new Rect(x,y,150f,25f),field.Name.Translate());
                        y += 30f;
                        foreach (QuestNode n in nodes)
                        {
                            Widgets.Label(new Rect(x, y, 150f, 20f), n.GetType().Name.Translate());
                            y += 25f;
                            if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 38f), "OpenNode".Translate()))
                            {
                                Find.WindowStack.Add(new Dialog_EditQuestNode() { node = n });
                            }
                            y += 40f;
                        }
                        y += 20f;
                        if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 30f), "AddNewNode".Translate()))
                        {
                            Page_QuestEditor.OpenQuestNodeSelect(d => nodes.Add((QuestNode)Activator.CreateInstance(d)));
                        }
                        if (CQFUIStyle.ButtonText(new Rect(x + 200f, y, 150f, 30f), "DeleteNode".Translate()))
                        {
                            CQFEditorTools.DrawFloatMenu(nodes, (n2) => nodes.Remove(n2), (n2) => n2.GetType().Name.Translate());
                        }
                        y += 40f;
                    }
                    if (value is int || value is float)
                    {
                        string label = field.Name.Translate();
                        Page_QuestEditor.GetQuestFieldRects(ref y, x, fieldWidth, label, out Rect labelRect, out Rect inputRect);
                        Page_QuestEditor.DrawQuestFieldLabel(labelRect, label);
                        string buffer = Page_QuestEditor.GetBuffer(key);
                        if (value is int number)
                        {
                            Widgets.TextFieldNumeric(inputRect, ref number, ref buffer, -9999);
                        }
                        else
                        {
                            float decimalNumber = (float)value;
                            Widgets.TextFieldNumeric(inputRect, ref decimalNumber, ref buffer, -9999);
                        }
                        Page_QuestEditor.buffers[key] = buffer;
                        continue;
                    }

                    Type type = field.FieldType;
                    if (value is ISlateRef reference && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SlateRef<>))
                    {
                        Type argument = type.GenericTypeArguments[0];
                        string text = reference.SlateRef ?? string.Empty;
                        string label = field.Name.Translate();
                        void WriteReference(string selected)
                        {
                            reference.SlateRef = selected;
                            field.SetValue(node, reference);
                        }

                        if (signalField && (argument == typeof(string) || argument == typeof(IEnumerable<string>)
                            || argument == typeof(List<string>) || argument == typeof(string[])))
                        {
                            Page_QuestEditor.DrawQuestSignalField(ref y, x, fieldWidth, label, text, WriteReference);
                            continue;
                        }

                        Action? select = null;
                        if (argument == typeof(bool) || argument == typeof(bool?))
                        {
                            select = () => CQFEditorTools.DrawFloatMenu(new List<bool> { true, false },
                                selected => WriteReference(selected.ToString()), selected => selected.ToString().Translate());
                        }
                        else if (argument.IsSubclassOf(typeof(Def)))
                        {
                            select = () =>
                            {
                                Type database = typeof(DefDatabase<>).MakeGenericType(argument);
                                if (database.GetProperty(nameof(DefDatabase<ThingDef>.AllDefsListForReading))?.GetValue(null) is not IEnumerable<Def> definitions)
                                {
                                    throw new InvalidOperationException($"Cannot read the definition list for {argument.FullName}");
                                }
                                CQFEditorTools.DrawFloatMenu(definitions.ToList(),
                                    selected => WriteReference(selected.defName), selected => selected.label ?? selected.defName);
                            };
                        }
                        else if (argument.IsEnum)
                        {
                            select = () => CQFEditorTools.DrawFloatMenu(Enum.GetNames(argument).ToList(), WriteReference, selected => selected);
                        }

                        Page_QuestEditor.GetQuestFieldRects(ref y, x, fieldWidth, label, out Rect labelRect, out Rect inputRect);
                        Page_QuestEditor.DrawQuestFieldLabel(labelRect, label, select);
                        if (select == null)
                        {
                            string typeName = (Nullable.GetUnderlyingType(argument) ?? argument).Name;
                            TooltipHandler.TipRegion(labelRect, label + "\n" + "type".Translate() + " " + typeName);
                        }
                        string edited = Widgets.TextField(inputRect, text);
                        if (edited != text)
                        {
                            WriteReference(edited);
                        }
                    }
                }
            }
            y += 8f;
            Page_QuestEditor.drawHeight.SetOrAdd(nodeKey, y);
        }

        private static void DrawQuestSignalField(ref float y, float x, float width, string label, string? value, Action<string> setValue)
        {
            Page_QuestEditor.GetQuestFieldRects(ref y, x, width, label, out Rect labelRect, out Rect inputRect);
            CQFSignalEditor.DrawBookField(labelRect, inputRect, label, value, setValue);
        }

        private static void GetQuestFieldRects(ref float y, float x, float width, string label, out Rect labelRect, out Rect inputRect)
        {
            const float inputHeight = 28f;
            const float columnGap = 10f;
            const float rowGap = 6f;
            bool stacked = width < 360f;
            float labelWidth = stacked ? width : Mathf.Min(260f, width * 0.42f);
            using CQFUIScope scope = new CQFUIScope();
            float labelHeight = Text.CalcHeight(label, Mathf.Max(1f, labelWidth - 12f));
            labelHeight = labelHeight > inputHeight ? Mathf.Max(42f, labelHeight) : inputHeight;
            labelRect = new Rect(x, y, labelWidth, labelHeight);
            inputRect = stacked
                ? new Rect(x, labelRect.yMax + 3f, width, inputHeight)
                : new Rect(labelRect.xMax + columnGap, y + (labelHeight - inputHeight) * 0.5f, width - labelWidth - columnGap, inputHeight);
            y = Mathf.Max(labelRect.yMax, inputRect.yMax) + rowGap;
        }

        private static void DrawQuestFieldLabel(Rect rect, string label, Action? select = null)
        {
            if (select != null)
            {
                if (CQFUIStyle.ButtonText(rect, label, false, overrideTextAnchor: TextAnchor.MiddleLeft))
                {
                    select();
                }
                return;
            }

            using CQFUIScope scope = new CQFUIScope();
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect textRect = rect.ContractedBy(Mathf.Min(6f, rect.width / 4f), 0f);
            Widgets.Label(textRect, label);
        }

        private static void OpenQuestNodeSelect(Action<Type> acceptAction)
        {
            Dictionary<string, Func<Type, bool>> typeFilters = Page_QuestEditor.MakeQuestNodeModFilters();
            Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(Page_QuestEditor.UseableNodes, Page_QuestEditor.GetQuestNodeLabel, acceptAction, null, Page_QuestEditor.GetQuestNodeTip, null, t => t.Name, null, typeFilters, null), "Select".Translate()));
        }

        private static string GetQuestNodeLabel(Type type)
        {
            return type.Name.CanTranslate() ? type.Name.Translate() : type.Name;
        }

        private static string GetQuestNodeTip(Type type)
        {
            return (type.Name + "_Tip").CanTranslate() ? (type.Name + "_Tip").Translate() : "";
        }

        private static Dictionary<string, Func<Type, bool>> MakeQuestNodeModFilters()
        {
            Dictionary<string, Func<Type, bool>> result = new Dictionary<string, Func<Type, bool>>();
            List<string> modNames = Page_QuestEditor.UseableNodes
                .Select(Page_QuestEditor.QuestNodeModFilterName)
                .Distinct()
                .OrderBy(name => name == CQFModName ? 0 : name == VanillaModName ? 1 : 2)
                .ThenBy(name => name)
                .ToList();
            foreach (string modName in modNames)
            {
                string capturedName = modName;
                result[capturedName] = type => Page_QuestEditor.QuestNodeModFilterName(type) == capturedName;
            }
            return result;
        }

        private static string QuestNodeModFilterName(Type type)
        {
            if (type.Assembly == typeof(QuestNode).Assembly)
            {
                return VanillaModName;
            }
            if (ModTypeUtility.IsCQFType(type))
            {
                return CQFModName;
            }
            return ModTypeUtility.GetModName(type);
        }

        private void DrawButtonToOtherTools(Rect inRect)
        {
            int toolIndex = 0;
            Rect NextToolRect()
            {
                int column = toolIndex % 4;
                int row = toolIndex / 4;
                toolIndex++;
                return new Rect(inRect.width - 230f + column * 45f, 50f + row * 45f, 38f, 38f);
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), miscIcon, "Misc"))
            {
                Find.WindowStack.Add(new Dialog_QuestEditorMisc());
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), loadQuestIcon, "LoadQuest"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                DefDatabase<QuestScriptDef>.AllDefsListForReading.ForEach((x) =>
               options.Add(new FloatMenuOption(x.defName,() =>
               {
                   this.CurQuestData = x;
                   Page_QuestEditor.buffers["GenerationChance"] = (100f *x.rootSelectionWeight).ToString();
               }))
                );
                Find.WindowStack.Add(new FloatMenu(options));
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), ruleCreaterIcon, "RuleCreater"))
            {
                Find.WindowStack.Add(new QuestEditor_CreateNewRlueDef(Page_QuestEditor.RulePath));
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), customQuestMapIcon, "CustomQuestMap"))
            {
                Find.WindowStack.Add(new QuestEditor_CustomQuestMap());
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), pawnEditorIcon, "PawnEditor"))
            {
                if (Current.Game != null)
                {
                    Find.WindowStack.Add(new QuestEditor_PawnDataEditor());
                }
                else
                {
                    Messages.Message("NoGame".Translate(), MessageTypeDefOf.CautionInput);
                }
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), dialogManagerIcon, "DialogManager"))
            {
                Find.WindowStack.Add(new QuestEditor_DialogManager());
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), dutyDefEditorIcon, "CQF_DutyDefEditor"))
            {
                Find.WindowStack.Add(new QuestEditor_DutyDefEditor());
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), dutyMapEditorIcon, "CQF_DutyMapEditor"))
            {
                Find.WindowStack.Add(new QuestEditor_DutyMap());
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), questBookEditorIcon, "CQF_QuestBookEditor"))
            {
                Find.WindowStack.Add(new QuestEditor_QuestBook());
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), TexButton.Play, "CQF_Cinematic_Editor"))
            {
                if (Current.Game != null && Find.CurrentMap != null)
                {
                    Close();
                    Find.WindowStack.Add(new QuestEditor_Cinematics());
                }
                else Messages.Message("CQF_Cinematic_MapRequired".Translate(), MessageTypeDefOf.CautionInput);
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), mainMapDefEditorIcon, "MainMapDefEditor"))
            {
                Find.WindowStack.Add(new QuestEditor_MainMapDefEditor());
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), generateNodeTextIcon, "GenerateNodeText"))
            {
                List<XElement> publicText = new List<XElement>();
                Dictionary<XElement,List<XElement>> texts = new Dictionary<XElement, List<XElement>>();
                XElement textXml = new XElement("LanguageData");
                Func<string, string> getText = (name) =>
                 {
                     string fieldText = "";
                     bool first = true;
                     foreach (char c in name)
                     {
                         if (first)
                         {
                             fieldText += c.ToString().ToUpper();
                             first = false;
                             continue;
                         }
                         if (c >= 'A' && c <= 'Z')
                         {
                             fieldText += " " + c;
                             continue;
                         }
                         fieldText += c;
                     }
                     return fieldText;
                 };
                foreach (Type node in typeof(QuestNode).AllSubclassesNonAbstract())
                {
                    foreach (FieldInfo field in node.GetFields())
                    {
                        if (publicText.Exists(x => x.Name == field.Name))
                        {
                            continue;
                        }
                        foreach (KeyValuePair<XElement, List<XElement>> text in texts)
                        {
                            if(text.Value.Find(x => x.Name == field.Name) is XElement rerepeatXml)
                            {
                                text.Value.Remove(rerepeatXml);
                                publicText.Add(rerepeatXml);
                                continue;
                            }
                        }

                        if (texts.Keys.ToList().Find(x => x.Name == node.Name) is XElement nodeXml)
                        {
                            string fieldText = getText(field.Name);
                            texts[nodeXml].Add(new XElement(field.Name, fieldText));
                        }
                        else
                        {
                            texts.Add(new XElement(node.Name,node.Name), new List<XElement>() { new XElement(field.Name, getText(field.Name)) });
                        }
                    }

                }
                textXml.Add(" ");
                textXml.Add(new XComment("Public Text"));
                publicText.ForEach(x => textXml.Add(x));
                textXml.Add(" ");
                textXml.Add(new XComment("Node Text"));
                foreach (KeyValuePair<XElement, List<XElement>> xml in texts)
                {
                    textXml.Add(new XComment(xml.Key.Name.ToString()));
                    textXml.Add(xml.Key);
                    textXml.Add(" ");
                    xml.Value.ForEach(x => textXml.Add(x));

                }
                string path = CQFContentPaths.Quests+ "QuestNode" + ".xml";
                textXml.Save(path,SaveOptions.DisableFormatting);
                Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), groupEditorIcon, "CQF_GroupTemplate_Title"))
            {
                Find.WindowStack.Add(new QuestEditor_GroupEditor());
            }
            if (Page_QuestEditor.DrawToolButton(NextToolRect(), Designator_CustomLord.icon_Lord, "CQF_Organization_Title"))
            {
                Find.WindowStack.Add(new Window_CQFOrganization(Current.Game?.CurrentMap));
            }
            if (CQFAIButton.Draw(NextToolRect())) CQFAIBridge.Open(this.AIContext);
        }

        private static bool DrawToolButton(Rect rect, Texture2D icon, string tipKey)
        {
            TooltipHandler.TipRegion(rect, tipKey.Translate());
            return CQFUIStyle.ButtonImage(rect, icon);
        }

        public static void DrawSelectRule(ref float y,Action<XmlNode> ruleAction , float x = 0f)
        {
            if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 38f), "SelectRule".Translate()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                DirectoryInfo ruleDir = new DirectoryInfo(Page_QuestEditor.RulePath);
                foreach (FileInfo file in ruleDir.GetFiles("*.xml"))
                {
                    XmlDocument xml = new XmlDocument();
                    xml.Load(file.FullName);
                    foreach (XmlNode xmlNode in xml.SelectNodes("//RuleText"))
                    {
                        FloatMenuOption option = new FloatMenuOption(xmlNode["ruleName"].InnerText, () =>
                        {
                            ruleAction(xmlNode);
                        });
                        options.Add(option);
                    }
                    if (options.Any())
                    {
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                }
            }
            y += 45f;
        }

        public static void GetQuestPath()
        {
            if (ModLister.AllInstalledMods.ToList().Find((x) => x.PackageIdPlayerFacing == "HaiLuan.CustomQuestFramework") is ModMetaData data)
            {
                Page_QuestEditor.questPath = System.IO.Path.Combine(data.RootDir.FullName, "Quests");
            }
            else
            {
                Log.Error("无法找到任务编辑器文件");
            }
        }

        public static ModMetaData modData;

        public static Dictionary<string, string> buffers = new Dictionary<string,string>();
        public static Dictionary<string, float> drawHeight = new Dictionary<string, float>();
        public static RuleData questNameRules;
        public static RuleData questDescriptionRules;
        public static string questPath = null;
        private static QuestScriptDef curQuestData = null;
        public Vector2 scrollPos = Vector2.zero;
        public static readonly Texture2D miscIcon = ContentFinder<Texture2D>.Get("UI/Icon_Edit", true);
        public static readonly Texture2D loadQuestIcon = ContentFinder<Texture2D>.Get("UI/QuestEditor/LoadQuest", true);
        public static readonly Texture2D questBookEditorIcon = ContentFinder<Texture2D>.Get("UI/QuestEditor/QuestBookEditor", true);
        public static readonly Texture2D ruleCreaterIcon = ContentFinder<Texture2D>.Get("UI/QuestEditor/RuleCreater", true);
        public static readonly Texture2D customQuestMapIcon = ContentFinder<Texture2D>.Get("UI/QuestEditor/CustomQuestMap", true);
        public static readonly Texture2D pawnEditorIcon = ContentFinder<Texture2D>.Get("UI/QuestEditor/PawnEditor", true);
        public static readonly Texture2D dialogManagerIcon = ContentFinder<Texture2D>.Get("UI/QuestEditor/DialogManager", true);
        public static readonly Texture2D dutyDefEditorIcon = ContentFinder<Texture2D>.Get("UI/QuestEditor/DutyDefEditor", true);
        public static readonly Texture2D dutyMapEditorIcon = ContentFinder<Texture2D>.Get("UI/QuestEditor/DutyMapEditor", true);
        public static readonly Texture2D mainMapDefEditorIcon = ContentFinder<Texture2D>.Get("UI/QuestEditor/MainMapDefEditor", true);
        public static readonly Texture2D generateNodeTextIcon = ContentFinder<Texture2D>.Get("UI/QuestEditor/GenerateNodeText", true);
        public static readonly Texture2D groupEditorIcon = ContentFinder<Texture2D>.Get("UI/QuestEditor/GroupEditor", true);
        private static readonly string CQFModName = "CQF_QuestNodeCategory_CQF".Translate().ToString();
        private static readonly string VanillaModName = "CQF_QuestNodeCategory_Vanilla".Translate().ToString();
    }

}
