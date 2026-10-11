using RimWorld;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_SetCustomInspectText : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.ThingChange;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            string source_text = this.text.CanTranslate() ? this.text.Translate().ToString() : this.text;
            string edited_text = source_text;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_CustomInspectText".Translate(), ref edited_text, x, 240f);
            if (edited_text != source_text)
                this.text = edited_text;
            y += 30f;
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (this.text.NullOrEmpty())
            {
                Log.Error("[CQF] CQFAction_SetCustomInspectText cannot set empty inspect text.");
                return;
            }

            foreach (KeyValuePair<string, TargetInfo> target in targets)
            {
                if (!target.Value.HasThing)
                {
                    Log.Error($"[CQF] CQFAction_SetCustomInspectText target '{target.Key}' is not a thing.");
                    continue;
                }

                CompCustomText comp = target.Value.Thing.TryGetComp<CompCustomText>();
                if (comp == null)
                {
                    Log.Error($"[CQF] CQFAction_SetCustomInspectText target '{target.Value.Thing}' has no CompCustomText.");
                    continue;
                }

                comp.useCustomInspectText = true;
                comp.customInspectText = (this.text.CanTranslate() ? this.text.Translate().ToString() : this.text);
            }
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("text", this.text));
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.text, "text");
        }

        [CQFLocalizableText]
        public string text = string.Empty;
    }
}
