using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.Grammar;
using Verse;

namespace QuestEditor_Library
{
    public class QuestNode_GenerateCustomWorldObject : QuestNode
    {
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named(this.faction));
            PlanetTile tile = default;
            PlanetLayer layer = null;
            if (this.planetLayer != null && this.planetLayer.GetValue(slate) is PlanetLayerDef layerDef) 
            {
                layer = Find.WorldGrid.FirstLayerOfDef(layerDef);
            }
            if (slate.Get<PlanetTile>("CQFMapTile") != default(PlanetTile) 
                || (this.tile != null && !this.tile.ToString().NullOrEmpty()
                && this.tile.TryGetValue(slate, out tile))
                || TileFinder.TryFindNewSiteTile(out tile, Find.AnyPlayerHomeMap.Tile
                , this.distance.min, this.distance.max,false,null,0.5f,true,TileFinderMode.Random,false,
                layer != null && layer.Def.isSpace,layer,
                 (x) => this.blacklist == null ||
                !this.blacklist.Contains(Find.World.grid[x].PrimaryBiome)))
            {
                if (slate.Get<PlanetTile>("CQFMapTile") is PlanetTile tilevar)
                {
                    tile = tilevar;
                }
                if (tile == default)
                {
                    return;
                }
                WorldObject wo = WorldObjectMaker.MakeWorldObject(this.worldObject.GetValue(slate));
                wo.Tile = tile;
                quest.SpawnWorldObject(wo, null, null);
                slate.Set(this.storeAs.GetValue(slate), wo);
            }
            else
            {
                quest.End(QuestEndOutcome.Fail);
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
 

        [NoTranslate]
        public SlateRef<string> storeAs;
        public SlateRef<PlanetLayerDef> planetLayer;
        public SlateRef<WorldObjectDef> worldObject;
        public string faction;
        public IntRange distance = new IntRange(10, 20);
        public List<BiomeDef> blacklist = new List<BiomeDef>();
        public SlateRef<PlanetTile> tile;
    }
}
