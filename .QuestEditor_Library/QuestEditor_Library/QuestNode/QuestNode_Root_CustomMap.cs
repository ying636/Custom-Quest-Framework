using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using RimWorld.Planet;
using Verse.Grammar;
using UnityEngine;
using System.Xml;
using System.IO;
using System.Reflection;

namespace QuestEditor_Library
{
    public abstract class QuestNode_Root_CustomMap : QuestNode , IDrawable
	{
		public abstract CustomMapDataDef GetMap();
        protected override void RunInt()
        {
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
            Faction faction = GameTools.GetFaction(this.faction.GetValue(slate),null);
			if (this.overrideFaction != null && this.overrideFaction.GetValue(slate) is Faction f) 
			{
				faction = f;
			}
			PlanetTile root = Find.AnyPlayerHomeMap.Tile;
            PlanetTile tile = default;
            PlanetLayer layer = null;
            if (this.planetLayer != null && this.planetLayer.GetValue(slate) is PlanetLayerDef layerDef)
            {
                layer = Find.WorldGrid.FirstLayerOfDef(layerDef);
            }
            if (slate.Get<PlanetTile>("CQFMapTile") != default(PlanetTile)
                || (this.tile != null && !this.tile.ToString().NullOrEmpty()
                && this.tile.TryGetValue(slate, out tile))
                || TileFinder.TryFindNewSiteTile(out tile, root
                , this.distance.min, this.distance.max, false, null, 0.5f, true, TileFinderMode.Random, false,
                layer != null && layer.Def.isSpace, layer,
                 (x) => this.blacklist == null ||
                !this.blacklist.Contains(Find.World.grid[x].PrimaryBiome)))
            {
                if (tile == default) 
				{
					return;
				}

				var map = this.GetMap(); 
				CustomSite site = QuestNode_Root_CustomMap
					.GenerateCustomSite(Gen.YieldSingle<SitePartDefWithParams>(
						new SitePartDefWithParams(DefDatabase<SitePartDef>.GetNamed("QE_CustomSite"), new SitePartParams()))
					, tile, faction, false, null,this.worldObjectDef); 
				site.siteIconPath = this.siteIconPath;
				site.reenterable = this.reenterable;
				site.beUnreenterableWhenAllEnemiesDefeated = this.beUnreenterableWhenAllEnemiesDefeated;
                site.expandingIconPath = this.expandingIconPath;
                site.quest = quest;
				site.disdestroyBecauseOfNoColonist = this.disdestroyBecauseOfNoColonist;
				site.customLabel = map.label;
				site.customDescription = map.description;
				site.mapDef = map; 
				site.replaceMapGeneration = this.replaceMapGeneration;
                quest.SpawnWorldObject(site, null, null);
                if (this.storeAs.GetValue(slate) != null)
                {
	                slate.Set<Site>(this.storeAs.GetValue(slate), site);   
                }
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

        public virtual void Draw(ref float y, Rect inRect,float x)
        {
			y += 10f;
			CQFEditorTools.DrawLabelAndText_SlateRef_Line(y, "tile".Translate(), ref this.tile, x + 7f, 110f);
			TooltipHandler.TipRegion(new Rect(x + 7f, y, 150f, 25f), "tile_Tip".Translate());	
			y += 30f;
            CQFEditorTools.DrawSelectableText(y, "MapFaction".Translate(), ref this.faction, () => CQFEditorTools.DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll((f) => !f.isPlayer), (f) => this.faction = f.defName, (f) => f.label, new List<FloatMenuOption>()
            {
                new FloatMenuOption("RandomHostile".Translate(),() => this.faction = "RandomHostile"),
                new FloatMenuOption("RandomAlly".Translate(),() => this.faction = "RandomAlly"),
                new FloatMenuOption("RandomNeutral".Translate(),() => this.faction = "RandomNeutral"),
            }), 7f + x, 120f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "SiteIconPath".Translate(), ref this.siteIconPath, x + 7f, 150f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "ExpandingIconPath".Translate(), ref this.expandingIconPath, x + 7f, 150f);
            y += 30f;
			Widgets.CheckboxLabeled(new Rect(x + 7f,y,300f,25f), "DisdestroyBecauseOfNoColonist".Translate(), ref this.disdestroyBecauseOfNoColonist);
			y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 7f, y, 300f, 25f), "Reenterable".Translate(),
				ref this.reenterable);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 7f, y, 300f, 25f),
                "BeUnreenterableWhenAllEnemiesDefeated".Translate(), ref this.beUnreenterableWhenAllEnemiesDefeated);
            y += 30f;
            Rect r = new Rect(x + 7f, y, 250f, 25f);
			Widgets.CheckboxLabeled(r, "replaceMapGeneration".Translate(), ref this.replaceMapGeneration);
			TooltipHandler.TipRegion(r, "replaceMapGeneration_Tip".Translate());
			y += 30f;
			CQFEditorTools.DrawLabelAndText_SlateRef_Line(y, "StoreAsText".Translate(), ref this.storeAs, x + 7f, 100f);
			y += 30f;
			CQFEditorTools.DrawIntRange(ref y, "MapDistance".Translate(), ref this.distance, ref this.buffer, ref this.bufferMin, x + 7f);
			Rect worldObjectDefRect = new Rect(x + 7f, y, 800f, 25f);
			CQFEditorTools.DrawSelectButton(x + 7f,ref y,
				"WorldObjectDefOfSite".Translate(this.worldObjectDef == null ? 
				"Default".Translate().ToString()
				: this.worldObjectDef.label),DefDatabase<WorldObjectDef>.AllDefsListForReading,
				d => this.worldObjectDef = d,d => d.label,
				new List<FloatMenuOption>()
				{
					new FloatMenuOption("Default".Translate(), () => this.worldObjectDef = null)
				});
			TooltipHandler.TipRegion(worldObjectDefRect, "WorldObjectDefOfSite_Tip".Translate());
			List<PlanetLayerDef> PlanetLayerDef = DefDatabase<PlanetLayerDef>.AllDefsListForReading;
			CQFEditorTools.DrawSelectableField(x + 7f,ref y, "planetLayer".Translate(this.planetLayer == null ?
				null : this.planetLayer.ToString())
				,PlanetLayerDef, d => this.planetLayer = d, d => d.label,new Vector2(120f,25f)); 
            string listText = "";
            this.blacklist.ForEach(b => listText = b.label + "," + listText);
            Widgets.Label(new Rect(x + 7f, y, 300f, 60f), (enableBlack ? "BiomesBlackList".Translate() : "BiomesWhiteList".Translate()) + listText);
			y += 70f;
			if (Widgets.ButtonText(new Rect(x + 7f, y, 70f, 25f), "Add".Translate()))
            {
                CQFEditorTools.DrawFloatMenu<BiomeDef>(DefDatabase<BiomeDef>.AllDefs.ToList().FindAll(b => !this.blacklist.Contains(b)), (b) => this.blacklist.Add(b), (b) => b.label);
            }
            if (Widgets.ButtonText(new Rect(x + 70f, y, 70f, 25f), "Delete".Translate()))
            {
                CQFEditorTools.DrawFloatMenu<BiomeDef>(this.blacklist, (b) => this.blacklist.Remove(b), (b) => b.label);
            }
			y += 30f;
        }

		public static CustomSite GenerateCustomSite(IEnumerable<SitePartDefWithParams> sitePartsParams,
			PlanetTile tile, Faction faction, bool hiddenSitePartsPossible = false,
			RulePack singleSitePartRules = null, WorldObjectDef worldObjectDef = null)
		{
			Slate slate = QuestGen.slate;
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
				SitePartParams parms = 
					SitePartDefOf.PossibleUnknownThreatMarker.Worker
					.GenerateDefaultParams(0f, tile, faction);
				SitePartDefWithParams val = 
					new SitePartDefWithParams(SitePartDefOf.PossibleUnknownThreatMarker, parms);
				sitePartsParams = sitePartsParams.Concat(Gen.
					YieldSingle<SitePartDefWithParams>(val));
			}
			CustomSite site = QuestNode_Root_CustomMap.
				MakeCustomSite(sitePartsParams, tile, faction, true,worldObjectDef);
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
							rule_String.keyword = string.Concat(new object[]
							{
								"sitePart",
								num,
								"_",
								rule_String.keyword
							});
						}
						list.Add(rule);
					}
					foreach (KeyValuePair<string, string> keyValuePair in dictionary2)
					{
						string text2 = keyValuePair.Key;
						if (num != 0)
						{
							text2 = string.Concat(new object[]
							{
								"sitePart",
								num,
								"_",
								text2
							});
						}
						if (!dictionary.ContainsKey(text2))
						{
							dictionary.Add(text2, keyValuePair.Value);
						}
					}
					num++;
				}
			}
			if (!list2.Any<string>())
			{
				list.Add(new Rule_String("allSitePartsDescriptions", "HiddenOrNoSitePartDescription".Translate()));
				list.Add(new Rule_String("allSitePartsDescriptionsExceptFirst", "HiddenOrNoSitePartDescription".Translate()));
			}
			else
			{
				list.Add(new Rule_String("allSitePartsDescriptions", list2.ToClauseSequence().Resolve()));
				if (list2.Count >= 2)
				{
					list.Add(new Rule_String("allSitePartsDescriptionsExceptFirst", list2.Skip(1).ToList<string>().ToClauseSequence().Resolve()));
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
			return site;
		}

		public static CustomSite MakeCustomSite(IEnumerable<SitePartDefWithParams> siteParts
			, PlanetTile tile, Faction faction, bool ifHostileThenMustRemainHostile = true
			,WorldObjectDef worldObjectDef = null)
		{
			CustomSite site = (CustomSite)WorldObjectMaker.MakeWorldObject(
				worldObjectDef != null ? worldObjectDef : (tile.Layer.Def.isSpace ?
            QEDefOf.QE_SpaceCustomSite : QEDefOf.CQF_CustomSite));
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
					var part = new SitePart(site, sitePartDefWithParams.def, sitePartDefWithParams.parms);
                    site.AddPart(part);
				}
			}
			site.desiredThreatPoints = site.ActualThreatPoints;
			return site;
		}

		public string buffer;
        public string bufferMin;
        public string siteIconPath;
        public string expandingIconPath;
		public bool replaceMapGeneration = false;
		public bool disdestroyBecauseOfNoColonist = false;
        public bool reenterable;
        public bool beUnreenterableWhenAllEnemiesDefeated;
        public bool enableBlack = true;
		public WorldObjectDef worldObjectDef;
        public SlateRef<PlanetLayerDef> planetLayer;
        [NoTranslate]
        public SlateRef<string> storeAs;
        [NoTranslate]
        public SlateRef<string> faction;
		public SlateRef<Faction> overrideFaction;
        public IntRange distance = new IntRange(10,20);
		public List<BiomeDef> blacklist = new List<BiomeDef>();
		public SlateRef<PlanetTile> tile;
    }
}
