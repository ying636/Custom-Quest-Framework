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
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            Rect rect = new Rect(x, y, inRect.width - x - 8f, 25f);
            if (CQFUIStyle.ButtonText(rect, "CQF_PawnGroupDef".Translate(this.group?.DisplayName ?? "CQF_GroupBook".Translate().ToString()), false))
                CQFGroupBookBridge.Open(group => this.group = group);
            y += 30f;
            if (!ReferenceEquals(CQFEditorContext.FixedPawnHeader, this))
                this.DrawMapGroup(ref y, x);
            this.DrawCanSaveWarning(ref y, x, inRect);
        }

        public override bool CanSaveToMap()
        {
            return this.group != null;
        }

        public override Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null, bool setLord = true)
        {
            if (this.group == null)
            {
                Log.Error("[CQF] Missing group template for generator: " + this.dataName);
                return new Dictionary<string, TargetInfo>();
            }
            if (!this.TryResolveMapGroup(map, ref lord))
                return new Dictionary<string, TargetInfo>();
            Dictionary<string, TargetInfo> result = this.group.Generate(map, position, quest, lord, questTag);
            if (result != null) this.RegisterEntityBookPawns(result.Values.Select(target => target.Thing).OfType<Pawn>(), quest, map);
            return result;
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
