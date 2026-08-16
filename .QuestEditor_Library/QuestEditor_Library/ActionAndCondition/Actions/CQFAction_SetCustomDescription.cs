using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_SetCustomDescription : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.ThingChange;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_CustomDescription".Translate(), ref this.text, x, 240f);
            y += 30f;
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (this.text.NullOrEmpty())
            {
                Log.Error("[CQF] CQFAction_SetCustomDescription cannot set an empty custom description.");
                return;
            }

            foreach (KeyValuePair<string, TargetInfo> target in targets)
            {
                if (!target.Value.HasThing)
                {
                    Log.Error($"[CQF] CQFAction_SetCustomDescription target '{target.Key}' is not a thing.");
                    continue;
                }

                CompCustomText comp = target.Value.Thing.TryGetComp<CompCustomText>();
                if (comp == null)
                {
                    Log.Error($"[CQF] CQFAction_SetCustomDescription target '{target.Value.Thing}' has no CompCustomText.");
                    continue;
                }

                comp.useCustomDescription = true;
                comp.customDescription = this.text.Translate().ToString();
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

        public string text = string.Empty;
    }
}
