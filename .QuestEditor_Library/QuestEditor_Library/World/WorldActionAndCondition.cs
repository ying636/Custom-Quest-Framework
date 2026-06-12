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
using UnityEngine.Tilemaps;
using Verse;

namespace QuestEditor_Library
{
    public class WorldTarget
    {
        public static implicit operator WorldTarget(Caravan caravan)
        {
            return new WorldTarget() { caravan = caravan };
        }
        public int Tile => this.tile ?? this.caravan.Tile;
        public TargetInfo target;
        public Caravan caravan;
        private int? tile;
    }
    public class WorldAction : IExposable
    {
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
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.actions, "actions",LookMode.Def,LookMode.Value);
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
            if (target.caravan is Caravan c && GetOrGenerateMapUtility.GetOrGenerateMap(wo.Tile, wo.def) is Map m) 
            {
                CaravanEnterMapUtility.Enter(c,m,CaravanEnterMode.Edge);
            }
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
            if (target.caravan is Caravan c && GetOrGenerateMapUtility.GetOrGenerateMap(wo.Tile,wo.def) is Map m)
            {
                CaravanEnterMapUtility.Enter(c, m, CaravanEnterMode.Edge);
            }
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
            if (target.caravan is Caravan c && GetOrGenerateMapUtility.
                    GetOrGenerateMap(site.Tile,this.replaceMapGeneration ?
                        site.mapDef.size : Find.World.info.initialMapSize, site.def) is Map m)
            {
                site.mapDef.EnterCaravan(c, m);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.faction, "faction");
            Scribe_Values.Look(ref this.setCaravanTileAsTarget, "setCaravanTileAsTarget");
            Scribe_Values.Look(ref this.sentDefeatLetter, "sentDefeatLetter");
            Scribe_Defs.Look(ref this.part, "part");
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

        public QuestScriptDef quest;
        public bool setCaravanTileAsTarget;
    }

    public class WorldCondition : IExposable
    {
        public virtual bool Satisfied(WorldTarget target)
        {
            return true;
        }
        public virtual void ExposeData()
        {
       
        }
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
    public class WorldCondition_TotalSkill : WorldCondition
    {
        public override bool Satisfied(WorldTarget target)
        {
            int result = 0;
            if (target.caravan == null) 
            {
                return false;
            }
            foreach (var pawn in target.caravan.pawns)
            {
                if (pawn.skills != null && pawn.skills.GetSkill(this.skill) is SkillRecord record) 
                {
                    result += record.Level;
                }
            }

            return result >= this.vaule;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.vaule, "vaule");
            Scribe_Defs.Look(ref this.skill, "skill");
        }

        public SkillDef skill;
        public int vaule;
    }
}
