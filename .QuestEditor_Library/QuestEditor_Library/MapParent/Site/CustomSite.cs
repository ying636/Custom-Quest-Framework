using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst.Intrinsics;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CustomSite : Site
    {
        public override Material Material
        {
            get
            {
                if (!this.siteIconPath.NullOrEmpty())
                {
                    if (this.material == null)
                    {
                        this.material = MaterialPool.MatFrom(this.siteIconPath,
                            ShaderDatabase.WorldOverlayTransparentLit,UnityEngine.Color.white
                            , WorldMaterials.WorldObjectRenderQueue);
                    }
                    return this.material;
                }
                return base.Material;
            }
        }
        public override Texture2D ExpandingIcon
        {
            get
            {
                if (!this.expandingIconPath.NullOrEmpty())
                {
                    if (this.expandingIcon == null) 
                    {
                        this.expandingIcon = ContentFinder<Texture2D>.Get(this.expandingIconPath);
                    }
                    return this.expandingIcon;
                }
                return base.ExpandingIcon;
            }
        }
        public Dictionary<CustomMapDataDef, int> GenerationCount
        {
            get 
            {
                if (this.generationCount == null) 
                {
                    this.generationCount = new Dictionary<CustomMapDataDef, int>();
                }
                return this.generationCount;
            }
        }
        public override string GetDescription()
        {
            return this.customDescription ?? base.GetDescription();
        }
        public override void Notify_MyMapRemoved(Map map)
        {
            map.GetComponent<MapComponent_CustomMapData>().subMaps.ForEach(m => m.Destroy());
        }
        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            bool result = base.ShouldRemoveMapNow(out alsoRemoveWorldObject);
            return result && Find.TickManager.TicksGame - this.creationGameTicks > 5 
                && this.ShouldRemoveMapBySubmap(ref alsoRemoveWorldObject) && (!this.disdestroyBecauseOfNoColonist || this.forceRemoveWorldObjectWhenMapRemoved);
        }
        private bool ShouldRemoveMapBySubmap(ref bool alsoRemoveWorldObject)
        {
            foreach (MapParent_Custom parent in allSubMaps)
            {
                if ((parent.Map?.mapPawns?.AnyPawnBlockingMapRemoval) ?? false
                    || ((parent.exit?.thereIsPawnIsEntering) ?? false) || 
                    ((parent.entrance?.thereIsPawnIsEntering) ?? false))
                {
                    alsoRemoveWorldObject = false;
                    return false;
                }
            }
            if (defeated == null) 
            {
                defeated = typeof(Site)
                .GetField("allEnemiesDefeatedSignalSent", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            alsoRemoveWorldObject = !this.reenterable || 
                (this.beUnreenterableWhenAllEnemiesDefeated && 
                (bool)defeated.GetValue(this));
            return true;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (DebugSettings.godMode) 
            {
                yield return new Command_Action()
                {
                    defaultLabel = "Log data",
                    action = () => Log.Message($"Quest:{this.quest}," +
                    $"Tile:{this.Tile},Part:{this.parts.Count},faction:{this.Faction?.Name}") };
            }
            yield break;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.siteIconPath, "siteIconPath");
            Scribe_Values.Look(ref this.expandingIconPath, "expandingIconPath");

            Scribe_Values.Look(ref this.reenterable, "reenterable");
            Scribe_Values.Look(ref this.beUnreenterableWhenAllEnemiesDefeated, "beUnreenterableWhenAllEnemiesDefeated");

            Scribe_Values.Look(ref this.disdestroyBecauseOfNoColonist, "disdestroyBecauseOfNoColonist");
            Scribe_Values.Look(ref this.customDescription, "customDescription");
            Scribe_References.Look(ref this.quest, "quest");
            Scribe_Defs.Look(ref this.mapDef, "mapDef");
            Scribe_Collections.Look(ref this.allSubMaps, "allSubMaps",LookMode.Reference);
            Scribe_Collections.Look(ref this.generationCount, "generationCount", LookMode.Def,LookMode.Value);
            
            Scribe_Values.Look(ref this.replaceMapGeneration, "replaceMapGeneration");
            Scribe_Values.Look(ref this.dev, "dev");
        }

        public string siteIconPath;
        public string expandingIconPath;

        Texture2D expandingIcon;
        Material material;

        public bool reenterable;
        public bool beUnreenterableWhenAllEnemiesDefeated;
        public bool disdestroyBecauseOfNoColonist = false;
        public string customDescription;
        public CustomMapDataDef mapDef;
        public List<MapParent_Custom> allSubMaps = new List<MapParent_Custom>();
        private Dictionary<CustomMapDataDef, int> generationCount = new Dictionary<CustomMapDataDef, int>();
        public Quest quest;
 
        public bool replaceMapGeneration = false;
        public bool dev = false;

        FieldInfo defeated;
    }
}
