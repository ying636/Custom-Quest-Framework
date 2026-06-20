using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    public class CustomDutyMap : IExposable, ISignalReceiver
    {
        public DutyMapNode CurrentNode => this.dutyMap?.GetNode(this.currentNodeId) ?? this.dutyMap?.StartNode;

        public Dictionary<string, string> Strings
        {
            get
            {
                if (this.strings == null)
                {
                    this.strings = new Dictionary<string, string>();
                }
                return this.strings;
            }
        }

        public Dictionary<string, int> Ints
        {
            get
            {
                if (this.ints == null)
                {
                    this.ints = new Dictionary<string, int>();
                }
                return this.ints;
            }
        }

        public Dictionary<string, float> Floats
        {
            get
            {
                if (this.floats == null)
                {
                    this.floats = new Dictionary<string, float>();
                }
                return this.floats;
            }
        }

        public Dictionary<string, bool> Bools
        {
            get
            {
                if (this.bools == null)
                {
                    this.bools = new Dictionary<string, bool>();
                }
                return this.bools;
            }
        }

        public List<TargetWithKey> Targets
        {
            get
            {
                if (this.targets == null)
                {
                    this.targets = new List<TargetWithKey>();
                }
                return this.targets;
            }
        }

        public void SetPawn(Pawn pawn)
        {
            this.pawn = pawn;
        }

        public void RegisterSignalReceiver()
        {
            if (this.registered || this.dutyMap == null)
            {
                return;
            }
            Find.SignalManager.RegisterReceiver(this);
            this.registered = true;
        }

        public void DeregisterSignalReceiver()
        {
            if (!this.registered)
            {
                return;
            }
            Find.SignalManager.DeregisterReceiver(this);
            this.registered = false;
        }

        public void Notify_SignalReceived(Signal signal)
        {
            if (this.pawn == null || this.pawn.Destroyed || this.pawn.Dead || this.dutyMap == null || signal.tag.NullOrEmpty())
            {
                return;
            }
            this.lastSignal = signal.tag;
            this.lastSignalTick = Find.TickManager.TicksGame;
            LordJob_ComplexCustom.GetForPawn(this.pawn)?.TryRunTriggeredTransition(this.pawn, null, null, typeof(CustomDutyTrigger_Signal));
        }

        public string GetString(string key, string defaultValue = null)
        {
            return !key.NullOrEmpty() && this.Strings.TryGetValue(key, out string value) ? value : defaultValue;
        }

        public void SetString(string key, string value)
        {
            if (!key.NullOrEmpty())
            {
                this.Strings.SetOrAdd(key, value);
            }
        }

        public int GetValue(string key, int defaultValue = 0)
        {
            return !key.NullOrEmpty() && this.Ints.TryGetValue(key, out int value) ? value : defaultValue;
        }

        public void SetValue(string key, int value)
        {
            if (!key.NullOrEmpty())
            {
                this.Ints.SetOrAdd(key, value);
            }
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return !key.NullOrEmpty() && this.Floats.TryGetValue(key, out float value) ? value : defaultValue;
        }

        public void SetFloat(string key, float value)
        {
            if (!key.NullOrEmpty())
            {
                this.Floats.SetOrAdd(key, value);
            }
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return !key.NullOrEmpty() && this.Bools.TryGetValue(key, out bool value) ? value : defaultValue;
        }

        public void SetBool(string key, bool value)
        {
            if (!key.NullOrEmpty())
            {
                this.Bools.SetOrAdd(key, value);
            }
        }

        public void RecordTarget(string key, TargetInfo target)
        {
            if (key.NullOrEmpty())
            {
                return;
            }
            if (this.Targets.Find(data => data.key == key) is TargetWithKey targetData)
            {
                targetData.target = target;
                return;
            }
            this.Targets.Add(new TargetWithKey(key, target));
        }

        public TargetInfo GetTarget(string key)
        {
            if (!key.NullOrEmpty() && this.Targets.Find(data => data.key == key) is TargetWithKey targetData)
            {
                return targetData.target;
            }
            return TargetInfo.Invalid;
        }

        public bool TargetExists(string key, bool needSpawned = false)
        {
            return !key.NullOrEmpty() && this.Targets.Exists(data => data.key == key &&
                (!needSpawned || data.target.HasThing && data.target.Thing.Spawned));
        }

        public bool HasKey(string key)
        {
            return !key.NullOrEmpty() && (this.Strings.ContainsKey(key) || this.Ints.ContainsKey(key) ||
                this.Floats.ContainsKey(key) || this.Bools.ContainsKey(key) || this.Targets.Exists(data => data.key == key));
        }

        public void RemoveKey(string key)
        {
            if (key.NullOrEmpty())
            {
                return;
            }
            this.Strings.Remove(key);
            this.Ints.Remove(key);
            this.Floats.Remove(key);
            this.Bools.Remove(key);
            this.Targets.RemoveAll(data => data.key == key);
        }

        public void ClearDatabase()
        {
            this.Strings.Clear();
            this.Ints.Clear();
            this.Floats.Clear();
            this.Bools.Clear();
            this.Targets.Clear();
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref this.dutyMap, "dutyMap");
            Scribe_Values.Look(ref this.currentNodeId, "currentNodeId");
            Scribe_Values.Look(ref this.lastTransitionTick, "lastTransitionTick");
            Scribe_Values.Look(ref this.nextTickTransitionTick, "nextTickTransitionTick", -1);
            Scribe_Values.Look(ref this.lastDamageTick, "lastDamageTick", -1);
            Scribe_Values.Look(ref this.lastSignal, "lastSignal");
            Scribe_Values.Look(ref this.lastSignalTick, "lastSignalTick", -1);
            Scribe_Collections.Look(ref this.strings, "strings", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref this.ints, "ints", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref this.floats, "floats", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref this.bools, "bools", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref this.targets, "targets", LookMode.Deep);
        }

        public DutyMapDef dutyMap;
        public string currentNodeId;
        public int lastTransitionTick;
        public int nextTickTransitionTick = -1;
        public int lastDamageTick = -1;
        public string lastSignal;
        public int lastSignalTick = -1;
        private Pawn pawn;
        private bool registered;
        private Dictionary<string, string> strings = new Dictionary<string, string>();
        private Dictionary<string, int> ints = new Dictionary<string, int>();
        private Dictionary<string, float> floats = new Dictionary<string, float>();
        private Dictionary<string, bool> bools = new Dictionary<string, bool>();
        private List<TargetWithKey> targets = new List<TargetWithKey>();
    }
}
