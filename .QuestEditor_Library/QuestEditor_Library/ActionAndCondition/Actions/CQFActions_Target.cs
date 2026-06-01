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
            base.Draw(ref y, inRect, x);
            TooltipHandler.TipRegion(new Rect(x, y, 150f, 25f), "CQFTargetTextTip".Translate());
            CQFEditorTools.DrawSelectableStringList(this.targetsText, ref y, (rect, text, index) =>
              {
                  string text2 = text;
                  CQFEditorTools.DrawSelectableText(rect.y + 4.5f, "DialogueTarget".Translate(), ref text2, () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts,
                      t =>
                      {
                          text2 = t;
                          this.targetsText[index] = t;
                      }, t => t.Translate()), x + 5f, 150f);
                  this.targetsText[index] = text2;
              }, null, "CQFTargetTextTip".Translate(), true, x, 320f);
            y += 20f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFEditorTools.SaveList(this.targetsText, "targetsText"));
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rectData = new Rect(x + 5f, y, 600f, 25f);
            float initY = y;
            y += 5f;
            foreach (LootData data in this.datas)
            {
                if (Widgets.ButtonText(rectData, data.dataName, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable((IDrawable)data));
                }
                y += 30f;
                rectData.y += 30f;
            }
            y -= 5f;
            Widgets.DrawBox(new Rect(x, initY, inRect.width - 40f - (2 * x), y - initY), 1, QuestEditor_Dialog.blueTex);
            y += 7f;
            if (Widgets.ButtonText(new Rect(x + 10f, y, 150f, 25f), "AddNewLootData".Translate()))
            {
                this.datas.Add(new LootData());
            }
            if (Widgets.ButtonText(new Rect(x + 174f, y, 150f, 25f), "DeleteLootData".Translate()) && this.datas.Any())
            {
                CQFEditorTools.DrawFloatMenu(this.datas, (d) => this.datas.Remove(d), (d) => d.dataName);
            }
            y += 30f;
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawIntVector(ref y,"StartPosition".Translate(),
                ref this.pos,ref this.p_X,ref this.p_Z,ref this.p_Y,x,60f);
            y += 30f;
            if (Widgets.ButtonText(new Rect(x,y + 5f,100f,25f),"EditCustomMapGenerationSet".Translate(),false)) 
            {
                Find.WindowStack.Add(new Dialog_EditIDrawable(this.set));
            }
            y += 30f;
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

        string p_X;
        string p_Z;
        string p_Y;
        public IntVec3 pos = IntVec3.Zero;
        public CustomMapGenerationSet set = new CustomMapGenerationSet();
    }
    public class CQFAction_SpawnAndAddToInventory : CQFAction_Target
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rectData = new Rect(x + 5f, y, 600f, 25f);
            float initY = y;
            y += 5f;
            foreach (LootData data in this.datas)
            {
                if (Widgets.ButtonText(rectData, data.dataName, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable((IDrawable)data));
                }
                y += 30f;
                rectData.y += 30f;
            }
            y -= 5f;
            Widgets.DrawBox(new Rect(x, initY, inRect.width - 40f - (2 * x), y - initY), 1, QuestEditor_Dialog.blueTex);
            y += 7f;
            if (Widgets.ButtonText(new Rect(x + 10f, y, 150f, 25f), "AddNewLootData".Translate()))
            {
                this.datas.Add(new LootData());
            }
            if (Widgets.ButtonText(new Rect(x + 174f, y, 150f, 25f), "DeleteLootData".Translate()) && this.datas.Any())
            {
                CQFEditorTools.DrawFloatMenu(this.datas, (d) => this.datas.Remove(d), (d) => d.dataName);
            }
            y += 30f;
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
            base.Draw(ref y, inRect, x);
            Rect rectData = new Rect(x + 5f, y, 600f, 25f);
            float initY = y;
            y += 5f;
            foreach (LootData data in this.datas)
            {
                if (Widgets.ButtonText(rectData, data.dataName, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable((IDrawable)data));
                }
                y += 30f;
                rectData.y += 30f;
            }
            y -= 5f;
            Widgets.DrawBox(new Rect(x, initY, inRect.width - 40f - (2 * x), y - initY), 1, QuestEditor_Dialog.blueTex);
            y += 7f;
            if (Widgets.ButtonText(new Rect(x + 10f, y, 150f, 25f), "AddNewLootData".Translate()))
            {
                this.datas.Add(new LootData());
            }
            if (Widgets.ButtonText(new Rect(x + 174f, y, 150f, 25f), "DeleteLootData".Translate()) && this.datas.Any())
            {
                CQFEditorTools.DrawFloatMenu(this.datas, (d) => this.datas.Remove(d), (d) => d.dataName);
            }
            y += 30f;
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
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("value", this.value));
            result.Add(new XElement("alwaysIsOpposite", this.alwaysIsOpposite));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Widgets.CheckboxLabeled(new Rect(15f, y, 350f, 25f), "OpeningStatus".Translate(), ref this.value);
            y += 35f;
            Widgets.CheckboxLabeled(new Rect(15f, y, 350f, 25f), "AlwaysIsOpposite".Translate(), ref this.alwaysIsOpposite);
            y += 30f;
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line<float>(y,"PolluatingRadius".Translate(),ref this.radius,ref this.buffer,x);
            y += 30f;
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
    public class CQFAction_GetThingToRecord : CQFAction_RecordToDatabase
    {
        public override Dictionary<string, TargetInfo> GetTargetFromGaveTarget(Dictionary<string, TargetInfo> targets)
        {
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            targets.ToList().ForEach(t => 
            {
                if (t.Value.Map != null && t.Value.Cell.GetThingList(t.Value.Map).Find(t2 => t.Value.Thing == null || t2 != t.Value.Thing) is Thing t3) 
                {
                    result.Add(t.Key,t3);
                }
            });
            return result;
        }
    } 
    public class CQFAction_GetCellToRecord : CQFAction_RecordToDatabase
    {
        public override Dictionary<string, TargetInfo> GetTargetFromGaveTarget(Dictionary<string, TargetInfo> targets)
        {
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Map != null)
                {
                    result.Add(t.Key, new TargetInfo(t.Value.Cell, t.Value.Map));
                }
            });
            return result;
        }
    }
    public class CQFAction_AddExtraOpteration: CQFAction_Target
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x,y,250f,25f),this.option.interactionText,false)) 
            {
                Find.WindowStack.Add(new Dialog_InteractionOption(this.option));
            }
            y += 30f;
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is Thing thing && thing.Map?.GetComponent<MapComponent_CustomMapData>() is MapComponent_CustomMapData component)
                {
                    if (component.ExtraOperations.TryGetValue(thing, out List<InteractionOperation> os))
                    {
                        os.Add(this.option);
                    }
                    else 
                    {
                        CQFEditorTools.AddOrSetObjectToListFromDictionary(component.ExtraOperations,thing,this.option);
                    }
                }
            });
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.HasThing)
                {
                    Current.Game.GetComponent<GameComponent_Editor>().RemoveDialog(t.Value.Thing);
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x, y, 320f, 25f), "DialogManagerForSpawner".Translate(this.dialog?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, m => this.dialog = m, m => m.defName);
            }
            y += 30f;
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            targets.ToList().ForEach(t =>
            {
                if (t.Value.HasThing)
                {
                    Current.Game.GetComponent<GameComponent_Editor>().AddDialog(t.Value.Thing, this.dialog);
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
 CQFEditorTools.DrawEditableStringList(this.tags,ref y,"Tags".Translate());
            y += 30f;
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            var dialog = DefDatabase<DialogManagerDef>.AllDefsListForReading.FindAll(t
                => t.tags.Exists(t2 => this.tags.Contains(t2))).RandomElement();
            targets.ToList().ForEach(t =>
            {
                if (t.Value.HasThing)
                {
                    Current.Game.GetComponent<GameComponent_Editor>().AddDialog(t.Value.Thing,dialog );
                }
            });
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFEditorTools.SaveList(this.tags,"tags"));
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x, y, 250f, 25f), "CQFReplaceThing".Translate(this.data.stuff?.label + " " + this.data.def?.label), false))
            {
                this.data.OpenSelectDialog();
            }
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 150f, 25f), "UseSameStuff".Translate(), ref this.useSameStuff);
            y += 30f;
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            if (this.data != null && this.customThing == null) 
            {
                this.customThing = this.data.SpawnThing(null,null, out List<Thing> ts, null,true);;
            }
            if (Widgets.ButtonText(new Rect(x, y, 250f, 25f), 
                    "CQFReplaceThing".Translate(this.customThing == null ? "" : ((Thing)this.customThing).Label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<ThingDef>(Designator_CQFTools.Basespawnable.FindAll(sp =>
                        sp != QEDefOf.QE_Spawner_Editor && sp != QEDefOf.QE_ZoneCore),
             t => t.uiIcon, t =>  t.label.Colorize(ColorLibrary.SkyBlue)
                                  + "(" + t.thingClass.Name.Translate() + ")", "Select".Translate(),
             t =>
             {
                 string label = t.label;
                 if (t.MadeFromStuff)
                 {
                     Find.WindowStack.Add(new Dialog_Select<ThingDef>(GenStuff.AllowedStuffsFor(t).ToList(), s => s.uiIcon, s => s.label, "SelectStuff".Translate(), s =>
                     {
                         this.customThing = GameTools.MakeThingWithoutID(t,s);
                     }, s => s.graphic?.Color ?? Color.white, (s, r) => Widgets.DefIcon(r, s, null)));
                 }
                 else 
                 {
                     this.customThing = GameTools.MakeThingWithoutID(t);
                 }
             }, t => t.graphic?.Color ?? Color.white, (t, r) => Widgets.DefIcon(r, t, null)));
            }
            y += 30f;
            if (this.customThing != null) 
            {
                if (Widgets.ButtonText(new Rect(x, y, 250f, 25f), "EditCustomThing".Translate(), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawTabable((IDrawTabable)this.customThing));
                }
                y += 30f;
                if (Widgets.ButtonText(new Rect(x, y, 250f, 25f), "EditActionAndText".Translate(), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIActionAndText((Thing)this.customThing));
                }
            }
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 150f, 25f), "UseSameStuff".Translate(), ref this.useSameStuff);
            y += 30f;
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            if (this.data != null && this.customThing == null)
            {
                this.customThing = this.data.SpawnThing(null, null, out List<Thing> ts, null, true);
                ;
            }

            if (Widgets.ButtonText(new Rect(x, y, 250f, 25f),
                    "CQFSpawnThing".Translate(this.customThing == null ? "" : ((Thing)this.customThing).Label),
                    false))
            {
                Find.WindowStack.Add(new Dialog_Select<ThingDef>(
                    Designator_CQFTools.Basespawnable.FindAll(sp =>
                        sp != QEDefOf.QE_Spawner_Editor && sp != QEDefOf.QE_ZoneCore),
                    t => t.uiIcon, t => t.label.Colorize(ColorLibrary.SkyBlue) 
                                        + "(" + t.thingClass.Name.Translate() + ")", "Select".Translate(),
                    t =>
                    {
                        string label = t.label;
                        if (t.MadeFromStuff)
                        {
                            Find.WindowStack.Add(new Dialog_Select<ThingDef>(GenStuff.AllowedStuffsFor(t).ToList(),
                                s => s.uiIcon, s => s.label , "SelectStuff".Translate(),
                                s => { this.customThing = GameTools.MakeThingWithoutID(t, s); },
                                s => s.graphic?.Color ?? Color.white, (s, r) =>
                                    Widgets.DefIcon(r, s, null)));
                        }
                        else
                        {
                            this.customThing = GameTools.MakeThingWithoutID(t);
                        }
                    }, t => t.graphic?.Color ?? Color.white, (t, r) => Widgets.DefIcon(r, t, null)));
            }

            y += 30f;
            if (this.customThing != null)
            {
                if (Widgets.ButtonText(new Rect(x, y, 250f, 25f), "EditCustomThing".Translate(), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawTabable((IDrawTabable)this.customThing));
                }

                y += 30f;
                if (Widgets.ButtonText(new Rect(x, y, 250f, 25f), "EditActionAndText".Translate(), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIActionAndText((Thing)this.customThing));
                }

                y += 30f;
            }
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref this.key, x, 150f);
            y += 30f; 
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
                        GameComponent_Editor.Component.GetQuestData(quest).RecordTarget(this.key,thing);
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
            base.Draw(ref y, inRect, x);
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
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("faction", this.faction.defName));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            Widgets.Label(rect, "Faction".Translate() + ":" + this.faction?.label);
            rect.x = 160f;
            if (Widgets.ButtonText(rect, "Select".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<FactionDef>.AllDefsListForReading, f => this.faction = f, f => f.label);
            }
            y += 30f;
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
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("duty", this.duty.defName));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            if (Widgets.ButtonText(rect, "DutyType".Translate(this.duty != null && this.duty.HasModExtension<ModExtension_CustomDuty>() ? this.duty?.label : this.duty?.defName.Translate().ToString()), false))
            {
                Find.WindowStack.Add(new Dialog_Select<DutyDef>(DefDatabase<DutyDef>.AllDefsListForReading,
                    null, (d) => d == null ? null : d.HasModExtension<ModExtension_CustomDuty>() ? d.label : d.defName.Translate().ToString(), "Select".Translate(), (d) => this.duty = d,
                    null, null, d => d.description, d => d.HasModExtension<ModExtension_CustomDuty>() || d.defName.CanTranslate() ? 1 : 5));
            }
            y += 30f;
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
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("xenotype", this.xenotype.defName));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            if (Widgets.ButtonText(rect, "CQFXenotypeDef".Translate(this.xenotype?.label), false))
            {
                CQFEditorTools.DrawFloatMenu<XenotypeDef>(DefDatabase<XenotypeDef>.AllDefsListForReading, (d) => this.xenotype = d, (d) => d.label.Translate());
            }
            y += 30f;
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
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            if (Widgets.ButtonText(rect, "GivenHediff".Translate() + this.hediff?.label, false))
            {
                Find.WindowStack.Add(new Dialog_Select<HediffDef>(DefDatabase<HediffDef>.AllDefsListForReading, null, t => t.label, "Select".Translate(), t =>
                {
                    this.hediff = t;
                }));
            }
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line<float>(y, "SeverityOfHediff".Translate(), ref this.severity, ref this.buffer, x);
            y += 30f;
            if (Widgets.ButtonText(new Rect(x, y, 350f, 25f), "CQFBodyPartForHediff".Translate(this.labelBuffer ?? "FullBody".Translate()), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<BodyDef>.AllDefsListForReading, b =>
                {
                    CQFEditorTools.DrawFloatMenu(b.AllParts, h =>
                    {
                        this.bodyPart = h.def;
                        this.customLabel = h.untranslatedCustomLabel;
                        this.labelBuffer = h.customLabel ?? h.def.label;
                    }, h => h.customLabel ?? h.def.label);
                }, b => b.label);
            }
            y += 30f;
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
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("ability", this.ability.defName)); 
            return result;
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x); 
            if (Widgets.ButtonText(new Rect(x,y,inRect.width,25f),
                    "CQFAbilityDef".Translate(this.ability?.label),
                    false))
            {
                Find.WindowStack.Add(new Dialog_Select<AbilityDef>(DefDatabase<AbilityDef>.AllDefsListForReading, null,
                    t => t.label, "Select".Translate(), t =>
                    {
                        this.ability = t;
                    }));
            }

            y += 30f;
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
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("trait", this.trait.defName));
            result.Add(new XElement("degree", this.degree));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);

            Rect rect = new Rect(x, y, 150f, 25f);
            List<KeyValuePair<TraitDef, TraitDegreeData>> stagets = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
            DefDatabase<TraitDef>.AllDefsListForReading.ForEach(t =>
            {
                t.degreeDatas.ForEach(s =>
                {
                    stagets.Add(new KeyValuePair<TraitDef, TraitDegreeData>(t, s));
                });
            });
            if (Widgets.ButtonText(rect, "RequiredTrait".Translate(this.trait?.degreeDatas.Find(d => d.degree == this.degree)?.label),false))
            {
                Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, null, t => t.Value.label, "Select".Translate(), t =>
                {
                    this.trait = t.Key;
                    this.degree = t.Value.degree;
                }));
            }
            y += 30f;
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
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("trait", this.trait.defName));
            result.Add(new XElement("degree", this.degree));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);

            Rect rect = new Rect(x, y, 150f, 25f);
            List<KeyValuePair<TraitDef, TraitDegreeData>> stagets = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
            DefDatabase<TraitDef>.AllDefsListForReading.ForEach(t =>
            {
                t.degreeDatas.ForEach(s =>
                {
                    stagets.Add(new KeyValuePair<TraitDef, TraitDegreeData>(t, s));
                });
            });
            if (Widgets.ButtonText(rect, "RequiredTrait".Translate(this.trait?.degreeDatas.Find(d => d.degree == this.degree)?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, null, t => t.Value.label, "Select".Translate(), t =>
                {
                    this.trait = t.Key;
                    this.degree = t.Value.degree;
                }));
            }
            y += 30f;
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
            base.Draw(ref y, inRect, x);

            Rect rect = new Rect(x, y, 150f, 25f);
            if (Widgets.ButtonText(rect,
                    "GiveTrait".Translate(this.trait?.defName),
                    false))
            {
                Find.WindowStack.Add(new Dialog_Select<TraitDef>(DefDatabase<TraitDef>.AllDefsListForReading, null,
                    t => t.defName, "Select".Translate(), t =>
                    {
                        this.trait = t;
                    },null,null, t =>
                    {
                        StringBuilder tip = new StringBuilder();
                       foreach (var traitDegreeData in t.degreeDatas)
                       {
                           tip.AppendLine(traitDegreeData.label);
                       }
                        return tip.ToString().Trim();
                    }));
            } 
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y,"InitDegree".Translate(),
                ref initDegree,ref buffer,x,100f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y,"InitMessage".Translate(),
                ref initMessage,x,100f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y,"CQFMessage".Translate(),
                ref message,x,100f);
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
                            Messages.Message(this.message.Translate(targetPawn.Label
                                    ,t.Label,trait.Label)
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
                        Messages.Message(this.initMessage.Translate(targetPawn.Label,trait.Label)
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
        public string initMessage;
        public string message;
        public int initDegree = 0;
        public TraitDef trait;
    }

    public class CQFAction_Explosion : CQFAction_Target
    {
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
            base.Draw(ref y, inRect, x);

            Rect rect = new Rect(x, y, 350f, 25f);
            if (Widgets.ButtonText(rect, "ExplostionDamageType".Translate() + this.damage?.label, false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DamageDef>.AllDefsListForReading, d => this.damage = d, d => d.label);
            }
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line<int>(y, "DamageAmount".Translate(), ref this.amount, ref this.buffer, x);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line<float>(y, "ExplosionRadius".Translate(), ref this.radius, ref this.bufferR, x);
            y += 30f;
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            if (Widgets.ButtonText(rect, "CQF_EffectDef".Translate(this.effect?.label ?? this.effect?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<EffecterDef>.AllDefsListForReading, d => this.effect = d, d => d.label ?? d.defName);
            }
            y += 30f;
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            if (Widgets.ButtonText(rect, "CQF_MoteDef".Translate(this.mote?.defName), false))
            {
                Find.WindowStack.Add(
                    new Dialog_Select<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll(d
                        => d.category == ThingCategory.Mote),null,
                        d => d.defName,"Select".Translate()
                        , d => this.mote = d));
            }
            y += 30f;
            CQFEditorTools.DrawVector(ref y, "MoteOffset".Translate(), ref this.off, ref buffer, ref buffer2, ref buffer3, x, 40f); 
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "MoteScale".Translate(), ref this.scale, ref bufferS, x, 80f);
            y += 30f;
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
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("damage", this.damage.defName));
            result.Add(new XElement("amount", this.amount));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);

            Rect rect = new Rect(x, y, 350f, 25f);
            if (Widgets.ButtonText(rect, "DamageType".Translate() + this.damage?.label, false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DamageDef>.AllDefsListForReading, d => this.damage = d, d => d.label);
            }
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line<float>(y, "DamageAmount".Translate(), ref this.amount, ref this.buffer, x);
            y += 30f;
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
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("thought", this.thought.defName));
            result.Add(new XElement("stage", this.stage));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);

            Rect rect = new Rect(x, y, 350f, 25f);
            List<KeyValuePair<ThoughtDef, ThoughtStage>> stagets = new List<KeyValuePair<ThoughtDef, ThoughtStage>>();
            DefDatabase<ThoughtDef>.AllDefsListForReading.ForEach(t =>
            {
                if (t.IsMemory) 
                {
                    t.stages.ForEach(s =>
                    {
                        stagets.Add(new KeyValuePair<ThoughtDef, ThoughtStage>(t, s));
                    });
                }
            });
            if (Widgets.ButtonText(rect, "CQF_ThoughtDef".Translate(this.thought?.stages.Count > this.stage ? this.thought?.stages[this.stage].label : ""), false))
            {
                Find.WindowStack.Add(new Dialog_Select<KeyValuePair<ThoughtDef, ThoughtStage>>(stagets, null, t => t.Value?.label, "Select".Translate(), t =>
                   {
                       this.thought = t.Key;
                       if (t.Key.stages.Contains(t.Value))
                       {
                           this.stage = t.Key.stages.IndexOf(t.Value);
                       }
                       else
                       {
                           Log.Message("CQF Action Gain Mood Error:A thoughtstage without thought");
                       }
                   }));
            }
            y += 30f;
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
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            if (Widgets.ButtonText(rect, "SkillType".Translate(this.skill == null ? "Random".Translate().ToString() : this.skill?.label), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<SkillDef>.AllDefsListForReading, d => this.skill = d, d => d.label, new List<FloatMenuOption>() { new FloatMenuOption("Random".Translate().ToString(), () => this.skill = null) });
            }
            y += 30f;
            CQFEditorTools.DrawFloatRange(ref y, "GainExperienceRange".Translate(), ref this.experienceRange, ref this.buffer, ref this.maxBuffer, x, 100f);
            y += 30f;
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
            base.Draw(ref y, inRect, x);
            Widgets.CheckboxLabeled(new Rect(x,y,150f,25f), "IsPermanent".Translate(), ref this.permanent);
            y += 30f;
            if (!this.permanent)
            {
                CQFEditorTools.DrawIntRange(ref y, "Duration".Translate(),
                    ref this.duration, ref this.buffer, ref this.maxBuffer, x, 100f);
            }
            if (Widgets.ButtonText(new Rect(x, y, 150f, 25f), 
                "CQFGameConditionDef".Translate(this.condition?.label), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<GameConditionDef>.AllDefsListForReading,
                    f => this.condition = f, f => f.label);
            }
            y += 30f;

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
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions,"actions"));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Widgets.CheckboxLabeled(new Rect(x,y,150f,25f), "IsPermanent".Translate(), ref this.permanent);
            y += 30f;
            if (!this.permanent)
            {
                CQFEditorTools.DrawIntRange(ref y, "Duration".Translate(),
                    ref this.duration, ref this.buffer, ref this.maxBuffer, x, 100f);
            }
            if (Widgets.ButtonText(new Rect(x, y, 150f, 25f), 
                "CQFGameConditionDef".Translate(this.condition?.label), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<GameConditionDef>.AllDefsListForReading.FindAll(c =>
                        typeof(GameCondition_Actions).IsAssignableFrom(c.conditionClass)),
                    f => this.condition = f, f => f.label);
            }
            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x,y,360f,30f),"UseTick".Translate(),ref this.useTick);
            y += 35f;
            if (useTick)
            {
                CQFEditorTools.DrawLabelAndText_Line(y,
                    "TickToTrigger".Translate(),ref this.tick,ref tickBuffer);
                y += 35f;
            }
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
            result.Add(CQFEditorTools.SaveList_Saveable(this.comps, "comps"));
            return result;
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x); 
            if (Widgets.ButtonText(new Rect(x, y, 150f, 25f),
                    "GivenHediff".Translate() + this.hediff?.label, false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<HediffDef>.AllDefsListForReading.FindAll(c =>
                        typeof(CustomHediff).IsAssignableFrom(c.hediffClass)),
                    f => this.hediff = f, f => f.label);
            } 
            y += 30f; 
            CQFEditorTools.DrawLabelAndText_Line(y,"CQF_CustomName".Translate(),ref label,x,100f);
            y += 30f; 
            CQFEditorTools.DrawLabelAndText_Line(y,"CQF_CustomDescription".Translate(),ref desc,x,100f);
            y += 30f; 
            CQFEditorTools.DrawSelectColorButtons(ref y,"HediffColor".Translate(),this.color,c => 
                this.color = c,x);
            y += 5f; 
            CQFEditorTools.DrawIDrawList_UseWindow(ref y,x,this.comps,inRect,"ActionComps".Translate(),
                c => c.compName, t =>
                {
                    t.allowedActions = Allows;
                });
            y += 30f;
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

                        hd.overridedLabel = this.label.Translate();
                        hd.overridedDescription = this.desc.Translate();
                        hd.overridedColor = this.color;
                    }
                    else
                    {
                        CustomHediff h = (CustomHediff)pawn.health.AddHediff(this.hediff);
                        foreach (var actionComp in this.comps)
                        {
                            h.comps.Add(actionComp.Copy());   
                        }
                        h.overridedLabel = this.label.Translate();
                        h.overridedDescription = this.desc.Translate();
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

        public string label;
        public string desc;
        public Color color = Color.white;
        public List<ActionComp> comps = new List<ActionComp>();
        public HediffDef hediff;
    }

    public class CQFAction_Destory : CQFAction_Target
    {
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
            result.Add(CQFEditorTools.SaveList_Saveable(this.requirations, "requirations"));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawIDrawList(ref y, x, this.requirations, inRect, "RequiredThings".Translate(), () =>
CQFEditorTools.DrawFloatMenu(new List<Type>() { typeof(CQFThingDefCount) }, t =>
{
    CQFThingData.OpenSelectWindow(t, d => this.requirations.Add(d));
}, t => t.Name.Translate()), t => t.ToString(), (t, y2, rect, x2) =>
{
    t.DrawWithSingleCount(ref y2, rect, x2);
    return y2;
});
            y += 5f;
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
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            Widgets.Label(rect, "Faction".Translate() + ":" + this.fixedFaction?.label);
            rect.x = 160f;
            if (Widgets.ButtonText(rect, "Select".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<FactionDef>.AllDefsListForReading, f => this.fixedFaction = f, f => f.label,new List<FloatMenuOption>() {new FloatMenuOption("Null".Translate(),() => this.fixedFaction = null)});
            }
            rect.y += 30f;
            rect.x = x;
            y += 30f;
            Widgets.CheckboxLabeled(rect,"IsIncrease".Translate(),ref this.isIncrease);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y,"GoodwillValue".Translate(),ref this.value,ref this.buffer,x);
            y += 30f;
            rect.y += 60f;
            Widgets.CheckboxLabeled(rect, "SendLetter".Translate(), ref this.sendLetter);
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
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            if (Widgets.ButtonText(rect, "CQFMentalState".Translate(this.state?.label), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<MentalStateDef>.AllDefsListForReading, f => this.state = f, f => f.label);
            }
            y += 30f;
            if (this.state == MentalStateDefOf.SocialFighting)
            {
                CQFEditorTools.DrawSelectableText(y, "stateTargetText".Translate(), ref this.stateTargetText, () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.stateTargetText = t, t => t.Translate()), x, 150f);
                y += 30f;
            }
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
    public class CQFAction_RecordToGroup : CQFAction_Target
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("recordKey", this.recordKey)); 
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref this.recordKey, x, 150f);
            y += 30f; 
        }
        public virtual Dictionary<string, TargetInfo> GetTargetFromGaveTarget(Dictionary<string, TargetInfo> targets) 
        {
            return targets;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.recordKey, "CQFAction_Record_recordKey"); 
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            List<Thing> result = new List<Thing>();
            this.GetTargetFromGaveTarget(targets).ToList().ForEach(t =>
            {
                if (t.Value.Thing is {} thing)
                {
                    result.Add(thing);
                }
            });
            GameComponent_Editor.Component.GetQuestData(quest).AddGroup(this.recordKey,result);
        }

        public string recordKey; 
    }
    public class CQFAction_RecordToDatabase : CQFAction_Target
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("recordKey", this.recordKey));
            if (this.recordToTemporaryBase) 
            {
                result.Add(new XElement("recordToTemporaryBase", this.recordToTemporaryBase));
            }
            if (this.recordToQuestBase)
            {
                result.Add(new XElement("recordToQuestBase", recordToQuestBase));
            }
            if (this.recordToGlobalBase)
            {
                result.Add(new XElement("recordToGlobalBase", this.recordToGlobalBase));
            }
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref this.recordKey, x, 150f);
            y += 30f;
            Rect rect = new Rect(x, y, 350f, 25f);
            Widgets.CheckboxLabeled(rect, "RecordToTemporaryBase".Translate(), ref this.recordToTemporaryBase);
            TooltipHandler.TipRegion(rect, "RecordToTemporaryBase_Tip".Translate());
            y += 30f;
            rect.y += 30f;
            Widgets.CheckboxLabeled(rect, "RecordToQuestBase".Translate(), ref this.recordToQuestBase);
            y += 30f;
            rect.y += 30f;
            Widgets.CheckboxLabeled(rect, "RecordToGlobalBase".Translate(), ref this.recordToGlobalBase);
            y += 30f;
        }
        public virtual Dictionary<string, TargetInfo> GetTargetFromGaveTarget(Dictionary<string, TargetInfo> targets) 
        {
            return targets;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.recordKey, "CQFAction_Record_recordKey");
            Scribe_Values.Look(ref this.recordToQuestBase, "recordToQuestBase");
            Scribe_Values.Look(ref this.recordToTemporaryBase, "CQFAction_Record_recordToTemporaryBase");
            Scribe_Values.Look(ref this.recordToGlobalBase, "recordToGlobalBase");
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            this.GetTargetFromGaveTarget(targets).ToList().ForEach(t =>
            {
                if (this.recordToTemporaryBase)
                {
                    GameTools.AddTemporaryTagret(this.recordKey, t.Value);
                }
                if (this.recordToQuestBase)
                {
                    GameComponent_Editor.Component.GetQuestData(quest)?.RecordTarget(recordKey, t.Value);
                }
                if (this.recordToGlobalBase) 
                {
                    GameComponent_Editor.Component.GlobalDatabase.RecordTarget(recordKey, t.Value);
                }
            });
        }

        public string recordKey;
        public bool recordToQuestBase = false;
        public bool recordToTemporaryBase = false;
        public bool recordToGlobalBase = false;
    }
    public class CQFAction_RecordStartCell : CQFAction_Target
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(),
                ref this.recordKey, x, 150f);
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("recordKey", this.recordKey));
            return result;
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var target in targets)
            {
                if (target.Value.IsValid && target.Value.Map is Map map) 
                {
                    MapComponent_CustomMapData.GetComp(map).StartCells.SetOrAdd(this.recordKey,target.Value.Cell);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.recordKey, "recordKey");
        }

        public string recordKey;
    }
    public class CQFAction_FinishRect : CQFAction_Target
    {  
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(),
                ref this.recordKey, x, 150f);
            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("recordKey", this.recordKey));
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            return result;
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var target in targets)
            {
                if (target.Value.IsValid && target.Value.Map is Map map)
                {
                    if (MapComponent_CustomMapData.GetComp(map) is { } comp
                        && comp.StartCells.ContainsKey(this.recordKey))
                    {
                        IntVec3 start = comp.StartCells[this.recordKey];
                        CellRect rect = CellRect.FromLimits(start,target.Value.Cell);
                        foreach (var cell in rect)
                        {
                            foreach (var action in this.actions)
                            {
                                action.Work(new Dictionary<string, TargetInfo>() 
                                {
                                    ["Position"] = new TargetInfo(cell,target.Value.Map)
                                },quest);
                            }
                        }
                        comp.StartCells.Remove(this.recordKey);
                    } 
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.recordKey, "recordKey");
            Scribe_Collections.Look(ref this.actions,"actions",LookMode.Deep);
        }

        public string recordKey;
        public List<CQFAction> actions = new List<CQFAction>();
    }

    public class CQFAction_DoActionForGroup : CQFAction
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(),
                ref this.recordKey, x, 150f);
            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(),
                a => a.GetType().Name.Translate());
            y += 30f;
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("recordKey", this.recordKey));
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            return result;
        }

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var target in GameComponent_Editor.Component.GetQuestData(quest).GetGroup(this.recordKey))
            {
                foreach (var action in this.actions)
                {
                    action.Work(new Dictionary<string, TargetInfo>()
                    {
                        ["Target"] = new TargetInfo(target)
                    }, quest);
                }
            }
        }

        public override void ExposeData()
        { 
            Scribe_Values.Look(ref this.recordKey, "recordKey");
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }

        public string recordKey;
        public List<CQFAction> actions = new List<CQFAction>();
    }

    public class CQFAction_AddThingActionTrigger : CQFAction_Target
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(),
                ref this.key, x, 150f);
            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
            CQFEditorTools.DrawSelectButton(x,ref y,
                "TriggerMode".Translate((("ActionTriggerMode_" + this.mode.ToString()).Translate())),
                new List<ActionTriggerMode>() {ActionTriggerMode.Damaged},m => this.mode = m,
                m => ("ActionTriggerMode_" + m.ToString()).Translate());
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("key", this.key));
            result.Add(new XElement("mode", this.mode));
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
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
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("tag", this.tag));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "QuestTag".Translate(), ref this.tag, x, 150f);
            y += 30f;
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
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "LordName".Translate(), ref this.lordName, x, 150f);
            TooltipHandler.TipRegion(new Rect(x, y, 150f, 25f), "LordNameTip".Translate());
            y += 30f;
        }
        public abstract void WorkForLord(Dictionary<string, TargetInfo> targets, Quest quest, Lord lord);

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (GameComponent_Editor.Component.GetQuestData(quest) is QuestData data && data.Lords.TryGetValue(this.lordName, out Lord lord))
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
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            if (Widgets.ButtonText(rect, "RequiredFaction".Translate(this.faction?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<FactionDef>(DefDatabase<FactionDef>.AllDefsListForReading, null, t => t.label, "Select".Translate(), t =>
                {
                    this.faction = t;
                }));
            }
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "durationTicks".Translate(), ref this.durationTicks, ref this.buffer, x, 150f);
            y += 30f;
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

        private FactionDef faction;
        private int durationTicks;
        private string buffer;
    }
    public class CQFAction_EndGame : CQFAction
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y,"CQFAction_EndGame_Message".Translate(), ref this.message,x,150f);
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            GenGameEnd.EndGameDialogMessage(this.message.Translate());
        }  
        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.message,"message");
        }


        public string message;
    }
}
