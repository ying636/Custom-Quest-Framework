using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace QuestEditor_Library
{
    public class MainMapWorldComponent : WorldComponent
    {
        public MainMapWorldComponent(World world) : base(world)
        {
        }

        public static MainMapWorldComponent Component
        {
            get
            {
                return Find.World?.GetComponent<MainMapWorldComponent>();
            }
        }

        public void RegisterMainSite(MainSite site)
        {
            if (site == null)
            {
                return;
            }
            if (this.mainSites == null)
            {
                this.mainSites = new List<MainSite>();
            }
            if (!this.mainSites.Contains(site))
            {
                this.mainSites.Add(site);
            }
            this.AddMainSiteToIndex(site);
        }

        public void UnregisterMainSite(MainSite site)
        {
            if (site == null)
            {
                return;
            }
            this.mainSites?.Remove(site);
            this.RebuildMainSiteIndex();
        }

        public bool DestroyMainSite(MainSite site, bool destroyMap = true, bool notifyPlayer = true)
        {
            if (site == null || site.Destroyed)
            {
                return false;
            }
            if (destroyMap && site.HasMap)
            {
                Current.Game.DeinitAndRemoveMap(site.Map, notifyPlayer);
            }
            if (!site.Destroyed)
            {
                site.Destroy();
            }
            this.UnregisterMainSite(site);
            return true;
        }

        public bool TryDestroyMainSiteByKey(string key, Quest quest, Dictionary<string, TargetInfo> targets = null, bool destroyMap = true, bool notifyPlayer = true)
        {
            if (this.TryGetMainSiteByKey(key, quest, targets, out MainSite site))
            {
                return this.DestroyMainSite(site, destroyMap, notifyPlayer);
            }
            return false;
        }

        public bool TryGetMainSiteByKey(string key, Quest quest, Dictionary<string, TargetInfo> targets, out MainSite site)
        {
            site = null;
            if (key.NullOrEmpty())
            {
                return false;
            }
            TargetInfo target = GameTools.GetTarget(targets, quest, key);
            if (target != null)
            {
                if (target.Thing?.Map?.Parent is MainSite targetSite)
                {
                    site = targetSite;
                    return true;
                }
                if (target.Map?.Parent is MainSite mapSite)
                {
                    site = mapSite;
                    return true;
                }
            }
            site = this.GetAllMainSites().FirstOrDefault(s => s.MainMapKey == key || s.ID.ToString() == key);
            return site != null;
        }

        public List<MainSite> GetMainSites(MainMapDef mainMapDef)
        {
            if (mainMapDef == null)
            {
                return new List<MainSite>();
            }
            if (this.mainSitesByDef == null)
            {
                this.RebuildMainSiteIndex();
            }
            if (this.mainSitesByDef.TryGetValue(mainMapDef, out List<MainSite> sites))
            {
                return sites.Where(site => site != null && !site.Destroyed).ToList();
            }
            return new List<MainSite>();
        }

        public List<MainSite> GetAllMainSites()
        {
            if (this.mainSites == null)
            {
                return new List<MainSite>();
            }
            return this.mainSites.Where(site => site != null && !site.Destroyed).ToList();
        }

        public void RebuildMainSiteIndex()
        {
            this.mainSitesByDef = new Dictionary<MainMapDef, List<MainSite>>();
            if (this.mainSites == null)
            {
                return;
            }
            this.mainSites.RemoveAll(site => site == null || site.Destroyed);
            foreach (MainSite site in this.mainSites)
            {
                this.AddMainSiteToIndex(site);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.mainSites, "mainSites", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.RebuildMainSiteIndex();
            }
        }

        private void AddMainSiteToIndex(MainSite site)
        {
            if (site == null || site.mainMapDef == null)
            {
                return;
            }
            if (this.mainSitesByDef == null)
            {
                this.mainSitesByDef = new Dictionary<MainMapDef, List<MainSite>>();
            }
            if (!this.mainSitesByDef.TryGetValue(site.mainMapDef, out List<MainSite> sites))
            {
                sites = new List<MainSite>();
                this.mainSitesByDef.Add(site.mainMapDef, sites);
            }
            if (!sites.Contains(site))
            {
                sites.Add(site);
            }
        }

        public List<MainSite> mainSites = new List<MainSite>();
        public Dictionary<MainMapDef, List<MainSite>> mainSitesByDef = new Dictionary<MainMapDef, List<MainSite>>();
    }
}
