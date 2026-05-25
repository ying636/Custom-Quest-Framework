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
    public class CompPower_Level : CompPowerPlant
    {
        protected override float DesiredPowerOutput => 
            this.outputMode ? (this.linked == null ? 0f : -this.targetPowerOutput) :
            (this.LinkedComp == null || !this.LinkedComp.PowerOn) ? 0f : -this.LinkedComp.PowerOutput;
        public CompPower_Level LinkedComp
        {
            get
            {
                if (this.comp == null)
                {
                    this.comp = this.linked?.TryGetComp<CompPower_Level>();
                }
                return this.comp;
            }
        }
        public void Link(CompPower_Level comp)
        {
            this.linked = comp.parent;
            comp.linked = this.parent;
        }
        public override void UpdateDesiredPowerOutput()
        { 
            base.PowerOutput = this.DesiredPowerOutput;
        }
        public override void SetUpPowerVars()
        {
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (this.LinkedComp != null)
            { 
                this.LinkedComp.UpdateDesiredPowerOutput();
                this.UpdateDesiredPowerOutput();
            }
        }
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            if (this.LinkedComp != null) 
            {
                this.LinkedComp.linked = null;
                this.LinkedComp.UpdateDesiredPowerOutput();
                this.linked = null;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            };
            if (this.outputMode)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "CQF_SetReceiver".Translate(this.linked == null ? "Null".Translate().ToString()
                : this.linked.Label),
                    defaultDesc = "CQF_SetReceiverDesc".Translate(),
                    icon = CompTransmit.Icon,
                    action = () =>
                    {
                        List<Building> receivers = new List<Building>();
                        if (this.parent.Map.Parent is PocketMapParent parent
                        && parent.sourceMap is Map map)
                        {
                            foreach (var building in map.listerThings.GetThingsOfType<Building>())
                            {
                                if (building.TryGetComp<CompPower_Level>() is CompPower_Level comp
                                && !comp.outputMode)
                                {
                                    receivers.Add(building);
                                }
                            };
                        }
                        foreach (var map2 in this.parent.Map.GetComponent<MapComponent_CustomMapData>().Submaps)
                        {
                            if (map2.Map != null)
                            {
                                foreach (var building in map2.Map.listerThings.GetThingsOfType<Building>())
                                {
                                    if (building.TryGetComp<CompPower_Level>() is CompPower_Level comp
                                && !comp.outputMode)
                                    {
                                        receivers.Add(building);
                                    }
                                };
                            }
                        }
                        Find.WindowStack.Add(new Dialog_Select<Building>(receivers, null, r => r.Label
                        , "CQF_SetReceiverText".Translate()
                            , t => this.Link(t.TryGetComp<CompPower_Level>()), null, null, null, null, new List<ExtraOption>()
                            {
                    new ExtraOption("Null".Translate(),null,() =>
                    {
                   if(this.linked != null && this.linked.TryGetComp<CompPower_Level>() is CompPower_Level comp)
                        {
                          comp.linked = null;
                        }
                   this.linked = null;
                        this.comp = null;
                    })
                            }));
                    }
                };
                yield return new Command_Action()
                {
                    defaultLabel = "SetOutput".Translate(),
                    defaultDesc = "SetOutputDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Icon_Edit"),
                    action = () =>
                    {
                        Find.WindowStack.Add(new Dialog_Slider(
                            i => "TargetOutput".Translate(i),0,6000,i =>
                            { 
                                this.targetPowerOutput = i;
                                UpdateDesiredPowerOutput();
                                if (this.LinkedComp != null) 
                                {
                                    this.LinkedComp.UpdateDesiredPowerOutput();
                                }
                            },
                            ((int)this.targetPowerOutput)));
                        
                    }
                };
            }
            yield return new Command_Toggle()
            {
                defaultLabel = "ToggleOutputMode".Translate(),
                defaultDesc = "ToggleOutputModeDesc".Translate(),
                icon = TexCommand.DesirePower,
                isActive = () => this.outputMode,
                toggleAction = () => this.outputMode = !this.outputMode
            };
            yield break;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref this.linked, "linked");
            Scribe_Values.Look(ref this.targetPowerOutput, "targetPowerOutput");
            Scribe_Values.Look(ref this.outputMode, "outputMode");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.LinkedComp != null)
            {
                this.LinkedComp.UpdateDesiredPowerOutput();
                this.UpdateDesiredPowerOutput();
            }
        }

        public CompPower_Level comp; 
        public bool outputMode = false;
        public Thing linked;
        public float targetPowerOutput = 0f; 
    }

    public class Building_Renameable : Building, IRenameable
    {
        public override string Label => this.name.NullOrEmpty() ? base.Label : this.name;
        public string RenamableLabel
        { get => this.name; set => this.name = value; }

        public string BaseLabel => this.def.label;

        public string InspectLabel => this.RenamableLabel; 
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.name, "name");
        }

        public string name = "New receiver";
    }
}
