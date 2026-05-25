using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class QuestNode_GenerateGroup : QuestNode
    {    
        public Map Map 
        {
            get 
            {
                Slate slate = QuestGen.slate;
                if (this.map != null && this.map.TryGetValue(slate, out Map map))
                {
                    return map ?? Find.RandomPlayerHomeMap;
                }
                else 
                {
                    return Find.RandomPlayerHomeMap;
                }
            }
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            Map map = this.Map;
            GroupDataDef def = this.groupDef.GetValue(slate);
            if (this.TryFindEntryCell(map, out IntVec3 c))
            {
                Dictionary<string, TargetInfo> ps = def.Generate(map, c, quest); 
                List<Thing> things = new List<Thing>();
                ps.ToList().ForEach(p =>
                {
                    QuestUtility.AddQuestTag(ref p.Value.Thing.questTags, "Quest" + quest.id);
                    things.Add(p.Value.Thing);
                });

                slate.Set(this.storeAs.GetValue(slate), things);
            } 
        }

        protected override bool TestRunInt(Slate slate)
        {
            return this.Map is Map map && this.TryFindEntryCell(map,out IntVec3 c);
        }
        private bool TryFindEntryCell(Map map, out IntVec3 cell)
        {
            return RCellFinder.TryFindRandomPawnEntryCell(out cell, map, CellFinder.EdgeRoadChance_Neutral, false, null);
        }

        public SlateRef<Map> map;
        public SlateRef<GroupDataDef> groupDef;
        [NoTranslate]
        public SlateRef<string> storeAs;
    }
}
