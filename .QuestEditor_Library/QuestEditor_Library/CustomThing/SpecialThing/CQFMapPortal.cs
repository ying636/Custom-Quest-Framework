using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI.Group;
using Verse;

namespace QuestEditor_Library
{
    public abstract class CQFMapPortal : MapPortal
    {
        public virtual CQFMapPortal Exit => this;
        public virtual CQFMapPortal MapEntrance => this;
        public Dictionary<Pawn, Lord> PawnAndLords
        {
            get
            {
                if (this.pawnAndLords == null)
                {
                    this.pawnAndLords = new Dictionary<Pawn, Lord>();
                }
                return this.pawnAndLords;
            }
        }
        public List<LevelCD> CD
        {
            get
            {
                if (this.CDs == null)
                {
                    this.CDs = new List<LevelCD>();
                }
                return this.CDs;
            }
        }
        public virtual bool IsAllowed(Pawn pawn)
        {
            return !pawn.IsColonist || GameComponent_LevelSchedule.Instance.GetSchedule(pawn).
                allowedLevels.Contains(this.MapEntrance);
        }
        public bool IsAvailable(Pawn pawn)
        {
            return (!this.CD.Any() || !this.CD.Exists(c => c.pawn == pawn)) && this.IsAllowed(pawn);
        }
        public override void OnEntered(Pawn pawn)
        {
            this.Notify_ThingAdded(pawn);
            if (pawn.GetLord() is Lord l) 
            {
                if (l.LordJob is MultLevelLordJobBase job && job.metaLord is
    MetaLord metaLord)
                {
                    metaLord.PawnArrivedNewLevel(pawn, pawn.Map);
                }
                if ((l.LordJob is LordJob_AssaultColony)
                    || pawn.mindState.duty?.def == DutyDefOf.AssaultColony)
                {
                    this.PawnAndLords.SetOrAdd(pawn, pawn.GetLord());
                }
            }
            if (this.Exit.CD.Find(c => c.pawn == pawn) is LevelCD cd)
            {
                cd.cd = 1200;
                return;
            }
            this.Exit.CD.Add(new LevelCD() { pawn = pawn, cd = 1200 }); 
        }
        protected override void Tick()
        {
            base.Tick();
            if (this.PawnAndLords.Any())
            {
                List<Pawn> shouldRemove = new List<Pawn>();
                this.PawnAndLords.ToList().ForEach(p2 =>
                {
                    if (p2.Value != null && !p2.Value.ownedPawns.Contains(p2.Key))
                    {
                        p2.Value.AddPawn(p2.Key);
                    }
                    else
                    {
                        shouldRemove.Add(p2.Key);
                        if (p2.Value != null)
                        {
                            p2.Value.numPawnsEverGained--;
                        }
                    }
                });
                this.PawnAndLords.RemoveAll(p3 => shouldRemove.Contains(p3.Key));
            }
            if (this.CD.Any())
            {
                foreach (var item in this.CD)
                {
                    item.Tick();
                }
                this.CD.RemoveAll(c => c.cd <= 0);
            }
        }
        public override string GetInspectString()
        {
            if (Prefs.DevMode)
            {
                StringBuilder result = new StringBuilder();
                this.CDs.ToList().ForEach(c => result.AppendLine(c.ToString() + ","));
                return result.ToString().Trim();
            }
            return base.GetInspectString();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.pawnAndLords, "pawnAndLords", LookMode.Reference, LookMode.Reference, ref this.pawnAndLords_p, ref this.pawnAndLords_l);
            Scribe_Collections.Look(ref this.CDs, "CDs", LookMode.Deep);
        }

        public List<Pawn> pawnAndLords_p = new List<Pawn>();
        public List<Lord> pawnAndLords_l = new List<Lord>();
        private Dictionary<Pawn, Lord> pawnAndLords = new Dictionary<Pawn, Lord>();

        public List<LevelCD> CDs = new List<LevelCD>();
    }

    public class LevelCD : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.cd, "cd");
            Scribe_References.Look(ref this.pawn, "pawn");
        }

        public void Tick()
        {
            this.cd--;
        }

        public Pawn pawn;
        public int cd;
    }
}

