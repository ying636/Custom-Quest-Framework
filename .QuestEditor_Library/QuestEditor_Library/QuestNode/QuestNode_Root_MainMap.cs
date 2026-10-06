using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace QuestEditor_Library
{
    public class QuestNode_Root_MainMap : QuestNode, IDrawable
    {
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            MainMapDef def = this.mainMapDef.GetValue(slate);
            if (def == null)
            {
                quest.End(QuestEndOutcome.Fail);
                return;
            }
            MainSite site = this.GetOrCreateMainSite(def, quest, slate);
            if (site == null)
            {
                quest.End(QuestEndOutcome.Fail);
                return;
            }
            string storeAs = this.storeAs.GetValue(slate);
            if (!storeAs.NullOrEmpty())
            {
                slate.Set<Site>(storeAs, site);
            }
        }

        protected override bool TestRunInt(Slate slate)
        {
            return this.mainMapDef.GetValue(slate) != null;
        }

        public void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestNode_Root_MainMap.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        private MainSite GetOrCreateMainSite(MainMapDef def, Quest quest, Slate slate)
        {
            if (this.reuseExistingSite)
            {
                MainSite existing = MainMapWorldComponent.Component?.GetMainSites(def).FirstOrDefault();
                if (existing != null)
                {
                    return existing;
                }
            }
            PlanetTile tile = this.GetTile(slate);
            if (tile == default)
            {
                return null;
            }
            Faction faction = GameTools.GetFaction(this.faction.GetValue(slate), null);
            if (this.overrideFaction != null && this.overrideFaction.GetValue(slate) is Faction f)
            {
                faction = f;
            }
            MainSite site = QuestNode_Root_MainMap.GenerateMainSite(Gen.YieldSingle<SitePartDefWithParams>(new SitePartDefWithParams(QEDefOf.CQF_MainSitePart, new SitePartParams())), tile, faction, false, null);
            site.mainMapDef = def;
            site.siteIconPath = this.siteIconPath;
            site.reenterable = true;
            site.beUnreenterableWhenAllEnemiesDefeated = this.beUnreenterableWhenAllEnemiesDefeated;
            site.expandingIconPath = this.expandingIconPath;
            site.quest = quest;
            site.disdestroyBecauseOfNoColonist = this.disdestroyBecauseOfNoColonist;
            site.customLabel = def.label.NullOrEmpty() ? def.defName : def.label;
            site.customDescription = def.description;
            site.replaceMapGeneration = this.replaceMapGeneration;
            MainMapWorldComponent.Component?.RegisterMainSite(site);
            quest.SpawnWorldObject(site, null, null);
            return site;
        }

        private PlanetTile GetTile(Slate slate)
        {
            PlanetTile root = Find.AnyPlayerHomeMap.Tile;
            PlanetTile tile = default;
            PlanetLayer layer = null;
            if (this.planetLayer != null && this.planetLayer.GetValue(slate) is PlanetLayerDef layerDef)
            {
                layer = Find.WorldGrid.FirstLayerOfDef(layerDef);
            }
            if (slate.Get<PlanetTile>("CQFMapTile") != default(PlanetTile))
            {
                return slate.Get<PlanetTile>("CQFMapTile");
            }
            if (this.tile != null && !this.tile.ToString().NullOrEmpty() && this.tile.TryGetValue(slate, out tile))
            {
                return tile;
            }
            if (TileFinder.TryFindNewSiteTile(out tile, root, this.distance.min, this.distance.max, false, null, 0.5f, true, TileFinderMode.Random, false, layer != null && layer.Def.isSpace, layer, x => this.blacklist == null || !this.blacklist.Contains(Find.World.grid[x].PrimaryBiome)))
            {
                return tile;
            }
            return default;
        }

        public static MainSite GenerateMainSite(IEnumerable<SitePartDefWithParams> sitePartsParams, PlanetTile tile, Faction faction, bool hiddenSitePartsPossible = false, RulePack singleSitePartRules = null)
        {
            bool flag = false;
            using (IEnumerator<SitePartDefWithParams> enumerator = sitePartsParams.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.def.defaultHidden)
                    {
                        flag = true;
                        break;
                    }
                }
            }
            if (flag || hiddenSitePartsPossible)
            {
                SitePartParams parms = SitePartDefOf.PossibleUnknownThreatMarker.Worker.GenerateDefaultParams(0f, tile, faction);
                SitePartDefWithParams val = new SitePartDefWithParams(SitePartDefOf.PossibleUnknownThreatMarker, parms);
                sitePartsParams = sitePartsParams.Concat(Gen.YieldSingle<SitePartDefWithParams>(val));
            }
            MainSite site = QuestNode_Root_MainMap.MakeMainSite(sitePartsParams, tile, faction, true);
            QuestNode_Root_MainMap.AddSiteRules(site, singleSitePartRules);
            return site;
        }

        public static MainSite MakeMainSite(IEnumerable<SitePartDefWithParams> siteParts, PlanetTile tile, Faction faction, bool ifHostileThenMustRemainHostile = true)
        {
            MainSite site = (MainSite)WorldObjectMaker.MakeWorldObject(QEDefOf.CQF_MainSite);
            site.Tile = tile;
            site.SetFaction(faction);
            if (ifHostileThenMustRemainHostile && faction != null && faction.HostileTo(Faction.OfPlayer))
            {
                site.factionMustRemainHostile = true;
            }
            if (siteParts != null)
            {
                foreach (SitePartDefWithParams sitePartDefWithParams in siteParts)
                {
                    SitePart part = new SitePart(site, sitePartDefWithParams.def, sitePartDefWithParams.parms);
                    site.AddPart(part);
                }
            }
            site.desiredThreatPoints = site.ActualThreatPoints;
            return site;
        }

        private static void AddSiteRules(MainSite site, RulePack singleSitePartRules)
        {
            List<Rule> list = new List<Rule>();
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            List<string> list2 = new List<string>();
            int num = 0;
            for (int i = 0; i < site.parts.Count; i++)
            {
                List<Rule> list3 = new List<Rule>();
                Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
                site.parts[i].def.Worker.Notify_GeneratedByQuestGen(site.parts[i], QuestGen.slate, list3, dictionary2);
                if (!site.parts[i].hidden)
                {
                    if (singleSitePartRules != null)
                    {
                        List<Rule> list4 = new List<Rule>();
                        list4.AddRange(list3);
                        list4.AddRange(singleSitePartRules.Rules);
                        string text = QuestGenUtility.ResolveLocalText(list4, dictionary2, "root", false);
                        list.Add(new Rule_String("sitePart" + num + "_description", text));
                        if (!text.NullOrEmpty())
                        {
                            list2.Add(text);
                        }
                    }
                    for (int j = 0; j < list3.Count; j++)
                    {
                        Rule rule = list3[j].DeepCopy();
                        Rule_String rule_String = rule as Rule_String;
                        if (rule_String != null && num != 0)
                        {
                            rule_String.keyword = "sitePart" + num + "_" + rule_String.keyword;
                        }
                        list.Add(rule);
                    }
                    foreach (KeyValuePair<string, string> keyValuePair in dictionary2)
                    {
                        string text2 = keyValuePair.Key;
                        if (num != 0)
                        {
                            text2 = "sitePart" + num + "_" + text2;
                        }
                        if (!dictionary.ContainsKey(text2))
                        {
                            dictionary.Add(text2, keyValuePair.Value);
                        }
                    }
                    num++;
                }
            }
            if (!list2.Any())
            {
                list.Add(new Rule_String("allSitePartsDescriptions", "HiddenOrNoSitePartDescription".Translate()));
                list.Add(new Rule_String("allSitePartsDescriptionsExceptFirst", "HiddenOrNoSitePartDescription".Translate()));
            }
            else
            {
                list.Add(new Rule_String("allSitePartsDescriptions", list2.ToClauseSequence().Resolve()));
                if (list2.Count >= 2)
                {
                    list.Add(new Rule_String("allSitePartsDescriptionsExceptFirst", list2.Skip(1).ToList().ToClauseSequence().Resolve()));
                }
                else
                {
                    list.Add(new Rule_String("allSitePartsDescriptionsExceptFirst", "HiddenOrNoSitePartDescription".Translate()));
                }
            }
            QuestGen.AddQuestDescriptionRules(list);
            QuestGen.AddQuestNameRules(list);
            QuestGen.AddQuestDescriptionConstants(dictionary);
            QuestGen.AddQuestNameConstants(dictionary);
            QuestGen.AddQuestNameRules(new List<Rule>
            {
                new Rule_String("site_label", site.Label)
            });
        }

        public SlateRef<MainMapDef> mainMapDef;
        public string buffer;
        public string bufferMin;
        public string siteIconPath;
        public string expandingIconPath;
        public bool replaceMapGeneration = true;
        public bool disdestroyBecauseOfNoColonist = false;
        public bool beUnreenterableWhenAllEnemiesDefeated;
        public bool reuseExistingSite = true;
        public bool enableBlack = true;
        public SlateRef<PlanetLayerDef> planetLayer;
        [NoTranslate]
        public SlateRef<string> storeAs;
        [NoTranslate]
        public SlateRef<string> faction;
        public SlateRef<Faction> overrideFaction;
        public IntRange distance = new IntRange(10, 20);
        public List<BiomeDef> blacklist = new List<BiomeDef>();
        public SlateRef<PlanetTile> tile;
    }
}
