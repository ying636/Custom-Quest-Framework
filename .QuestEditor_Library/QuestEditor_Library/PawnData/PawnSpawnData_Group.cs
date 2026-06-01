using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
public class PawnSpawnData_Group : PawnSpawnData
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(20f + x, y, 250f, 25f);
            if (Widgets.ButtonText(rect, "CQF_PawnGroupDef".Translate(this.group?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu<GroupDataDef>(DefDatabase<GroupDataDef>.AllDefsListForReading, (k) => this.group = k, (k) =>
                {
                    return k.label;
                });
            }
            y += 30f;
            this.DrawCanSaveWarning(ref y, x, inRect);
        }
        public override bool CanSaveToMap()
        {
            return this.group != null;
        }
        public override Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null, bool setLord = true)
        {
            return this.group.Generate(map, position, quest);
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.group != null)
            {
                result.Add(new XElement("group", this.group.defName));
            }
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.group, "group");
        }

        public GroupDataDef group;
    }
}


