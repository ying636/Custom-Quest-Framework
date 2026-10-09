using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_SetCustomInspectText : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.ThingChange;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_SetCustomInspectText.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
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
