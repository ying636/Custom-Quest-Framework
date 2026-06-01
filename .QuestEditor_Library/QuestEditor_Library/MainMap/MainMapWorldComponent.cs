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
