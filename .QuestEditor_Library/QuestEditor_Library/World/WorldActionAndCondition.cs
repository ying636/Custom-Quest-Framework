using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine.Tilemaps;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class WorldTarget
    {
        public static implicit operator WorldTarget(Caravan caravan)
        {
            return new WorldTarget() { worldObject = caravan };
        }

        public static implicit operator WorldTarget(PlanetTile tile)
        {
            return new WorldTarget() { tile = tile };
        }

        public int Tile => this.tile ?? this.worldObject.Tile;
        public Caravan Caravan => this.worldObject as Caravan;

        public TargetInfo target;
        public WorldObject worldObject; 
        private int? tile;
    }
    public class WorldAction : ISaveable, IDrawable, IExposable
    {
        public virtual void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldAction.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", this.GetType().FullName);
            return result;
        }

        public virtual void Work(WorldTarget target) 
        {
        }

        public virtual void ExposeData()
        {

        }
    }
    public class WorldAction_Chance : WorldAction 
    {
        public override void Work(WorldTarget target)
        {
            this.actions.RandomElementByWeight(a => a.Value).Key.Work(target);
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldAction_Chance.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            XElement actions = new XElement("actions");
            foreach (KeyValuePair<WorldAction, float> action in this.actions)
            {
                XElement li = new XElement("li");
                li.Add(action.Key.SaveToXElement("key"));
                li.Add(new XElement("value", action.Value));
                actions.Add(li);
            }
            result.Add(actions);
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.actions, "actions",LookMode.Deep,LookMode.Value);
        }

        public Dictionary<WorldAction,float> actions = new Dictionary<WorldAction, float>();
    }
    public class WorldAction_GenerateMapAndEnter : WorldAction
    {
        public override void Work(WorldTarget target)
        {
            WorldObject wo = WorldObjectMaker.MakeWorldObject(this.worldObject);
            wo.Tile = target.Tile;
            Find.WorldObjects.Add(wo);
            if (target.Caravan is Caravan c && GetOrGenerateMapUtility.GetOrGenerateMap(wo.Tile, wo.def) is Map m) 
            {
                CaravanEnterMapUtility.Enter(c,m,CaravanEnterMode.Edge);
            }
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldAction_GenerateMapAndEnter.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("worldObject", this.worldObject?.defName));
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.worldObject,"worldObject");
        }

        public WorldObjectDef worldObject;
    }
    public class WorldAction_GenerateSiteAndEnter : WorldAction
    {
        public override void Work(WorldTarget target)
        {
            Site wo = SiteMaker.MakeSite(this.part,target.Tile,GameTools.GetFaction(this.faction,null));
            if (!this.sentDefeatLetter)
            {
                typeof(Site).GetField("allEnemiesDefeatedSignalSent", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(wo,true);
            }
            if (!this.setCaravanTileAsTarget)
            {
                if (TileFinder.TryFindPassableTileWithTraversalDistance(target.Tile, 1, 2, out PlanetTile tile))
                {
                    wo.Tile = tile;
                }
            }
            Find.WorldObjects.Add(wo);
            if (target.Caravan is Caravan c && GetOrGenerateMapUtility.GetOrGenerateMap(wo.Tile,wo.def) is Map m)
            {
                CaravanEnterMapUtility.Enter(c, m, CaravanEnterMode.Edge);
            }
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldAction_GenerateSiteAndEnter.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("faction", this.faction));
            result.Add(new XElement("setCaravanTileAsTarget", this.setCaravanTileAsTarget));
            result.Add(new XElement("sentDefeatLetter", this.sentDefeatLetter));
            result.Add(new XElement("part", this.part?.defName));
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.faction, "faction");
            Scribe_Values.Look(ref this.setCaravanTileAsTarget, "setCaravanTileAsTarget");
            Scribe_Values.Look(ref this.sentDefeatLetter, "sentDefeatLetter");
            Scribe_Defs.Look(ref this.part, "part");
        }

        public string faction;
        public bool sentDefeatLetter=true;
        public bool setCaravanTileAsTarget = true;
        public SitePartDef part;
    }
    public class WorldAction_GenerateCustomMapAndEnter : WorldAction
    {
        public override void Work(WorldTarget target)
        {
            Dictionary<CustomMapDataDef, float> datas = new Dictionary<CustomMapDataDef, float>();
            this.customMapDataTags.ForEach(t => DefDatabase<CustomMapDataDef>.AllDefsListForReading.ForEach(mapData =>
             {
                 if (mapData.tags.Contains(t.tag))
                 {
                     datas.SetOrAdd(mapData, t.weight);
                 }
             }));
            this.customMapDatas.ForEach(data => datas.SetOrAdd(data.data, data.weight)); 
            CustomSite site = QuestNode_Root_CustomMap
                .GenerateCustomSite(Gen.YieldSingle<SitePartDefWithParams>(
                    new SitePartDefWithParams(DefDatabase<SitePartDef>.GetNamed("QE_CustomSite"), new SitePartParams()))
                , target.Tile,GameTools.GetFaction(faction,null), false, null);
            site.siteIconPath = this.siteIconPath;
            site.expandingIconPath = this.expandingIconPath;
            site.mapDef = datas.RandomElementByWeight(d => d.Value).Key;
            site.customLabel =  site.mapDef.label;
            site.customDescription =site.mapDef.description;
            site.replaceMapGeneration = this.replaceMapGeneration;
            if (!this.sentDefeatLetter)
            {
                typeof(Site).GetField("allEnemiesDefeatedSignalSent", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(site, true);
            }
            if (!this.setCaravanTileAsTarget)
            {
                if (TileFinder.TryFindPassableTileWithTraversalDistance(target.Tile, 1, 2, out PlanetTile tile))
                {
                    site.Tile = tile;
                }
            }
            Find.WorldObjects.Add(site);
            if (target.Caravan is Caravan c && GetOrGenerateMapUtility.
                    GetOrGenerateMap(site.Tile,this.replaceMapGeneration ?
                        site.mapDef.size : Find.World.info.initialMapSize, site.def) is Map m)
            {
                site.mapDef.EnterCaravan(c, m);
            }
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldAction_GenerateCustomMapAndEnter.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("siteIconPath", this.siteIconPath));
            result.Add(new XElement("expandingIconPath", this.expandingIconPath));
            result.Add(new XElement("replaceMapGeneration", this.replaceMapGeneration));
            result.Add(CQFSerialization.SaveList_Saveable(this.customMapDataTags, "customMapDataTags"));
            result.Add(CQFSerialization.SaveList_Saveable(this.customMapDatas, "customMapDatas"));
            result.Add(new XElement("faction", this.faction));
            result.Add(new XElement("setCaravanTileAsTarget", this.setCaravanTileAsTarget));
            result.Add(new XElement("sentDefeatLetter", this.sentDefeatLetter));
            result.Add(new XElement("part", this.part?.defName));
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.faction, "faction");
            Scribe_Values.Look(ref this.setCaravanTileAsTarget, "setCaravanTileAsTarget");
            Scribe_Values.Look(ref this.sentDefeatLetter, "sentDefeatLetter");
            Scribe_Defs.Look(ref this.part, "part");
            Scribe_Values.Look(ref this.siteIconPath, "siteIconPath");
            Scribe_Values.Look(ref this.expandingIconPath, "expandingIconPath");
            Scribe_Values.Look(ref this.replaceMapGeneration, "replaceMapGeneration");
            Scribe_Collections.Look(ref this.customMapDataTags, "customMapDataTags", LookMode.Deep);
            Scribe_Collections.Look(ref this.customMapDatas, "customMapDatas", LookMode.Deep);
        }

        public string siteIconPath;
        public string expandingIconPath;
        public bool replaceMapGeneration = false;
        public List<CustomMapDataTagWithWeight> customMapDataTags = new List<CustomMapDataTagWithWeight>();
        public List<CustomMapDataWithWeight> customMapDatas = new List<CustomMapDataWithWeight>();

        public string faction;
        public bool sentDefeatLetter = true;
        public bool setCaravanTileAsTarget = true;
        public SitePartDef part;
    }
    public class WorldAction_GenerateQuest : WorldAction
    {
        public override void Work(WorldTarget target)
        {
            base.Work(target);
            Slate slate = new Slate();
            if (this.setCaravanTileAsTarget) 
            {
                if (TileFinder.TryFindPassableTileWithTraversalDistance(target.Tile,0,2,out PlanetTile tile))
                {
                    slate.Set("CQFMapTile", tile);
                }
            }
            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(this.quest,slate);
            if (!quest.hidden && this.quest.sendAvailableLetter)
            {
                QuestUtility.SendLetterQuestAvailable(quest);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.quest,"quest");
            Scribe_Values.Look(ref this.setCaravanTileAsTarget,"set");
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldAction_GenerateQuest.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("quest", this.quest?.defName));
            result.Add(new XElement("set", this.setCaravanTileAsTarget));
            return result;
        }

        public QuestScriptDef quest;
        public bool setCaravanTileAsTarget;
    }

    public class WorldCondition : ISaveable, IDrawable, IExposable
    {
        public virtual void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldCondition.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", this.GetType().FullName);
            return result;
        }

        public virtual bool Satisfied(WorldTarget target)
        {
            return true;
        }

        public virtual void ExposeData()
        {
       
        }
    }
    public class WorldCondition_And : WorldCondition
    {
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldCondition_And.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFSerialization.SaveList_Saveable(this.conditions, "condition"));
            return result;
        }

        public override bool Satisfied(WorldTarget target)
        {
            return this.conditions.NullOrEmpty() || !this.conditions.Exists(condition => !condition.Satisfied(target));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.conditions, "conditions", LookMode.Deep);
        }

        public List<WorldCondition> conditions = new List<WorldCondition>();
    }
    public class WorldCondition_Or : WorldCondition
    {
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldCondition_Or.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFSerialization.SaveList_Saveable(this.conditions, "condition"));
            return result;
        }

        public override bool Satisfied(WorldTarget target)
        {
            return !this.conditions.NullOrEmpty() && this.conditions.Exists(condition => condition.Satisfied(target));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.conditions, "conditions", LookMode.Deep);
        }

        public List<WorldCondition> conditions = new List<WorldCondition>();
    }
    public class WorldCondition_Reversal : WorldCondition
    {
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldCondition_Reversal.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.condition != null)
            {
                result.Add(this.condition.SaveToXElement("condition"));
            }
            return result;
        }

        public override bool Satisfied(WorldTarget target)
        {
            return this.condition == null || !this.condition.Satisfied(target);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref this.condition, "condition");
        }

        public WorldCondition condition;
    }
    public class WorldCondition_WorldObject : WorldCondition 
    {
        public override bool Satisfied(WorldTarget target)
        {
            return Find.World.worldObjects.WorldObjectAt(target.Tile,this.objectDef) is WorldObject wo && (this.faction == null 
                || wo.Faction?.def == this.faction) && (!this.nonPlayer ||wo.Faction == null || !wo.Faction.IsPlayer)
                && (!this.nonHostile || wo.Faction == null 
                || !wo.Faction.def.isPlayer || wo.Faction.HostileTo(Find.FactionManager.OfPlayer));
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldCondition_WorldObject.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("nonHostile", this.nonHostile));
            result.Add(new XElement("nonPlayer", this.nonPlayer));
            result.Add(new XElement("faction", this.faction?.defName));
            result.Add(new XElement("objectDef", this.objectDef?.defName));
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.nonHostile, "nonHostile");
            Scribe_Values.Look(ref this.nonPlayer, "nonPlayer");
            Scribe_Defs.Look(ref this.faction, "faction");
            Scribe_Defs.Look(ref this.objectDef, "objectDef");
        }

        public bool nonHostile;
        public bool nonPlayer = true;
        public FactionDef faction;
        public WorldObjectDef objectDef;
    }
    public class WorldCondition_Landmark : WorldCondition
    {
        public override bool Satisfied(WorldTarget target)
        {
            return this.landmark == null || Find.World.grid[target.Tile].Landmark?.def == this.landmark;
        }

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldCondition_Landmark.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("landmark", this.landmark?.defName));
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.landmark, "landmark");
        }

        public LandmarkDef landmark;
    }
    public class WorldCondition_Biome : WorldCondition
    {
        public override bool Satisfied(WorldTarget target)
        {
            return this.biome == null || Find.World.grid[target.Tile].Biomes.Contains(this.biome);
        }

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldCondition_Biome.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("biome", this.biome?.defName));
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.biome, "biome");
        }

        public BiomeDef biome;
    }
    public class WorldCondition_TileMutator : WorldCondition
    {
        public override bool Satisfied(WorldTarget target)
        {
            return this.mutator == null || Find.World.grid[target.Tile].Mutators.Contains(this.mutator);
        }

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldCondition_TileMutator.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("mutator", this.mutator?.defName));
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.mutator, "mutator");
        }

        public TileMutatorDef mutator;
    }
    public class WorldCondition_TotalSkill : WorldCondition
    {
        public override bool Satisfied(WorldTarget target)
        {
            int result = 0;
            if (target.Caravan == null) 
            {
                return false;
            }
            foreach (var pawn in target.Caravan.pawns)
            {
                if (pawn.skills != null && pawn.skills.GetSkill(this.skill) is SkillRecord record) 
                {
                    result += record.Level;
                }
            }

            return result >= this.vaule;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.WorldCondition_TotalSkill.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("skill", this.skill?.defName));
            result.Add(new XElement("vaule", this.vaule));
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.vaule, "vaule");
            Scribe_Defs.Look(ref this.skill, "skill");
        }

        public SkillDef skill;
        public int vaule;
        public string buffer;
    }
}
