using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_Letter : CQFAction_Target
    {
        public CQFAction_Letter()
        {
            this.targetsText = new List<string>();
        }

        public override CQFActionCategory ActionCategory => CQFActionCategory.DialogEvent;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            float width = Mathf.Max(40f, CQFUIScope.ContentWidth - x - 12f);
            Widgets.Label(new Rect(x, y, width, 24f), "CQF_LetterTitle".Translate());
            y += 26f;
            string sourceLabel = this.label.CanTranslate() ? this.label.Translate().ToString() : this.label;
            string editedLabel = Widgets.TextField(new Rect(x, y, width, 28f), sourceLabel ?? string.Empty);
            if (editedLabel != sourceLabel)
            {
                this.label = editedLabel;
            }
            y += 36f;
            Widgets.Label(new Rect(x, y, width, 24f), "CQF_LetterBody".Translate());
            y += 26f;
            string sourceText = this.text.CanTranslate() ? this.text.Translate().ToString() : this.text;
            string editedText = Widgets.TextArea(new Rect(x, y, width, 110f), sourceText ?? string.Empty);
            if (editedText != sourceText)
            {
                this.text = editedText;
            }
            y += 118f;
            if (CQFUIStyle.ButtonText(new Rect(x, y, width, 28f), "CQF_LetterType".Translate(GetTypeLabel(this.letterDef))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<LetterDef>.AllDefsListForReading.Where(def => def.letterClass == typeof(StandardLetter)).ToList(),
                    def => this.letterDef = def, GetTypeLabel);
            }
            y += 36f;
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("label", this.label), new XElement("text", this.text), new XElement("letterDef", this.letterDef?.defName));
            return result;
        }

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            this.RealWork(targets, quest);
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Find.LetterStack.ReceiveLetter(this.CreateLetter(targets, quest));
        }

        public ChoiceLetter CreateLetter(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (this.letterDef == null || this.letterDef.letterClass != typeof(StandardLetter))
            {
                throw new InvalidOperationException("CQF_LetterInvalidType".Translate(this.letterDef?.defName ?? string.Empty));
            }
            if (this.label.NullOrEmpty() || this.text.NullOrEmpty() || string.IsNullOrWhiteSpace(this.label) || string.IsNullOrWhiteSpace(this.text))
            {
                throw new InvalidOperationException("CQF_LetterMissingText".Translate());
            }
            Dictionary<string, TargetInfo> selected = new Dictionary<string, TargetInfo>();
            foreach (string key in (this.targetsText ?? new List<string>()).Distinct())
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new InvalidOperationException("CQF_LetterMissingTarget".Translate(key ?? string.Empty));
                }
                Dictionary<string, TargetInfo> resolved = GameTools.GetTargets(targets, quest, new List<string> { key });
                if (!resolved.Values.Any(target => target.IsValid))
                {
                    throw new InvalidOperationException("CQF_LetterMissingTarget".Translate(key));
                }
                foreach (KeyValuePair<string, TargetInfo> entry in resolved.Where(entry => entry.Value.IsValid))
                {
                    selected[entry.Key] = entry.Value;
                }
            }
            Dictionary<string, TargetInfo> context = targets == null ? new Dictionary<string, TargetInfo>() : new Dictionary<string, TargetInfo>(targets);
            foreach (KeyValuePair<string, TargetInfo> entry in selected)
            {
                context[entry.Key] = entry.Value;
            }
            return LetterMaker.MakeLetter(this.ResolveText(this.label, context), this.ResolveText(this.text, context), this.letterDef,
                new LookTargets(selected.Values.Distinct()), quest: quest);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.label, "CQFAction_Letter_label", nameof(CQFAction_Letter));
            Scribe_Values.Look(ref this.text, "CQFAction_Letter_text", nameof(CQFAction_Letter));
            Scribe_Defs.Look(ref this.letterDef, "CQFAction_Letter_letterDef");
        }

        public static string GetTypeLabel(LetterDef definition)
        {
            if (definition == null)
            {
                return "None".Translate();
            }
            string key = "CQF_LetterType_" + definition.defName;
            if (key.CanTranslate())
            {
                return key.Translate();
            }
            if (!definition.label.NullOrEmpty())
            {
                return definition.LabelCap;
            }
            return definition.defName.CanTranslate() ? definition.defName.Translate().ToString() : definition.defName;
        }

        private string ResolveText(string source, Dictionary<string, TargetInfo> context)
        {
            string result = source.CanTranslate() ? source.Translate().ToString() : source;
            List<NamedArgument> names = context.Where(entry => entry.Value.HasThing).Select(entry => entry.Value.Thing.Named(entry.Key)).ToList();
            result = result.Formatted(names);
            foreach (KeyValuePair<string, TargetInfo> entry in context)
            {
                if (entry.Value.Thing is Pawn pawn)
                {
                    result = result.AdjustedFor(pawn, entry.Key, true);
                }
            }
            return result;
        }

        [CQFLocalizableText]
        public string label = nameof(CQFAction_Letter);
        [CQFLocalizableText]
        public string text = nameof(CQFAction_Letter);
        public LetterDef letterDef = LetterDefOf.NeutralEvent;
    }
}
