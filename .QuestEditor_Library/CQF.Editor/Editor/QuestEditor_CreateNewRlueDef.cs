using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestEditor_CreateNewRlueDef : Page
    {
        public QuestEditor_CreateNewRlueDef(string path)
        {
            this.path = path;
        }
        public override Vector2 InitialSize => QuestEditor_CreateNewRlueDef.size;
        public override string PageTitle => "RuleCreater".Translate();
        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
            base.DrawPageTitle(inRect);
            if (Widgets.CloseButtonFor(inRect))
            {
                this.Close();
            }
            this.DrawLoadButton(inRect, 42f);
            this.DrawSaveButton(inRect, 42f);
            Rect viewport = new Rect(0f, 86f, inRect.width, inRect.height - 86f);
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, this.contentHeight));
            Widgets.BeginScrollView(viewport, ref this.scrollPosition, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            float y = 8f;
            CQFEditorTools.DrawFieldAndText(ref y, "RuleName".Translate(), ref QuestEditor_CreateNewRlueDef.curRule.ruleName);
            y += 25f;
            Widgets.Label(new Rect(0f, y, 300f, 30f), "StringRule".Translate());
            y += 36f;
            for (int i = 0; i < QuestEditor_CreateNewRlueDef.curRule.stringRules.Count; i++)
            {
                QuestEditor_CreateNewRlueDef.curRule.stringRules[i] = Widgets.TextField(new Rect(0f, y, content.width, 25f), QuestEditor_CreateNewRlueDef.curRule.stringRules[i]);
                y += 30f;
            }
            if (CQFUIStyle.ButtonText(new Rect(0f, y, 150f, 38f), "AddNewRuleString".Translate()))
            {
                QuestEditor_CreateNewRlueDef.curRule.stringRules.Add("");
            }
            if (QuestEditor_CreateNewRlueDef.curRule.stringRules.Any() && CQFUIStyle.ButtonText(new Rect(170f, y, 150f, 38f), "DeleteRuleString".Translate()))
            {
                QuestEditor_CreateNewRlueDef.curRule.stringRules.RemoveLast();
            }
            y += 63f;
            Widgets.Label(new Rect(0f, y, 300f, 30f), "RuleFilePaths".Translate());
            y += 36f;
            if (QuestEditor_CreateNewRlueDef.curRule.rulesFiles != null)
            {
                for (int i = 0; i < QuestEditor_CreateNewRlueDef.curRule.rulesFiles.Count; i++)
                {
                    QuestEditor_CreateNewRlueDef.curRule.rulesFiles[i] = Widgets.TextField(new Rect(0f, y, content.width, 25f), QuestEditor_CreateNewRlueDef.curRule.rulesFiles[i]);
                    y += 30f;
                }
            }
            if (CQFUIStyle.ButtonText(new Rect(0f, y, 150f, 38f), "AddNewFilePath".Translate()))
            {
                if (QuestEditor_CreateNewRlueDef.curRule.rulesFiles == null)
                {
                    QuestEditor_CreateNewRlueDef.curRule.rulesFiles = new List<string>();
                }
                QuestEditor_CreateNewRlueDef.curRule.rulesFiles.Add("");
            }
            if (QuestEditor_CreateNewRlueDef.curRule.rulesFiles != null && QuestEditor_CreateNewRlueDef.curRule.rulesFiles.Any() && CQFUIStyle.ButtonText(new Rect(170f, y, 150f, 38f), "DeleteFilePath".Translate()))
            {
                QuestEditor_CreateNewRlueDef.curRule.rulesFiles.RemoveLast();
            }
            this.contentHeight = y + 48f;
            Widgets.EndScrollView();
        }

        private Rect DrawLoadButton(Rect inRect, float y)
        {
            if (CQFUIStyle.ButtonText(new Rect(Mathf.Max(0f, inRect.width - 316f), y, Mathf.Min(150f, (inRect.width - 8f) / 2f), 32f), "LoadRule".Translate()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                DirectoryInfo ruleDir = new DirectoryInfo(this.path);
                foreach (FileInfo file in ruleDir.GetFiles("*.xml"))
                {
                    XmlDocument xml = new XmlDocument();
                    xml.Load(file.FullName);
                    foreach (XmlNode xmlNode in xml.SelectNodes("//RuleText"))
                    {
                        FloatMenuOption option = new FloatMenuOption(xmlNode["ruleName"].InnerText, () =>
                        {
                            QuestEditor_CreateNewRlueDef.curRule = DirectXmlToObject.ObjectFromXml<RuleData>(xmlNode,false);
                        });
                        options.Add(option);
                    }
                }
                if (options.Any())
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            return inRect;
        }

        private void DrawSaveButton(Rect inRect, float y)
        {
            if (CQFUIStyle.ButtonText(new Rect(Mathf.Max(0f, inRect.width - 158f), y, Mathf.Min(150f, (inRect.width - 8f) / 2f), 32f), "SaveToFile".Translate()))
            {
                if (QuestEditor_CreateNewRlueDef.curRule.ruleName == null || QuestEditor_CreateNewRlueDef.curRule.ruleName == "")
                {
                    Messages.Message("NoName".Translate(), MessageTypeDefOf.CautionInput);
                    return;
                }
                string xmlPath = Path.Combine(this.path, QuestEditor_CreateNewRlueDef.curRule.ruleName + ".xml");
                XDocument ruleXml = new XDocument();
                XElement root = DirectXmlSaver.XElementFromObject(QuestEditor_CreateNewRlueDef.curRule, typeof(RuleData), "RuleText");
                ruleXml.Add(root);
                ruleXml.Save(xmlPath);
                Messages.Message("SaveSucceed".Translate(xmlPath), MessageTypeDefOf.PositiveEvent);
            }
        }

        public string path;
        public static RuleData curRule = new RuleData();
        private Vector2 scrollPosition;
        private float contentHeight;
        private static readonly Vector2 size = new Vector2(760f,840f);
    }
}
