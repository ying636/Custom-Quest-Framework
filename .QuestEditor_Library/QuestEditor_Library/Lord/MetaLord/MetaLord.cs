using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class MetaLord : IExposable, ILoadReferenceable, IDisposable
    {
        public void Tick() 
        {
            this.tick++;
            if (this.tick != 0 && this.tick % GenDate.TicksPerHour == 0) 
            {
                this.Trigger(new LordEventSignal(LordEventSignalType.Tick));
            }
        }
        public void Trigger(LordEventSignal signal) 
        {
            foreach (var state in eventDef.events)
            {
                if (state.trigger.Active(this,signal)) 
                {
                    state.action.Do(this);
                }
            }
        }
        public void Dispose()
        {
            foreach (var lord in levelLords)
            {
                lord.Dispose();
            }
        }
        public void PawnArrivedNewLevel(Pawn pawn, Map level) 
        {
            if (this.moves.Find(m => m.pawn == pawn
            && m.targetMap == level) is MoveRequirment move) 
            {
                this.moves.Remove(move);
            }
            if (this.levelLords.Find(l => l.level == level) is LevelLord
                levelLord) 
            {
                if (levelLord.ownLords.Any()) 
                {
                    Lord targetLord = null;
                    float sorce = 0;
                    foreach (var lord in levelLord.ownLords)
                    {
                        if (lord.LordJob is MultLevelLordJobBase job) 
                        {
                            float curSorce = job.GetPawnAcceptScore(pawn);
                            if (targetLord == null || sorce < curSorce) 
                            {
                                targetLord = lord;
                                sorce = curSorce;
                            }
                        }
                    }
                    if (targetLord != null)
                    {
                        if (pawn.GetLord() is Lord last) 
                        {
                            last.RemovePawn(pawn);
                        }
                        targetLord.AddPawn(pawn);
                    }
                }
            }
        }
        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.loadID, "loadID", 0, false);
            Scribe_Values.Look(ref this.tick,"tick");
            Scribe_Collections.Look(ref this.levelLords, "levelLords", LookMode.Deep);
            Scribe_Collections.Look(ref this.moves, "moves", LookMode.Deep);
            Scribe_Defs.Look(ref this.eventDef, "eventDef");
        }

        public string GetUniqueLoadID()
        {
            return "MetaLord_" + this.loadID.ToString();
        }
 

        public int loadID;
        public int tick;
        public MetaLordEventDef eventDef;
        public List<MoveRequirment> moves = new List<MoveRequirment>();
        public List<LevelLord> levelLords = new List<LevelLord>();
    }
    public class MoveRequirment : IExposable
    {
        public void ExposeData()
        {
            Scribe_References.Look(ref this.pawn,"pawn");
            Scribe_References.Look(ref this.targetMap, "targetMap");
        }

        public Pawn pawn;
        public Map targetMap;
    }
    public class LevelLord  : IExposable, IDisposable
    {
        public void Tick() 
        {
        
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref this.level,"level");
            Scribe_Collections.Look(ref this.ownLords, "ownLords", LookMode.Reference);
            Scribe_Collections.Look(ref this.tags,"tags", LookMode.Value);
        }

        public void Dispose()
        {
            foreach (var lord in ownLords)
            {
                lord.Dispose();
            }
        }

        public Map level;
        public List<Lord> ownLords = new List<Lord>();
        public List<string> tags = new List<string>();
    }
}
