using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class GroupDataDef : Def,ISaveable
    {
        public Dictionary<string, TargetInfo> Generate(Map map, IntVec3 pos, Quest quest)
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            Lord lord = LordMaker.MakeNewLord(GameTools.GetFaction(this.lord.faction,map), this.lord.lordJobData.CreateJob(map, quest), map);
            this.pawns.ForEach(p =>
            {
                Dictionary<string, TargetInfo> targets2 = p.Spawn(pos, map, "Quest" + quest?.id, quest, lord);
                if (targets2 != null) 
                {
                    targets2.ToList().ForEach(t => targets.Add(t.Key, t.Value));
                }
            });
            this.lord.actions?.ForEach(a => a.WorkForLord(targets, quest, lord));
            return targets;
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("defName",this.defName));
            result.Add(this.lord.SaveToXElement("lord"));
            result.Add(CQFEditorTools.SaveList_Saveable(this.pawns, "pawns"));
            return result;
        }

        public LordData lord = new LordData();
        public List<PawnSpawnData> pawns = new List<PawnSpawnData>();
    }
}
