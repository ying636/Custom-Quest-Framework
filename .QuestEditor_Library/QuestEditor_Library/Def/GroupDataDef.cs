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
        public string DisplayName => this.label.NullOrEmpty() ? this.defName : this.LabelCap.ToString();

        public Dictionary<string, TargetInfo> Generate(Map map, IntVec3 pos, Quest quest)
        {
            return this.Generate(map, pos, quest, null, "Quest" + quest?.id);
        }

        internal Dictionary<string, TargetInfo> Generate(Map map, IntVec3 pos, Quest quest, Lord? destination, string questTag)
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            if (!generating.Add(this))
            {
                Log.Error("[CQF] Recursive group generation: " + this.defName);
                return targets;
            }
            try
            {
                LordData config = this.lord;
                if (destination == null)
                {
                    config = config.ResolveGroupTemplate(this.GetAvailableName(map));
                    LordJob job = config.Data.CreateJobForGroup(map, quest, pos, out Faction faction);
                    destination = LordMaker.MakeNewLord(faction, job, map);
                }
                foreach (PawnSpawnData recipe in this.pawns)
                {
                    Dictionary<string, TargetInfo> members = recipe.Spawn(pos, map, questTag, quest, destination);
                    if (members == null) continue;
                    foreach (var member in members)
                    {
                        string key = member.Key;
                        for (int suffix = 2; targets.ContainsKey(key); suffix++) key = member.Key + "_" + suffix;
                        targets.Add(key, member.Value);
                    }
                }
                if (config != this.lord)
                    config.actions?.ForEach(action => action.WorkForLord(targets, quest, destination));
                return targets;
            }
            finally
            {
                generating.Remove(this);
            }
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("defName",this.defName));
            if (!this.label.NullOrEmpty())
                result.Add(new XElement("label", this.label));
            result.Add(this.lord.SaveToXElement("lord"));
            result.Add(CQFSerialization.SaveList_Saveable(this.pawns, "pawns"));
            return result;
        }

        private string GetAvailableName(Map map)
        {
            string name = this.lord.name.NullOrEmpty() ? this.defName : this.lord.name;
            string candidate = name;
            var names = new HashSet<string>(map.lordManager.lords.Select(l => l.LordJob).OfType<MultLevelLordJobBase>().Select(group => group.groupName));
            for (int suffix = 2; names.Contains(candidate); suffix++)
                candidate = name + "_" + suffix;
            return candidate;
        }

        public LordData lord = new LordData();
        public List<PawnSpawnData> pawns = new List<PawnSpawnData>();
        private static readonly HashSet<GroupDataDef> generating = new HashSet<GroupDataDef>();
    }
}
