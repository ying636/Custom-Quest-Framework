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
    //
    //
    //
    //
    //
    //
    //
    public static class GameTools
    {
        public static void AddTemporaryTagret(string name,TargetInfo target) 
        {
            GameComponent_Editor.Instance.TemporaryDatabase.RecordTarget(name, target);
        }
        public static void ClearTemporaryTargets()
        {
            GameComponent_Editor.Instance.ClearTemporaryDatabase();
        }
        public static void ResetTemporaryTargets()
        {
            GameComponent_Editor.Instance.ResetTemporaryDatabase();
        }
        public static Map GenerateSubMap(IntVec3 size,PocketMapParent parent, MapGeneratorDef generatorDef, IEnumerable<GenStepWithParams> extraGenStepDefs, Map sourceMap)
        {
            parent.sourceMap = sourceMap;
            Map result = MapGenerator.GenerateMap(size, parent, generatorDef, extraGenStepDefs, null, true);
            Find.World.pocketMaps.Add(parent);
            return result;
        }
        public static void FogMap(Map map) 
        {
            map.fogGrid.Refog(CellRect.WholeMap(map));
            if (Current.ProgramState == ProgramState.Playing)
            {
                map.roofGrid.Drawer.SetDirty();
            }
        }
        public static Thing MakeThingWithoutID(ThingDef def, ThingDef stuff = null)
        {
            if (stuff != null && !stuff.IsStuff)
            {
                stuff = GenStuff.DefaultStuffFor(def);
            }
            if (def.MadeFromStuff && stuff == null)
            {
                stuff = GenStuff.DefaultStuffFor(def);
            }
            if (!def.MadeFromStuff && stuff != null)
            {
                stuff = null;
            }
            Thing thing = (Thing)Activator.CreateInstance(def.thingClass);
            thing.def = def;
            if (thing is ThingWithComps thingWithComp)
            {
                thingWithComp.InitializeComps();
            }
            if (thing.def.useHitPoints)
            {
                thing.HitPoints = Mathf.RoundToInt((float)thing.MaxHitPoints * Mathf.Clamp01(thing.def.startingHpRange.RandomInRange));
            }
            thing.SetStuffDirect(stuff);
            return thing;
        }
        public static Dictionary<string, TargetInfo> GetTargets(Dictionary<string, TargetInfo> targets, Quest quest, List<string> targetTexts)
        {
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            targetTexts.ForEach(t =>
            {
                if (GetTarget(targets, quest, t) is TargetInfo target) 
                {
                    result.Add(t, target);
                }
                int i = 0;
                GetTargetsFromGroup(quest, t)?.ForEach(p => 
                {
                    result.Add(t + i, p);
                    i ++;
                });
            });
            return result;
        }
        public static TargetInfo GetTarget(Dictionary<string, TargetInfo> targets, Quest quest, string targetText)
        {
            if (targetText == null)
            {
                return null;
            }
            TargetInfo target = null;
            if (targets != null)
            {
                targets.ToList().ForEach(t =>
                {
                    if (t.Key == targetText)
                    {
                        target = t.Value;
                    }
                });
            }
            if (!target.IsValid)
            {
                target = MapComponent_CQFTargets.Resolve(targets, quest, targetText);
            }
            if (target == null && GameTools.GetTargetFromQuestDatabase(quest, targetText) is TargetInfo target2)
            {
                target = target2;
            }
            if (target == null && GameTools.GetTargetFromTemporaryDatabase(targetText) is TargetInfo target3)
            {
                target = target3;
            }
            if (target == null && GameTools.GetTargetFromGlobalDatabase(quest, targetText) is TargetInfo target4)
            {
                target = target4;
            }
            return target;
        }
        public static TargetInfo GetTargetFromTemporaryDatabase(string targetText)
        {
            if (targetText == null)
            {
                return null;
            }
            return GameComponent_Editor.Instance.TemporaryDatabase.GetTarget(targetText);
        }
        public static TargetInfo GetTargetFromQuestDatabase(Quest quest, string targetText)
        {
            if (quest == null || targetText == null) 
            {
                return null;
            }
            
            TargetInfo? result = null;
            string[] ts = targetText.Split(new char[] { '.' });
            if (ts.Count() >= 2 && int.TryParse(ts.Last(), out int index0))
            {
                result = result ?? GetTargetWithIndex(quest, ts.First(), index0);
            }
            QuestData data = GameComponent_Editor.Instance.GetQuestData(quest);
            if (data != null)
            {
                if (data.GetGroup(targetText) is {} ps)
                {
                    result = result ?? ps.First();
                }
                if (data.TargetDatas.Find(t => t.key == targetText) is TargetWithKey target)
                {
                    result = result ?? target.target;
                }
            }
            if (DebugSettings.godMode)
            {
                StringBuilder debug = new StringBuilder();
                debug.AppendLine($"正在搜索目标，使用文本：{targetText}"); 
                ts.ToList().ForEach(t0 =>debug.AppendLine(t0));
                debug.AppendLine($"结果：{result}");
                Log.Message(debug.ToString().Trim());
            }
            return result ?? TargetInfo.Invalid;
        }
        public static TargetInfo GetTargetFromGlobalDatabase(Quest quest, string targetText)
        {
            if (quest == null)
            {
                return null;
            }
            TargetInfo? result = null;
            string[] ts = targetText.Split(new char[] { '.' });
            if (DebugSettings.godMode)
            {
                Log.Message(targetText);
                ts.ToList().ForEach(t0 => Log.Message(t0));
            }
            if (ts.Count() >= 2 && int.TryParse(ts.Last(), out int index0))
            {
                result = result ?? GetTargetWithIndex(quest, ts.First(), index0);
            }
            QuestData data = GameComponent_Editor.Instance.GlobalDatabase;
            if (data != null)
            {
                if (data.GetGroup(targetText) is {} ps)
                {
                    result = result ?? ps.First();
                }
                if (data.TargetDatas.Find(t => t.key == targetText) is TargetWithKey target)
                {
                    result = result ?? target.target;
                }
            }
            return result ?? TargetInfo.Invalid;
        }
        public static List<TargetInfo> GetTargetsFromGroup(Quest quest, string targetText)
        {
            QuestData data = GameComponent_Editor.Instance.GetQuestData(quest) ?? GameComponent_Editor.Instance.GlobalDatabase;
            if (DebugSettings.godMode && data != null)
            {
                Log.Message(data.ToString());
            }
            if (data != null && data.GetGroup(targetText) is {} ps)
            {
                List<TargetInfo> result = new List<TargetInfo>();
                ps.ForEach(p => result.Add(p));
                return result;
            }
            return null;
        }
        public static TargetInfo GetTargetWithIndex(Quest quest, string targetText, int index)
        {
            TargetInfo? result = null;
            QuestData data = GameComponent_Editor.Instance.GetQuestData(quest);
            if (DebugSettings.godMode && data != null)
            {
                Log.Message(data.ToString());
            }
            if (data != null && data.GetGroup(targetText) is {} ps)
            {
                result = result ?? new TargetInfo(ps[index]);
            }
            return result.Value;
        }
        public static Quest GetQuestFromMap(Map map)
        {
            Quest result = null;
            if (map == null)
            {
                return result;
            }
            if (map.Parent is CustomSite site)
            {
                result = result ?? site.quest;
            }
            if (map.Parent is MapParent_Custom parent)
            {
                result = result ?? parent.quest;
            }
            return result;
        }
        public static Quest GetQuestFromThing(Thing t)
        {
            Quest result = null;
            if (t == null)
            {
                return result;
            }
            if (t.questTags != null && t.questTags.Any())
            {
                List<string> tags = t.questTags.FindAll(t2 => t2.StartsWith("Quest"));
                foreach (string t3 in tags)
                {
                    string t4 = t3.Split('.').First().Remove(0, 5);
                    if (int.TryParse(t4, out int id) && Find.QuestManager.QuestsListForReading.Find(q => q.id == id) is Quest quest)
                    {
                        result = result ?? quest;
                    }
                }


            }
            if (t.Spawned && result == null)
            {
                result = GetQuestFromMap(t.Map);
            }
            return result;
        }
        public static List<Thing> AllConsumableThing(Map map)
        {
            return map.listerThings.AllThings.FindAll(t => !t.IsForbidden(Faction.OfPlayer) && !t.Position.Fogged(map)).ListFullCopy();
        }
        public static List<Thing> AllConsumableThingForDef(ThingDef def, Map map)
        {
            return map.listerThings.ThingsOfDef(def).FindAll(t => !t.IsForbidden(Faction.OfPlayer) && !t.Position.Fogged(map)).ListFullCopy();
        }
        public static bool CheckRequiredThings(List<CQFThingData> requiredThings, List<Thing> things
            , out ThingDef def, out int count,out int limit)
        {
            Dictionary<ThingDef, int> counts = new Dictionary<ThingDef, int>();
            requiredThings.ForEach(d => counts.Add(((CQFThingDefCount)d).thing, d.count.min));
            foreach (Thing t in things)
            {
                if (counts.ContainsKey(t.def))
                {
                    counts[t.def] -= t.stackCount;
                }
            }
            if (counts.ToList().Find(c => c.Value >= 1) 
                    is KeyValuePair<ThingDef, int> thing && thing.Key != null)
            {
                def = thing.Key;
                limit = requiredThings.Find(t => t is CQFThingDefCount tc && tc.thing == thing.Key).count.min;
                count = limit - thing.Value;
                return false;
            }
            def = null;
            count = 0;
            limit = 0;
            return true;
        }
        public static void ConsumeRequiredThings(Pawn interviewer, Pawn interviewee, List<CQFThingData> requiredThings)
        {
            if (requiredThings.Any() && interviewee != null)
            {
                if (interviewee.Map.IsPlayerHome)
                {
                    requiredThings.ForEach(d =>
                    {
                        GameTools.ConsumeThings(((CQFThingDefCount)d).thing, d.count.min, interviewee.Map, null);
                    });
                }
                else
                {
                    Dictionary<ThingCategoryDef, int> categoryAndCount = new Dictionary<ThingCategoryDef, int>();
                    foreach (CQFThingData data in requiredThings)
                    {
                        if (data is CQFThingDefCount tData)
                        {
                            interviewee?.inventory?.innerContainer.Take(interviewee?.inventory.
                                innerContainer.ToList().Find(i => i.def == tData.thing), tData.count.min).Destroy();
                        }
                        if (data is CQFThingCategoryCount cData)
                        {
                            categoryAndCount.Add(cData.category, cData.count.min);
                        }
                    }
                }
            }
        }
        public static void ConsumeThings(ThingDef def, int count, Map map, Pawn receiver = null)
        {
            foreach (Thing t in AllConsumableThingForDef(def, map))
            {
                int spliteCount = t.stackCount <= count ? t.stackCount : count;
                Thing thing = t.SplitOff(spliteCount);
                count -= spliteCount;
                if (receiver == null)
                {
                    thing.Destroy();
                }
                else
                {
                    receiver.inventory.TryAddAndUnforbid(thing);
                }
                if (count <= 0)
                {
                    break;
                }
            };
        }
        public static string GetDialogText(string text, Thing interviewer, Thing interviewee,DialogTreeDef dialog,Quest quest)
        {
            TaggedString result = text.CanTranslate() ? text.Translate() : new TaggedString(text);
            List<NamedArgument> names = new List<NamedArgument>() { interviewer.Named("Interviewer"), interviewee.Named("Interviewee") }; 
            Find.FactionManager.AllFactions.ToList().ForEach(f =>
            {
                if (!names.Exists(n => n.label == f.def.defName)) 
                {
                    names.Add(f.Named(f.def.defName));
                }
            });
            if (dialog != null && dialog.extraThingRefers.Any())
            {
                foreach (string key in dialog.extraThingRefers) 
                {
                    if (GameTools.GetTarget(new Dictionary<string, TargetInfo>(), quest, key).Thing is Thing t) 
                    {
                        names.Add(t.Named(key));
                    }
                }
             
            }
            result = result.Formatted(names);
            if (interviewer is Pawn interviwerPawn)
            {
                result.AdjustedFor(interviwerPawn, "Interviewer", true).Resolve();
            }
            if (interviewee is Pawn interviweePawn)
            {
                result.AdjustedFor(interviweePawn, "Interviewee", true).Resolve();
            }
            return result.Resolve();
        }

        public static Faction GetFaction(FactionDef faction)
        {
            return faction.isPlayer ? Find.FactionManager.OfPlayer : Find.FactionManager.FirstFactionOfDef(faction);
        }
        public static Faction GetFaction(string faction, Map map,bool humanlike = true)
        {
            if (faction == null)
            {
                return null;
            }
            if (faction == "RandomHostile")
            {
                return Find.FactionManager.AllFactionsListForReading.ToList().FindAll(f => !f.IsPlayer && f.PlayerRelationKind == FactionRelationKind.Hostile && (!humanlike || f.def.humanlikeFaction)).RandomElement();
            }
            if (faction == "RandomAlly")
            {
                return Find.FactionManager.AllFactions.ToList().FindAll(f => !f.IsPlayer && f.PlayerRelationKind == FactionRelationKind.Ally && (!humanlike || f.def.humanlikeFaction)).RandomElement();
            }
            if (faction == "RandomNeutral")
            {
                return Find.FactionManager.AllFactions.ToList().FindAll(f => !f.IsPlayer && f.PlayerRelationKind == FactionRelationKind.Neutral && (!humanlike || f.def.humanlikeFaction)).RandomElement();
            }
            if (faction == "MapFaction" && map != null)
            { 
                return map.Parent.Faction;
            }
            return faction.NullOrEmpty() ? null : Find.FactionManager.FirstFactionOfDef(FactionDef.Named(faction));
        }

        public static bool isGeneratingMap = false;
    }
    public class ThingData : IExposable, ISaveable
    {
        public void OpenSelectDialog()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.ThingData.OpenSelectDialog()", this, arguments);
        }
        public ThingData() { }
        public ThingData(Thing thing, IntVec3 pos)
        {
            this.targetKeys = thing.Map?.GetComponent<MapComponent_CQFTargets>().GetKeys(thing) ?? new List<string>();
            this.def = thing.def;
            this.rotation = thing.Rotation;
            this.position = pos;
            this.count = thing.stackCount;
            this.stuff = thing.Stuff;
            this.style = thing.StyleDef;
            this.faction = thing.Faction?.def;
            if (thing.TryGetQuality(out QualityCategory q)) 
            {
                this.quality = q;
            }
            if (thing is Plant plant)
            {
                this.growth = plant.Growth;
            }
            if (thing.def.useHitPoints && thing.MaxHitPoints != thing.HitPoints)
            {
                this.hitPoint = thing.HitPoints;
            }
            if (thing.TryGetComp<CompPowerBattery>() is CompPowerBattery compB)
            {
                this.storedEnergy = compB.StoredEnergy;
            }
            if (thing.TryGetComp<CompRefuelable>() is CompRefuelable compR) 
            {
                this.storedEnergy = compR.Fuel;
            }
            if (thing.TryGetComp<CompColorable>() is CompColorable color)
            {
                this.color = color.Color;
            }
            if (thing is Building b && b.PaintColorDef != null) 
            {
                this.colorDef = b.PaintColorDef;
            }
        }
        public Thing Spawn(Map map, IntVec3 pos, Func<ThingDef,bool, ThingDef> getDef, ThingDef forcedStuff = null, Rot4? forcedRot = null)
        {
            ThingDef def = getDef(this.def,false);
            if (def == null)
            {
                Log.Error("Spawn thing data error:" + this.ToString());
                return null;
            }
            Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? forcedStuff ?? getDef(this.stuff,true) : null);
            thing.stackCount = this.count;
            thing.StyleDef = this.style;
            thing.Rotation = this.rotation;
            thing.stackCount = this.count;
            if (thing.TryGetComp<CompQuality>() is CompQuality compQ) 
            {
                compQ.SetQuality(this.quality,null);
            }
            if (thing.TryGetComp<CompPowerBattery>() is CompPowerBattery compB)
            {
                compB.AddEnergy(this.storedEnergy);
            }
            if (thing.TryGetComp<CompRefuelable>() is CompRefuelable compR)
            {
                compR.Refuel(this.storedEnergy);
            }
            if (thing.def.useHitPoints && this.hitPoint != -1)
            {
                thing.HitPoints = (int)(((float)this.hitPoint / (float)this.def.GetStatValueAbstract(StatDefOf.MaxHitPoints, this.stuff ?? GenStuff.DefaultStuffFor(this.def)) * thing.MaxHitPoints));
            }
            if (thing is Plant plant)
            {
                plant.Growth = this.growth;
            }
            if (this.faction != null && Find.FactionManager.FirstFactionOfDef(this.faction) is Faction faction)
            {
                thing.SetFaction(faction);
            }
            if (thing.TryGetComp<CompColorable>() is CompColorable color)
            {
                color.SetColor(this.color);
            }
            if (thing is Building b) 
            {
                b.ChangePaint(this.colorDef);
            }
            Thing spawned = GenSpawn.Spawn(thing, pos, map, forcedRot ?? this.rotation);
            if (spawned != null && !this.targetKeys.NullOrEmpty())
            {
                foreach (string key in this.targetKeys)
                {
                    map.GetComponent<MapComponent_CQFTargets>().TryRegister(key, spawned);
                }
            }
            return spawned;
        }
        public XElement SaveToXElement(string nodeName)
        {
            if (this.def == null) 
            {
                return null;
            }
            XElement result = new XElement(nodeName);
            result.Add(new XElement("def", this.def?.defName));
            if (!this.targetKeys.NullOrEmpty())
            {
                result.Add(CQFSerialization.SaveList(this.targetKeys, "targetKeys"));
            }
            if (this.stuff != null)
            {
                result.Add(new XElement("stuff", this.stuff?.defName));
            }
            if (this.style != null)
            {
                result.Add(new XElement("style", this.style?.defName));
            }
            if (this.rotation != Rot4.North)
            {
                result.Add(new XElement("rotation", this.rotation.AsInt));
            }
            if (this.count > 1)
            {
                result.Add(new XElement("count", this.count));
            }
            if (this.faction != null)
            {
                result.Add(new XElement("faction", this.faction.defName));
            }
            if (this.growth != 0f)
            {
                result.Add(new XElement("growth", this.growth));
            }
            if (this.storedEnergy != 0f)
            {
                result.Add(new XElement("storedEnergy", this.storedEnergy));
            }
            if (this.quality != QualityCategory.Normal)
            {
                result.Add(new XElement("quality", this.quality));
            }
            if (this.def.useHitPoints && this.hitPoint != -1)
            {
                result.Add(new XElement("hitPoint", this.hitPoint));
            }
            if (this.color != Color.white)
            {
                result.Add(new XElement("color", this.color.ToString()));
            }
            if (this.colorDef != null)
            {
                result.Add(new XElement("colorDef", this.colorDef.defName));
            }
            if (this.allRect != null && this.allRect.Any())
            {
                result.Add(CQFSerialization.SaveList(this.allRect, "allRect"));
            }
            if (this.allPositions != null && this.allPositions.Any())
            {
                result.Add(CQFSerialization.SaveList(this.allPositions, "allPositions"));
            }
            if (this.allRect.NullOrEmpty() && this.allPositions.NullOrEmpty())
            {
                XElement pos = new XElement("position", $"({this.position.x},{this.position.y},{this.position.z})");
                result.Add(pos);
            }
            return result;
        }
        public ThingData Copy()
        {
            ThingData result = new ThingData();
            result.def = this.def;
            result.stuff = this.stuff;
            result.style = this.style;
            result.faction = this.faction;
            result.rotation = this.rotation;
            result.position = this.position;
            result.count = this.count;
            result.growth = this.growth;
            result.quality = this.quality;
            result.hitPoint = this.hitPoint;
            result.storedEnergy = this.storedEnergy;
            result.color = this.color;
            result.colorDef = this.colorDef;
            result.allPositions = this.allPositions.ListFullCopy();
            result.allRect = this.allRect.ListFullCopy();
            result.targetKeys = this.targetKeys?.ToList() ?? new List<string>();
            return result;
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref this.def, "def");
            Scribe_Defs.Look(ref this.style, "style");
            Scribe_Defs.Look(ref this.stuff, "stuff");
            Scribe_Defs.Look(ref this.faction, "faction"); 
            Scribe_Defs.Look(ref this.colorDef, "colorDef");
            Scribe_Values.Look(ref this.hitPoint, "QE_ThingData_hitPoint");
            Scribe_Values.Look(ref this.growth, "QE_ThingData_growth");
            Scribe_Values.Look(ref this.rotation, "QE_ThingData_rotation"); 
            Scribe_Values.Look(ref this.color, "color");
            Scribe_Values.Look(ref this.count, "QE_ThingData_count");
            Scribe_Values.Look(ref this.storedEnergy, "QE_ThingData_storedEnergy");
            Scribe_Values.Look(ref this.quality, "quality");
            Scribe_Collections.Look(ref this.allPositions, "positions", LookMode.Value);
            Scribe_Collections.Look(ref this.allRect, "allRect", LookMode.Value);
            Scribe_Collections.Look(ref this.targetKeys, "targetKeys", LookMode.Value);
        }

        public bool Equals_Def(ThingData data)
        {
            return data.targetKeys.NullOrEmpty() && this.targetKeys.NullOrEmpty() && data.def == this.def && data.stuff == this.stuff && data.style == this.style && data.faction == this.faction && data.rotation == this.rotation && data.count == this.count
               && data.hitPoint == this.hitPoint && this.growth == data.growth && this.storedEnergy == data.storedEnergy && this.color == data.color && this.colorDef == data.colorDef;
        }

        public ThingDef def = null;
        public ThingDef stuff = null;
        public ThingStyleDef style = null;
        public FactionDef faction = null;
        public Rot4 rotation = Rot4.North;
        public IntVec3 position = IntVec3.Zero;
        public Color color = Color.white;
        public ColorDef colorDef;
        public List<IntVec3> allPositions = new List<IntVec3>();
        public List<CellRect> allRect = new List<CellRect>();
        public QualityCategory quality = QualityCategory.Normal;
        public int count = 1;
        public float growth = 0f;
        public int hitPoint = -1;
        public float storedEnergy;
        public List<string> targetKeys = new List<string>();
    }
    public class RuleData
    {
        public RulePack GetRulePack()
        {
            RulePack result = new RulePack();
            if (this.rulesFiles != null)
            {
                typeof(RulePack).GetField("rulesFiles", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(result, this.rulesFiles);
            }
            typeof(RulePack).GetField("rulesStrings", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(result, this.stringRules);
            return result;
        }
        public RulePackDef GetRulePackDef()
        {
            RulePackDef result = new RulePackDef();
            result.defName = this.ruleName;
            RulePack pack = new RulePack();
            if (this.rulesFiles != null)
            {
                typeof(RulePack).GetField("rulesFiles", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pack, this.rulesFiles);
            }
            typeof(RulePack).GetField("rulesStrings", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pack, this.stringRules);
            typeof(RulePackDef).GetField("rulePack", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(result, pack);
            return result;
        }
        public XElement GetXmlNode(string nodeName)
        {
            XElement result = new XElement(nodeName);
            XElement ruleStrings = new XElement("rulesStrings");
            foreach (string rule in this.stringRules)
            {
                XElement li = new XElement("li", @rule);
                ruleStrings.Add(li);
            }
            result.Add(ruleStrings);
            return result;
        }

        public string ruleName = "";
        public List<string> rulesFiles;
        public List<string> stringRules = new List<string>() { "" };
    }
}
