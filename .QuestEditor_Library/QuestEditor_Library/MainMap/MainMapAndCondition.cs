using RimWorld;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class MainMapAndCondition : IExposable, IDrawable, ISaveable
    {
        public bool Satisfied(Quest quest)
        {
            if (this.conditions.NullOrEmpty())
            {
                return true;
            }
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            foreach (DialogCondition condition in this.conditions)
            {
                if (condition != null && !condition.Satisfied(targets, out string reason, quest))
                {
                    return false;
                }
            }
            return true;
        }

        public void Draw(ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawLabelAndText_Line(y, "MainMapConditionName".Translate(), ref this.name, x, 200f);
            TooltipHandler.TipRegion(new Rect(x, y, 405f, 25f), "MainMapConditionNameTip".Translate());
            y += 30f;
            if (this.set == null)
            {
                this.set = new CustomMapGenerationSet();
            }
            Rect generationSetRect = new Rect(x, y, 255f, 25f);
            Widgets.Label(generationSetRect, "MainMapGenerationSet".Translate().Colorize(ColorLibrary.PaleBlue));
            TooltipHandler.TipRegion(generationSetRect, "MainMapGenerationSetTip".Translate());
            y += 30f;
            this.set.Draw(ref y, inRect, x + 15f);
            float conditionsY = y;
            CQFEditorTools.DrawIDrawList_UseWindow(ref y, x, this.conditions, inRect, "MainMapConditions".Translate(), condition => condition.GetType().Name.Translate());
            TooltipHandler.TipRegion(new Rect(x, conditionsY, 255f, 25f), "MainMapConditionsTip".Translate());
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            if (!this.name.NullOrEmpty())
            {
                result.Add(new XElement("name", this.name));
            }
            if (this.set != null)
            {
                result.Add(this.set.SaveToXElement("set"));
            }
            if (!this.conditions.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.conditions, "conditions"));
            }
            return result;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.name, "name");
            Scribe_Deep.Look(ref this.set, "set");
            Scribe_Collections.Look(ref this.conditions, "conditions", LookMode.Deep);
        }

        public string name;
        public CustomMapGenerationSet set;
        public List<DialogCondition> conditions = new List<DialogCondition>();
    }
}
