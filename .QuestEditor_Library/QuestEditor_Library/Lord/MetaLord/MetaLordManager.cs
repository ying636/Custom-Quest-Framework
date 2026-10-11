using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class MetaLordManager : IExposable
    {
        public IReadOnlyList<MetaLord> Lords => this.metaLords.AsReadOnly();

        public MetaLord Get(string name, Map map)
        {
            Map root = LevelPather.GetRoot(map);
            return this.metaLords.Find(l => !l.Disposed && l.name == name && l.rootMap == root);
        }

        public MetaLord GetForPawn(Pawn pawn)
        {
            return pawn != null && this.pawnOwners.TryGetValue(pawn, out MetaLord owner) ? owner : null;
        }

        public MetaLord GetOrCreate(string name, Map map, Faction faction, MetaLordEventDef events = null, Quest quest = null)
        {
            if (string.IsNullOrWhiteSpace(name) || map == null || map.Disposed || faction == null)
                throw new ArgumentException("[CQF] MetaLord requires a name, valid map and faction.");
            MetaLord result = this.Get(name, map);
            if (result != null)
            {
                if (result.faction != faction || (events != null && result.eventDef != events) || (quest != null && result.quest != quest))
                    throw new InvalidOperationException("[CQF] Conflicting MetaLord configuration: " + name);
                return result;
            }
            result = new MetaLord
            {
                loadID = Find.UniqueIDsManager.GetNextLordID(), name = name,
                rootMap = LevelPather.GetRoot(map), faction = faction, eventDef = events, quest = quest,
                manager = this
            };
            this.metaLords.Add(result);
            return result;
        }

        public void Bind(Pawn pawn, MetaLord owner)
        {
            MetaLord previous = this.GetForPawn(pawn);
            if (previous != null && previous != owner)
                throw new InvalidOperationException("[CQF] Pawn belongs to another MetaLord: " + pawn);
            this.pawnOwners[pawn] = owner;
        }

        public void Unbind(Pawn pawn, MetaLord owner)
        {
            if (this.GetForPawn(pawn) == owner)
                this.pawnOwners.Remove(pawn);
        }

        public void Remove(MetaLord lord)
        {
            if (!this.metaLords.Contains(lord)) return;
            this.metaLords.Remove(lord);
            lord.Dispose();
        }

        public void NotifyMapRemoved(Map map)
        {
            foreach (MetaLord lord in this.metaLords.ToArray())
            {
                if (lord.rootMap == map)
                {
                    lord.Trigger(new LordEventSignal(LordEventSignalType.MapRemoved) { sourceMap = map });
                    this.Remove(lord);
                }
                else lord.NotifyMapRemoved(map);
            }
        }

        public void Tick()
        {
            if (this.metaLords.Count == 0) return;
            int tick = Find.TickManager.TicksGame;
            if (tick % 60 != 0) return;
            foreach (MetaLord lord in this.metaLords.ToArray())
            {
                if (lord.Disposed) continue;
                try { lord.Tick(); }
                catch (Exception error) { Log.Error("[CQF] MetaLord tick failed: " + lord.name + "\n" + error); }
            }
        }

        public void Rebuild()
        {
            this.pawnOwners.Clear();
            foreach (MetaLord lord in this.metaLords.ToArray())
            {
                if (lord.rootMap == null || lord.rootMap.Disposed || lord.faction == null)
                {
                    Log.Error("[CQF] Removing invalid loaded MetaLord: " + lord.name);
                    this.Remove(lord);
                    continue;
                }
                lord.manager = this;
                try { lord.Rebuild(); }
                catch (Exception error)
                {
                    Log.Error("[CQF] Invalid loaded MetaLord removed: " + lord.name + "\n" + error);
                    this.Remove(lord);
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref this.metaLords, "metaLords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                this.metaLords ??= new List<MetaLord>();
        }

        private List<MetaLord> metaLords = new List<MetaLord>();
        private readonly Dictionary<Pawn, MetaLord> pawnOwners = new Dictionary<Pawn, MetaLord>();
    }
}
