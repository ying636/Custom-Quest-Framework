using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public struct LordEventSignal
    {
        public LordEventSignal(LordEventSignalType type)
        {
            this = default;
            this.type = type;
        }

        public Dictionary<string, TargetInfo> MakeTargets(MetaLord owner)
        {
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            if (this.pawn != null)
            {
                result["Pawn"] = this.pawn;
                result["Target"] = this.pawn;
                result["Trigger"] = this.pawn;
            }
            if (owner.rootMap != null) result["RootMap"] = new TargetInfo(IntVec3.Zero, owner.rootMap);
            if (this.sourceMap != null) result["SourceMap"] = new TargetInfo(IntVec3.Zero, this.sourceMap);
            if (this.targetMap != null)
            {
                result["TargetMap"] = new TargetInfo(IntVec3.Zero, this.targetMap);
                result["Map"] = result["TargetMap"];
            }
            return result;
        }

        public LordEventSignalType type;
        public Pawn pawn;
        public Lord group;
        public Map sourceMap;
        public Map targetMap;
        public string tag;
        public string reason;
    }
}
