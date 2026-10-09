using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Xml;
using System.Xml.Linq;
using RimWorld.QuestGen;
using Verse.Grammar;
using System.Reflection;
using UnityEngine;
using System.Collections;
using Verse.AI;
using Verse.AI.Group;
using System.IO;
using Unity.Collections;
using RimWorld.Planet;
using System.Net.NetworkInformation;
using System.Text;

namespace QuestEditor_Library
{
    public abstract class CQFAction_Target : CQFAction
    {
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Target.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFSerialization.SaveList(this.targetsText, "targetsText"));
            return result;
        }
        public abstract void RealWork(Dictionary<string, TargetInfo> targets, Quest quest);
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Dictionary<string, TargetInfo> eligibleTargets = GameTools.GetTargets(targets,quest,this.targetsText);
            if (DebugSettings.godMode)
            {
                eligibleTargets.ToList().ForEach(t0 => Log.Message(t0.ToString()));
            }
            this.RealWork(eligibleTargets, quest);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref this.targetsText, "CQFAction_Target_targetsText", LookMode.Value);
        }

        [NoTranslate]
        public List<string> targetsText = new List<string>() { "null" };
    }
    public class CQFAction_Spawn : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.SpawnThing;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Spawn.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
        {
            this.datas.ForEach(d =>
            {
                if (t.Value.CenterCell.IsValid && t.Value.Map != null)
                {
                    d.SpawnLoots(t.Value.Map, t.Value.CenterCell, null, t.Value.Thing);
                }
            });
        });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.datas, "CQFAction_Spawn_datas", LookMode.Deep);
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            XElement datas = new XElement("datas");
            this.datas.ForEach(d => datas.Add(d.SaveToXElement("li")));
            result.Add(datas);
            return result;
        }

        public List<LootData> datas = new List<LootData>();
    }
    public class CQFAction_GenerateSubMap : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_GenerateSubMap.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(this.set.SaveToXElement("set"));
            result.Add(new XElement("pos",this.pos));
            return result;
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Map is Map map && this.set.GetMap() is CustomMapDataDef data) 
                {
                    data.GenerateAsSubmap(map,this.pos,quest != null ? quest.id.ToString() : null,null);
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.p_X, "p_X");
            Scribe_Values.Look(ref this.p_Z, "p_Z");
            Scribe_Values.Look(ref this.p_Y, "p_Y");

            Scribe_Values.Look(ref this.pos, "pos");
            Scribe_Deep.Look(ref this.set, "set");

        }
        internal string p_X;
        internal string p_Z;
        internal string p_Y;
        public IntVec3 pos = IntVec3.Zero;
        public CustomMapGenerationSet set = new CustomMapGenerationSet();
    }
    public class CQFAction_SpawnAndAddToInventory : CQFAction_Target
    {
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_SpawnAndAddToInventory.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                this.datas.ForEach(d =>
                {
                    if (t.Value.Thing is Pawn pawn)
                    {
                        d.SpawnLoots(t.Value.Map, t.Value.CenterCell, null, t.Value.Thing).ForEach(t2 =>
                        {
                            t2.DeSpawn();
                            pawn.inventory.innerContainer.TryAddOrTransfer(t2);
                        });
                    }
                });
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.datas, "CQFAction_Spawn_datas", LookMode.Deep);
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            XElement datas = new XElement("datas");
            this.datas.ForEach(d => datas.Add(d.SaveToXElement("li")));
            result.Add(datas);
            return result;
        }

        public List<LootData> datas = new List<LootData>();
    }
    public class CQFAction_SpawnAndAddToContainer : CQFAction_Target
    {
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_SpawnAndAddToContainer.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                var d = this.datas.RandomElementByWeight(d => d.chance);

                if (t.Value.Thing is IThingHolder holder)
                {
                    d.SpawnLoots(t.Value.Map, t.Value.CenterCell, null, t.Value.Thing).ForEach(t2 =>
                    {
                        t2.DeSpawn();
                        holder.GetDirectlyHeldThings().TryAddOrTransfer(t2);
                        if (t.Value.Thing.TryGetComp<CompEntityHolder>() is CompEntityHolder comp)
                        {
                            comp.Container.TryAddOrTransfer(t2);
                            if (t2.TryGetComp<CompHoldingPlatformTarget>() is CompHoldingPlatformTarget
                                compHoldingPlatformTarget)
                            {
                                compHoldingPlatformTarget.Notify_HeldOnPlatform(comp.Container);
                            }
                        }

                        if (t.Value.Thing is Building_Casket casket)
                        {
                            casket.GetType().GetField("contentsKnown",BindingFlags.Instance 
                                                                      | BindingFlags.NonPublic)
                                ?.SetValue(casket,false);
                        }
                    });
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData(); 
            Scribe_Collections.Look(ref this.datas, "CQFAction_Spawn_datas", LookMode.Deep);
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName); 
            XElement datas = new XElement("datas");

            this.datas.ForEach(d => datas.Add(d.SaveToXElement("li")));
            result.Add(datas);
            return result;
        } 
        public List<LootData> datas = new List<LootData>();
    } 
    public class CQFAction_ReleaseFromContainer : CQFAction_Target
    {
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is Building_HoldingPlatform building) 
                {
                    building.EjectContents();
                    return;
                }
                if (t.Value.Thing is IThingHolder holder)
                {
                    holder.GetDirectlyHeldThings().TryDropAll(t.Value.Thing.Position, t.Value.Map, ThingPlaceMode.Direct);
                }
            });
        }
    }
    public class CQFAction_SwtichEntranceStatus : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("value", this.value));
            result.Add(new XElement("alwaysIsOpposite", this.alwaysIsOpposite));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_SwtichEntranceStatus.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is CustomMapEntrance building)
                {
                    building.Swtich(this.alwaysIsOpposite ? !building.opended : this.value);
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.value,"value");
            Scribe_Values.Look(ref this.alwaysIsOpposite, "alwaysIsOpposite");
        }

        public bool value;
        public bool alwaysIsOpposite;
    } 
    public class CQFAction_Pollute : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Pollute.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.CenterCell.IsValid && t.Value.Map != null)
                {
                    GenRadial.RadialCellsAround(t.Value.Cell,this.radius,true).ToList().ForEach(c => t.Value.Map.pollutionGrid.SetPolluted(c,true));
                }
            });
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.radius, "radius");
            Scribe_Values.Look(ref this.buffer, "buffer");
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("radius", this.radius));
            return result;
        }

        public float radius = 1f;
        public string buffer;
    }
    public class CQFAction_AddExtraOpteration: CQFAction_Target
    {
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_AddExtraOpteration.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)

        {
            object[] arguments = new object[]
            {
                targets,
                quest
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_AddExtraOpteration.RealWork(None:System.Collections.Generic.Dictionary<string, Verse.TargetInfo>,None:RimWorld.Quest)", this, arguments);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref this.option, "option");
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(this.option.SaveToXElement("option"));
            return result;
        }

        public InteractionOperation option = new InteractionOperation();
    }
    public class CQFAction_RemoveDialogManager : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DialogEvent;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_RemoveDialogManager.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.HasThing)
                {
                    GameComponent_Editor.Instance.RemoveDialog(t.Value.Thing);
                }
            });
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            return result;
        }
    }
    public class CQFAction_AddDialogManager : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DialogEvent;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_AddDialogManager.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.HasThing)
                {
                    GameComponent_Editor.Instance.AddDialog(t.Value.Thing, this.dialog);
                }
            });
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("dialog", this.dialog.defName));
            return result;
        }
        public override void ExposeData()
        {
            Scribe_Defs.Look(ref this.dialog, "dialog");
        }

        public DialogManagerDef dialog;
    }
    public class CQFAction_AddRandomDialogManager : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DialogEvent;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_AddRandomDialogManager.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            var dialog = DefDatabase<DialogManagerDef>.AllDefsListForReading.FindAll(t
                => t.tags.Exists(t2 => this.tags.Contains(t2))).RandomElement();
            targets.ToList().ForEach(t =>
            {
                if (t.Value.HasThing)
                {
                    GameComponent_Editor.Instance.AddDialog(t.Value.Thing,dialog );
                }
            });
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFSerialization.SaveList(this.tags,"tags"));
            return result;
        }
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref this.tags, "tags",LookMode.Value);
        }

        public List<string> tags = new List<string>();
    }
    public class CQFAction_Replace : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.ThingChange;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Replace.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.CenterCell.IsValid && t.Value.Map != null)
                {
                    Thing thing = this.data.Spawn(t.Value.Map, t.Value.Thing?.Position ?? t.Value.CenterCell, (d,b) => d, this.useSameStuff ? t.Value.Thing?.Stuff : null, t.Value.Thing?.Rotation);
                    if (t.Value.Thing != null && !t.Value.Thing.Destroyed)
                    {
                        t.Value.Thing.Destroy();
                    }
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.useSameStuff, "useSameStuff");
            Scribe_Deep.Look(ref this.data, "data");
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(data.SaveToXElement("data"));
            if (this.useSameStuff)
            {
                result.Add(new XElement("useSameStuff", this.useSameStuff));
            }
            return result;
        }

        public ThingData data = new ThingData();
        public bool useSameStuff = false;
    }
    public class CQFAction_ReplaceUsingCustomThing : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.ThingChange;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_ReplaceUsingCustomThing.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.CenterCell.IsValid && t.Value.Map != null)
                {
                    Thing thing = this.data.SpawnThing(t.Value.Map,quest,out List<Thing> ts, 
                        t.Value.Thing?.Position ?? t.Value.CenterCell,false,null, (d,b) => this.useSameStuff ? t.Value.Thing?.Stuff : null, t.Value.Thing?.Rotation);
                    if (t.Value.Thing != null && !t.Value.Thing.Destroyed)
                    {
                        t.Value.Thing.Destroy();
                    }
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.useSameStuff, "useSameStuff");
            Scribe_Deep.Look(ref this.customThing, "customThing");
            Scribe_Deep.Look(ref this.data, "data");
        }
        public override XElement SaveToXElement(string nodeName)
        {
            if (this.customThing == null && this.data == null)
            {
                return null;
            }
            XElement result = base.SaveToXElement(nodeName);
            result.Add(this.customThing != null ? ((ICustomThing)this.customThing).GetData(IntVec3.Zero).SaveToXElement("data") : this.data.SaveToXElement("data"));
            if (this.useSameStuff)
            {
                result.Add(new XElement("useSameStuff", this.useSameStuff));
            }
            return result;
        }

        public Thing customThing;
        public CustomThingData data;
        public bool useSameStuff = false;
    }

    public class CQFAction_SpawnCustomThing : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.SpawnThing;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_SpawnCustomThing.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.CenterCell.IsValid && t.Value.Map != null)
                {
                    Thing thing = this.data.SpawnThing(t.Value.Map, quest, out List<Thing> ts,
                        t.Value.Thing?.Position ?? t.Value.CenterCell, false, null,
                        (d, b) => d, t.Value.Thing?.Rotation);
                    if (this.key != null)
                    {
                        GameComponent_Editor.Instance.GetQuestData(quest).RecordTarget(this.key,thing);
                    }
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData(); 
            Scribe_Deep.Look(ref this.customThing, "customThing");
            Scribe_Deep.Look(ref this.data, "data");
            Scribe_Values.Look(ref this.key, "key");
        } 

        public override XElement SaveToXElement(string nodeName)
        {
            if (this.customThing == null && this.data == null)
            {
                return null;
            }

            XElement result = base.SaveToXElement(nodeName);
            result.Add(this.customThing != null
                ? ((ICustomThing)this.customThing).GetData(IntVec3.Zero).SaveToXElement("data")
                : this.data.SaveToXElement("data"));
            if (!this.key.NullOrEmpty())
            {
                result.Add(new XElement("key", this.key));
            }
            return result;
        }

        public string key;
        public Thing customThing;
        public CustomThingData data; 
    }

    public class CQFAction_OpenLootBox : CQFAction_Target
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_OpenLootBox.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            this.targetsText.ForEach(t =>
            {
                if (targets.TryGetValue(t, out TargetInfo target) && target.Thing is LootBox box && !box.opened)
                {
                    box.Open();
                }
            });
        }
    }
    public class CQFAction_ActivateCustomMap : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            this.targetsText.ForEach(t =>
            {
                if (targets.TryGetValue(t, out TargetInfo target) && target.Thing is CustomMapEntrance entrance && entrance.CustomMap == null)
                {
                    entrance.GenerateCustomMap(entrance.Map,null);
                }
            });
        }
    }
    public class CQFAction_Fog : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var item in targets.ToList())
            {
                if (item.Value.Map is Map map && !map.fogGrid.IsFogged(item.Value.Cell)) 
                {
                    map.fogGrid.Refog(CellRect.SingleCell(item.Value.Cell));
                }
            }
        }
    }
    public class CQFAction_FloodUnfog : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var item in targets.ToList())
            {
                if (item.Value.Map is Map map)
                {
                    FloodFillerFog.FloodUnfog(item.Value.Cell, map);
                }
            }
        }
    }
    public class CQFAction_Faction : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Faction;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("faction", this.faction.defName));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Faction.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
                {
                    if (t.Value.Thing is Thing thing && thing.def.CanHaveFaction)
                    {
                        if (this.faction.isPlayer)
                        {
                            thing.SetFaction(Faction.OfPlayer);
                            return;
                        }
                        thing.SetFaction(Find.FactionManager.FirstFactionOfDef(this.faction));
                    }
                });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.faction, "CQFAction_Faction_faction");
        }

        public FactionDef faction;
    }
    public class CQFAction_SetDuty : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("duty", this.duty.defName));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_SetDuty.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is Pawn pawn)
                {
                    pawn.mindState.duty = new PawnDuty(this.duty);
                    if (pawn.GetLord()?.LordJob is LordJob_Custom custom)
                    {
                        custom.pawnDutyDatas.SetOrAdd(pawn, this.duty);
                    }
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.duty, "CQFAction_Duty_duty");
        }

        public DutyDef duty;
    }
    public class CQFAction_SetXenotype : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("xenotype", this.xenotype.defName));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_SetXenotype.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is Pawn pawn)
                {
                    pawn.genes.SetXenotype(this.xenotype);
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.xenotype, "CQFAction_xenotype");
        }

        public XenotypeDef xenotype;
    }
    public class CQFAction_Hediff : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("hediff", this.hediff.defName));
            if (this.bodyPart != null)
            {
                result.Add(new XElement("bodyPart", this.bodyPart.defName));
                result.Add(new XElement("customLabel", this.customLabel));
                result.Add(new XElement("labelBuffer", this.labelBuffer));
            }
            result.Add(new XElement("severity", this.severity));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Hediff.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value != null && t.Value.Thing is Pawn pawn)
                {
                    if (DebugSettings.godMode)
                    {
                        Log.Message("Hediff action worked:" + pawn.ToString());
                    }
                    if (this.severity == 0f)
                    {
                        return;
                    }
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(this.hediff, false);
                    if (hediff != null)
                    {
                        hediff.Severity += this.severity;
                        return;
                    }
                    if (this.severity > 0f)
                    {
                        hediff = HediffMaker.MakeHediff(this.hediff, pawn, this.bodyPart == null ? null : pawn.RaceProps.body.GetPartsWithDef(this.bodyPart).Find(b => this.customLabel == null || b.untranslatedCustomLabel == this.customLabel));
                        hediff.Severity = this.severity;
                        pawn.health.AddHediff(hediff, null, null, null);
                    }
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.severity, "CQFAction_Hediff_severity");
            Scribe_Values.Look(ref this.customLabel, "CQFAction_Hediff_customLabel");
            Scribe_Values.Look(ref this.labelBuffer, "CQFAction_Hediff_labelBuffer");
            Scribe_Defs.Look(ref this.bodyPart, "CQFAction_Hediff_bodyPart");
            Scribe_Defs.Look(ref this.hediff, "CQFAction_Hediff_hediff");
        }


        public string buffer;
        public HediffDef hediff;
        public float severity;
        public BodyPartDef bodyPart;
        [NoTranslate]
        public string customLabel;
        public string labelBuffer;
    }

    public class CQFAction_Ability : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("ability", this.ability.defName)); 
            return result;
        }

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Ability.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t => { this.DoAction(t.Value.Thing); });
        }

        public void DoAction(Thing targetPawn)
        {
            if (targetPawn is Pawn pawn && pawn.abilities is {} ab
                && ab.GetAbility(this.ability) == null)
            {
                ab.GainAbility(this.ability);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData(); 
            Scribe_Defs.Look(ref this.ability, "ability");
        }
 
        public AbilityDef ability; 
    }

    public class CQFAction_Trait : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("trait", this.trait.defName));
            result.Add(new XElement("degree", this.degree));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Trait.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                this.DoAction(t.Value.Thing);
            });
        }
        public void DoAction(Thing targetPawn)
        {
            if (targetPawn is Pawn pawn && pawn.story?.traits is TraitSet set)
            {
                if (!set.HasTrait(this.trait))
                {
                    set.GainTrait(new Trait(this.trait, this.degree));
                }
                else
                {
                    set.RemoveTrait(set.GetTrait(this.trait));
                    set.GainTrait(new Trait(this.trait, this.degree));
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.degree, "CQFAction_Trait_degree");
            Scribe_Defs.Look(ref this.trait, "CQFAction_Trait_trait");
        }

        public string buffer;
        public TraitDef trait;
        public int degree;
    }
    public class CQFAction_RemoveTrait : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("trait", this.trait.defName));
            result.Add(new XElement("degree", this.degree));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_RemoveTrait.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                this.DoAction(t.Value.Thing);
            });
        }
        public void DoAction(Thing targetPawn)
        {
            if (targetPawn is Pawn pawn && pawn.story?.traits is TraitSet set)
            {
                if (set.GetTrait(this.trait) is Trait t)
                {
                    set.RemoveTrait(t);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.degree, "CQFAction_Trait_degree");
            Scribe_Defs.Look(ref this.trait, "CQFAction_Trait_trait");
        }

        public string buffer;
        public TraitDef trait;
        public int degree;
    }

    public class CQFAction_UpgradeTrait : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("trait", this.trait.defName));
            if (this.initDegree != 0)
            {
                result.Add(new XElement("initDegree", this.initDegree));
            }
            if (this.message != null)
            {
                result.Add(new XElement("message", this.message));
            }
            if (this.initMessage != null)
            {
                result.Add(new XElement("initMessage", this.initMessage));
            }
            return result;
        }

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_UpgradeTrait.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t => { this.DoAction(t.Value.Thing); });
        }

        public void DoAction(Thing targetPawn)
        {
            if (targetPawn is Pawn { story.traits: { } set })
            {
                if (set.GetTrait(this.trait) is {} t)
                {
                    if (t.def.degreeDatas.Exists(d => d.degree == t.Degree + 1))
                    {
                        set.RemoveTrait(t);
                        var trait = new Trait(this.trait, t.Degree + 1);
                        set.GainTrait(trait);
                        if (this.message != null)
                        {
                            Messages.Message((this.message.CanTranslate() ? this.message.Translate(targetPawn.Label
                                    ,t.Label,trait.Label) : this.message.Formatted(targetPawn.Label
                                    ,t.Label,trait.Label).ToString())
                                , MessageTypeDefOf.PositiveEvent);
                        }
                    }
                }
                else
                {
                    var trait = new Trait(this.trait, initDegree);
                    set.GainTrait(trait);
                    if (this.initMessage != null)
                    {
                        Messages.Message((this.initMessage.CanTranslate() ? this.initMessage.Translate(targetPawn.Label,trait.Label) : this.initMessage.Formatted(targetPawn.Label,trait.Label).ToString())
                        , MessageTypeDefOf.PositiveEvent);
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.trait, "trait");
            Scribe_Values.Look(ref initDegree,"initDegree");
            Scribe_Values.Look(ref message,"message");
            Scribe_Values.Look(ref initMessage,"initMessage");
            Scribe_Values.Look(ref buffer,"buffer");
        }

        public string buffer;
        [CQFLocalizableText]
        public string initMessage;
        [CQFLocalizableText]
        public string message;
        public int initDegree = 0;
        public TraitDef trait;
    }

    public class CQFAction_Explosion : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.ThingChange;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("damage", this.damage.defName));
            result.Add(new XElement("amount", this.amount));
            result.Add(new XElement("radius", this.radius));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Explosion.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                this.DoAction(t.Value.CenterCell, t.Value.Map);
            });
        }

        public void DoAction(IntVec3 pos, Map map)
        {
            GenExplosion.DoExplosion(pos, map, this.radius, this.damage, null, this.amount);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.radius, "CQFAction_Explosion_radius");
            Scribe_Values.Look(ref this.amount, "CQFAction_Explosion_amount");
            Scribe_Defs.Look(ref this.damage, "CQFAction_Explosion_damage");
        }

        public string bufferR;
        public string buffer;
        public DamageDef damage;
        public int amount;
        public float radius;
    }
    public class CQFAction_Lightning : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.ThingChange;

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Map != null)
                {
                    Find.CurrentMap.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(t.Value.Map, t.Value.CenterCell));
                }
            });
        }
    }
    public class CQFAction_DoEffect : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.VisualEffect;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_DoEffect.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Map != null)
                {
                    this.effect.SpawnMaintained(t.Value.Cell, t.Value.Map);
                }
            });
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("effect", this.effect.defName));
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.effect,"effect");
        }

        public EffecterDef effect;
    }
    public abstract class CQFAction_Mote : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.VisualEffect;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Mote.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("mote", this.mote.defName));
            result.Add(new XElement("off", this.off.ToString()));
            result.Add(new XElement("scale", this.scale));
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.mote, "mote");
            Scribe_Values.Look(ref this.off, "off");
            Scribe_Values.Look(ref this.buffer, "buffer");
            Scribe_Values.Look(ref this.buffer2, "buffer2");
            Scribe_Values.Look(ref this.buffer3, "buffer3");
            Scribe_Values.Look(ref this.scale, "scale");
            Scribe_Values.Look(ref this.bufferS, "bufferS");
        }

        public string buffer;
        public string buffer2;
        public string buffer3;
        public string bufferS;
        public ThingDef mote;
        public Vector3 off = Vector3.zero;
        public float scale = 1;
    }
    public class CQFAction_MakeMoteStatic : CQFAction_Mote
    {
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Map != null)
                {
                    MoteMaker.MakeStaticMote(t.Value.Cell.ToVector3() + this.off,t.Value.Map,this.mote,this.scale);
                }
            });
        }
    }
    public class CQFAction_ThrowMote : CQFAction_Mote
    {
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Map != null)
                {
                    MoteMaker.MakeAttachedOverlay(t.Value.Thing,this.mote,this.off,this.scale);
                }
            });
        }
    }
    public class CQFAction_TakeDamage : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.ThingChange;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("damage", this.damage.defName));
            result.Add(new XElement("amount", this.amount));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_TakeDamage.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                this.DoAction(t.Value.Thing);
            });
        }

        public void DoAction(Thing targetPawn)
        {
            targetPawn.TakeDamage(new DamageInfo(this.damage, this.amount));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.amount, "CQFAction_TakeDamage_severity");
            Scribe_Defs.Look(ref this.damage, "CQFAction_TakeDamage_damage");
        }


        public string buffer;
        public DamageDef damage;
        public float amount;
    }
    public class CQFAction_GainMood : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("thought", this.thought.defName));
            result.Add(new XElement("stage", this.stage));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_GainMood.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is Pawn pawn)
                {
                    this.DoAction(pawn);
                }
            });
        }

        public void DoAction(Pawn targetPawn)
        {
            Thought_Memory thought = ThoughtMaker.MakeThought(this.thought, this.stage);
            targetPawn.needs.mood.thoughts.memories.TryGainMemory(thought);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.thought, "thought");
            Scribe_Values.Look(ref this.stage, "stage");
            Scribe_Values.Look(ref this.buffer, "buffer");
        }

        public ThoughtDef thought;
        public int stage;
        public string buffer;
    }
    public class CQFAction_GainExperience : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.skill != null)
            {
                result.Add(new XElement("skill", this.skill.defName));
            }
            result.Add(new XElement("experienceRange", this.experienceRange.ToString()));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_GainExperience.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                this.DoAction(t.Value.Thing);
            });
        }

        public void DoAction(Thing targetPawn)
        {
            if (targetPawn is Pawn pawn)
            {
                float experience = this.experienceRange.RandomInRange;
                SkillDef skill = this.skill == null ? pawn.skills.skills.RandomElement().def : this.skill;
                pawn.skills.Learn(skill, experience);
                Messages.Message("PawnGainExperience".Translate(pawn.Name.ToString(), experience, skill.label), MessageTypeDefOf.PositiveEvent);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.buffer, "buffer");
            Scribe_Values.Look(ref this.maxBuffer, "maxBuffer");
            Scribe_Values.Look(ref this.experienceRange, "experienceRange");
            Scribe_Defs.Look(ref this.skill, "skill");
        }

        public string buffer;
        public string maxBuffer;
        public SkillDef skill;
        public FloatRange experienceRange;
    }
    public class CQFAction_SetGameCondition : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.SignalState;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.permanent)
            {
                result.Add(new XElement("permanent", this.permanent));
            }
            else 
            {
                result.Add(new XElement("duration", this.duration));
            }
            result.Add(new XElement("condition", this.condition.defName));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_SetGameCondition.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Map is Map map) 
                {
                    map.GameConditionManager.RegisterCondition(this.permanent ?
                        GameConditionMaker.MakeConditionPermanent(this.condition) :
                        GameConditionMaker.MakeCondition(this.condition,this.duration.RandomInRange));
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.buffer, "buffer");
            Scribe_Values.Look(ref this.maxBuffer, "maxBuffer");
            Scribe_Values.Look(ref this.permanent, "permanent");
            Scribe_Values.Look(ref this.duration, "duration");
            Scribe_Defs.Look(ref this.condition, "condition");
        }


        public string buffer;
        public string maxBuffer;
        public bool permanent = false;
        public IntRange duration = new IntRange(100,100);
        public GameConditionDef condition;
    }  
    public class CQFAction_SetGameConditionWithActions : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.SignalState;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.permanent)
            {
                result.Add(new XElement("permanent", this.permanent));
            }
            else 
            {
                result.Add(new XElement("duration", this.duration));
            }

            if (this.useTick)
            {
                result.Add(new XElement("useTick", this.useTick));
                result.Add(new XElement("tick", this.tick));
            }
            result.Add(new XElement("condition", this.condition.defName));
            result.Add(CQFSerialization.SaveList_Saveable(this.actions,"actions"));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_SetGameConditionWithActions.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Map is Map map)
                {
                    GameCondition_Actions condition = (GameCondition_Actions)(this.permanent
                        ? GameConditionMaker.MakeConditionPermanent(this.condition)
                        : GameConditionMaker.MakeCondition(this.condition, this.duration.RandomInRange));
                    condition.actions = this.actions.ListFullCopy();
                    if (useTick)
                    {
                        condition.useTick = true;
                        condition.tick = this.tick;
                    }
                    map.GameConditionManager.RegisterCondition(condition);
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.useTick, "useTick");
            Scribe_Values.Look(ref this.tick, "tick");
            Scribe_Values.Look(ref this.tickBuffer, "tickBuffer");
            
            Scribe_Values.Look(ref this.buffer, "buffer");
            Scribe_Values.Look(ref this.maxBuffer, "maxBuffer");
            Scribe_Values.Look(ref this.permanent, "permanent");
            Scribe_Values.Look(ref this.duration, "duration");
            Scribe_Defs.Look(ref this.condition, "condition");
            Scribe_Collections.Look(ref actions,"actions");
        }


        public bool useTick;
        public int tick;
        public string tickBuffer;
        public string buffer;
        public string maxBuffer;
        public bool permanent = false;
        public IntRange duration = new IntRange(100,100);
        public List<CQFAction> actions = new List<CQFAction>();
        public GameConditionDef condition;
    }

    public class CQFAction_SetCustomHediff : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public List<ActionTriggerMode> Allows =>
            [ActionTriggerMode.Damaged, ActionTriggerMode.Tick, ActionTriggerMode.Down
            ,ActionTriggerMode.Kill];
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName); 
            result.Add(new XElement("hediff", this.hediff.defName));
            if (this.label != null)
            {
                result.Add(new XElement("label", this.label));
            }
            if (this.desc != null)
            {
                result.Add(new XElement("desc", this.desc));
            }
            if (this.color != Color.white)
            {
                result.Add(new XElement("color", this.color));
            }
            result.Add(CQFSerialization.SaveList_Saveable(this.comps, "comps"));
            return result;
        }

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_SetCustomHediff.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is Pawn pawn)
                {
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(this.hediff) is CustomHediff hd)
                    {
                        foreach (var actionComp in this.comps)
                        {
                            hd.comps.Add(actionComp.Copy());   
                        }

                        hd.overridedLabel = (this.label.CanTranslate() ? this.label.Translate().ToString() : this.label);
                        hd.overridedDescription = (this.desc.CanTranslate() ? this.desc.Translate().ToString() : this.desc);
                        hd.overridedColor = this.color;
                    }
                    else
                    {
                        CustomHediff h = (CustomHediff)pawn.health.AddHediff(this.hediff);
                        foreach (var actionComp in this.comps)
                        {
                            h.comps.Add(actionComp.Copy());   
                        }
                        h.overridedLabel = (this.label.CanTranslate() ? this.label.Translate().ToString() : this.label);
                        h.overridedDescription = (this.desc.CanTranslate() ? this.desc.Translate().ToString() : this.desc);
                        h.overridedColor = this.color;
                    }
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData(); 
            Scribe_Values.Look(ref label,"label");
            Scribe_Values.Look(ref desc,"desc");
            Scribe_Values.Look(ref color,"color");
            Scribe_Defs.Look(ref this.hediff, "hediff");
            Scribe_Collections.Look(ref comps, "comps");
            if (Scribe.mode == LoadSaveMode.Inactive)
            {
                foreach (var t in this.comps)
                {
                    t.allowedActions = Allows;
                }
            }
        }

        [CQFLocalizableText]
        public string label;
        [CQFLocalizableText]
        public string desc;
        public Color color = Color.white;
        public List<ActionComp> comps = new List<ActionComp>();
        public HediffDef hediff;
    }

    public class CQFAction_Destory : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.ThingChange;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            return result;
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is { } thing)
                {
                    if (Prefs.DevMode)
                    {
                        Log.Message("CQFAction:Try Destroy:" + thing.Label);
                    }

                    if (!thing.Destroyed)
                    {
                        thing.Destroy();   
                    }
                }
            });
        }
    }

    public class CQFAction_ConsumeInInventory: CQFAction_Target
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFSerialization.SaveList_Saveable(this.requirations, "requirations"));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_ConsumeInInventory.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Dictionary<ThingDef, int> required = new Dictionary<ThingDef, int>();
            foreach (var item in requirations)
            {
                if (item is CQFThingDefCount c) 
                {
                    required.Add(c.thing,c.count.RandomInRange);
                }
            }
            foreach (var item in targets)
            {
                if (item.Value.Thing is Pawn p && p.inventory != null) 
                {
                    foreach (var thingData in required.Keys.ToList().ListFullCopy())
                    {
                        if (required[thingData] > 0 && p.inventory.innerContainer.InnerListForReading.ListFullCopy().Find(t => t.def ==
                      thingData) is Thing thing) 
                        {
                            if (thing.stackCount > required[thingData])
                            {
                                thing.SplitOff(required[thingData]).Destroy();
                                required[thing.def] = 0;
                            }
                            else 
                            {
                                p.inventory.innerContainer.Remove(thing);
                                required[thing.def] -= thing.stackCount;
                                thing.Destroy();
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.requirations, "requirations", LookMode.Deep);
        }
        public List<CQFThingData> requirations = new List<CQFThingData>();
    }
    public class CQFAction_ChangeGoodwillOfFaction : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Faction;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.fixedFaction != null)
            {
                result.Add(new XElement("fixedFaction", this.fixedFaction.defName));
            }
            result.Add(new XElement("isIncrease", this.isIncrease));
            result.Add(new XElement("value", this.value));
            result.Add(new XElement("sendLetter", this.sendLetter));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_ChangeGoodwillOfFaction.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (this.fixedFaction != null)
            {
                Faction.OfPlayer.TryAffectGoodwillWith(Find.FactionManager.FirstFactionOfDef(this.fixedFaction), this.isIncrease ? this.value : -this.value,this.sendLetter, this.sendLetter,null, targets.First().Value);
            }
            else
            {
                targets.ToList().ForEach(t =>
                {
                    if(t.Value.Thing is Thing thing && thing.Faction is Faction f)
                    Faction.OfPlayer.TryAffectGoodwillWith(f, this.isIncrease ? this.value : -this.value, this.sendLetter, this.sendLetter, this.eventDef, targets.First().Value);
                });
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.fixedFaction, "fixedFaction");
            Scribe_Values.Look(ref this.sendLetter, "sendLetter");
            Scribe_Values.Look(ref this.isIncrease, "isIncrease");
            Scribe_Values.Look(ref this.value, "value");
        }

        public HistoryEventDef eventDef = HistoryEventDefOf.GaveGift;
        public bool sendLetter;
        public FactionDef fixedFaction;
        public bool isIncrease;
        public string buffer;
        public int value;
    }
    public class CQFAction_StartMentalState : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("state", this.state.defName));
            if (this.stateTargetText != null && this.stateTargetText != "")
            {
                result.Add(new XElement("stateTargetText", this.stateTargetText));
            }
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_StartMentalState.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Pawn stateTarget = null;
            targets.ToList().ForEach(t =>
            {
                if (t.Key == this.stateTargetText && t.Value.Thing is Pawn p)
                {
                    stateTarget = p;
                }
            });
            if (stateTarget == null && GameTools.GetTargetFromQuestDatabase(quest, this.stateTargetText) is TargetInfo target2 && target2.Thing is Pawn p2)
            {
                stateTarget = p2;
            }
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is Pawn pawn)
                {
                    pawn.mindState.mentalStateHandler.TryStartMentalState(this.state, null, false,false, false,stateTarget);
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.state, "CQFAction_state");
            Scribe_Values.Look(ref this.stateTargetText, "CQFAction_stateTargetText");
        }

        public MentalStateDef state;
        public string stateTargetText;
    }
    public class CQFAction_AddThingActionTrigger : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.ThingChange;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_AddThingActionTrigger.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("key", this.key));
            result.Add(new XElement("mode", this.mode));
            result.Add(CQFSerialization.SaveList_Saveable(this.actions, "actions"));
            return result;
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var target in targets)
            {
                if (target.Value.Map is Map map)
                {
                    MapComponent_CustomMapData comp =
                        MapComponent_CustomMapData.GetComp(map);
                    if (comp.Triggers.Find(t => t.key == this.key) is ThingActionTrigger 
                        trigger)
                    {
                        trigger.things.Add(target.Value.Thing);
                    }
                    else 
                    {
                        comp.Triggers.Add(new ThingActionTrigger() {mode = this.mode,
                        key = this.key,actions = this.actions.ListFullCopy()});
                    }
                } 
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.key,"key");
            Scribe_Values.Look(ref this.mode, "mode");
            Scribe_Collections.Look(ref this.actions,"actions",LookMode.Deep);
        }

        public string key;
        public ActionTriggerMode mode = ActionTriggerMode.Damaged;
        public List<CQFAction> actions = new List<CQFAction>();
    }
    public class CQFAction_AddQuestTag : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.SignalState;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("tag", this.tag));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_AddQuestTag.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.tag, "tag");
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var item in targets)
            {
                if (item.Value.Thing is Thing t) 
                {
                    QuestUtility.AddQuestTag(ref t.questTags,"Quest" + quest.id + "." + this.tag);
                }
            }
        }

        public string tag;
    }
    public abstract class CQFAction_Lord : CQFAction
    {
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Lord.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public abstract void WorkForLord(Dictionary<string, TargetInfo> targets, Quest quest, Lord lord);

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (GameComponent_Editor.Instance.GetQuestData(quest) is QuestData data && data.Lords.TryGetValue(this.lordName, out Lord lord))
            {
                this.WorkForLord(targets, quest, lord);
            }
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("lordName", this.lordName));
            return result;
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.lordName, "lordName");
        }

        public string lordName;
    }
    public class CQFAction_Lord_Visit : CQFAction_Lord
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Faction;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Lord_Visit.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void WorkForLord(Dictionary<string, TargetInfo> targets, Quest quest, Lord lord)
        {
            IntVec3 chillSpot;
            Pawn p = targets.ToList().Find(t => t.Value.Thing is Pawn).Value.Thing as Pawn;
            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(p, out chillSpot))
            {
                chillSpot = CellFinder.RandomCell(p.Map);
            }
            Faction faction = GameTools.GetFaction(this.faction);
            lord.SetJob(new LordJob_VisitColony(faction, chillSpot, this.durationTicks));
            lord.GotoToil(lord.Graph.StartingToil);
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("faction", this.faction.defName));
            result.Add(new XElement("durationTicks", this.durationTicks));
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref faction, "faction");
            Scribe_Values.Look(ref this.durationTicks, "durationTicks");
        }
        internal FactionDef faction;
        internal int durationTicks;
        internal string buffer;
    }
    public class CQFAction_Pawn_RunDutyMapTransition : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Pawn_RunDutyMapTransition.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is Pawn pawn)
                {
                    if (this.dutyMap != null)
                    {
                        GameComponent_ComplexDuty.Instance.SetDutyMap(pawn, this.dutyMap, quest, true);
                    }
                    CustomDutyMap runtime = GameComponent_ComplexDuty.Instance.GetRuntime(pawn);
                    LordJob_ComplexCustom.GetForPawn(pawn)?.TryChangeByTransition(pawn, runtime?.CurrentNode?.nodeId, this.toNodeId, quest, targets);
                }
            });
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.dutyMap != null)
            {
                result.Add(new XElement("dutyMap", this.dutyMap.defName));
            }
            result.Add(new XElement("toNodeId", this.toNodeId));
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.dutyMap, "dutyMap");
            Scribe_Values.Look(ref this.toNodeId, "toNodeId");
            string legacyNodeId = null;
            Scribe_Values.Look(ref legacyNodeId, "nodeId");
            if (this.toNodeId.NullOrEmpty())
            {
                this.toNodeId = legacyNodeId;
            }
        }

        public DutyMapDef dutyMap;
        public string toNodeId;
    }

    public class CQFAction_Pawn_SetDutyMap : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_Pawn_SetDutyMap.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (this.dutyMap == null)
            {
                return;
            }
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is Pawn pawn)
                {
                    GameComponent_ComplexDuty.Instance.SetDutyMap(pawn, this.dutyMap, quest, this.useStartNode);
                }
            });
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.dutyMap != null)
            {
                result.Add(new XElement("dutyMap", this.dutyMap.defName));
            }
            result.Add(new XElement("useStartNode", this.useStartNode));
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.dutyMap, "dutyMap");
            Scribe_Values.Look(ref this.useStartNode, "useStartNode", true);
        }

        public DutyMapDef dutyMap;
        public bool useStartNode = true;
    }

    public class CQFAction_EndGame : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DialogEvent;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_EndGame.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            GenGameEnd.EndGameDialogMessage((this.message.CanTranslate() ? this.message.Translate().ToString() : this.message));
        }  
        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.message,"message");
        }


        [CQFLocalizableText]
        public string message;
    }
}
