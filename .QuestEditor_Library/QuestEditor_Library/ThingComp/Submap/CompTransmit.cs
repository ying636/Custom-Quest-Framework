using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library 
{
    [StaticConstructorOnStartup]
    public class CompTransmit : ThingComp
    {
        public Building_Storage Building => this.parent as Building_Storage;
        public virtual bool CanTransmit => (this.power == null || this.power.PowerOn) && (this.refuelable == null
            || this.refuelable.HasFuel) && this.receiver != null && this.receiver.Spawned;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.power = this.parent.TryGetComp<CompPowerTrader>();
            this.refuelable = this.parent.TryGetComp<CompRefuelable>();
        }
        public override void CompTick()
        {  
            if (this.parent.Spawned && this.parent.IsHashIntervalTick(15)
                && this.CanTransmit) 
            { 
                this.Building.slotGroup.HeldThings.ToList().ListFullCopy().ForEach(x =>
                {
                    x.DeSpawn();
                    GenSpawn.Spawn(x, this.receiver.Position, this.receiver.Map); 
                });
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action() 
            {
            defaultLabel = "CQF_SetReceiver".Translate(this.receiver == null ? "Null".Translate().ToString()
            : this.receiver.name),
            defaultDesc = "CQF_SetReceiverDesc".Translate(),
            icon = Icon,
            action = () => 
            {
                List<Building_TransmitReceiver> receivers = new List<Building_TransmitReceiver>();
                if (this.parent.Map.Parent is PocketMapParent parent
                && parent.sourceMap is Map map) 
                {
                    foreach (var building in map.listerThings.GetThingsOfType<Building_TransmitReceiver>())
                    {
                        receivers.Add(building);
                    };
                }
                foreach (var map2 in this.parent.Map.GetComponent<MapComponent_CustomMapData>().Submaps)
                {
                    if (map2.Map != null)
                    {
                        foreach (var building in map2.Map.listerThings.GetThingsOfType<Building_TransmitReceiver>())
                        {
                            receivers.Add(building);
                        };
                    }
                }
                Find.WindowStack.Add(new Dialog_Select<Building_TransmitReceiver>(new TextSelectDrawer<Building_TransmitReceiver>(receivers, r => r.Label, t => this.receiver = t, null, null, null, null, new List<ExtraOption>() 
                    {
                    new ExtraOption("Null".Translate(),null,() => this.receiver = null)
                    }, null), "CQF_SetReceiverText".Translate()));
            }
            };
            yield break;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref this.receiver, "receive");
        }


        CompPowerTrader power;
        CompRefuelable refuelable;
        Building_TransmitReceiver receiver;

        public static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/Icons/Icon_SetReceiver");
    }
    public class Building_TransmitReceiver : Building_Storage , IRenameable
    {
        public override string Label => this.name.NullOrEmpty() ? base.Label : this.name;
        public string RenamableLabel
        { get => this.name; set => this.name = value; }

        public string BaseLabel => this.def.label;

        public string InspectLabel => this.RenamableLabel;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            yield return new Command_Action() 
            {
                defaultLabel = "Rename".Translate(),
                icon = TexButton.Rename,
                action = () => Find.WindowStack.Add(new Dialog_RenameReceiver(this))
            };
            yield break;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.name, "name");
        }

        public string name = "New receiver";
    }
}
