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
                 x => this.ValidTile(x)))
            {
                if (tile == default) 
				{
					return;
				}
                if (!this.ValidTile(tile))
                {
                    quest.End(QuestEndOutcome.Fail);
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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestNode_Root_CustomMap.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawBiomeFilter(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestNode_Root_CustomMap.DrawBiomeFilter(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawWorldConditions(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestNode_Root_CustomMap.DrawWorldConditions(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawSectionHeader(ref float y, Rect inRect, float x, string title, Action addAction, Action removeAction)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x,
                title,
                addAction,
                removeAction
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestNode_Root_CustomMap.DrawSectionHeader(Ref:float,None:UnityEngine.Rect,None:float,None:string,None:System.Action,None:System.Action)", this, arguments);
            y = (float)arguments[0];
        }
        internal string WorldConditionLabel(WorldCondition condition)
		{
			string typeName = condition.GetType().Name.Translate();
			string targetName = null;
			if (condition is WorldCondition_WorldObject worldObject)
			{
				targetName = worldObject.objectDef?.label ?? worldObject.objectDef?.defName;
			}
			else if (condition is WorldCondition_Landmark landmark)
			{
				targetName = landmark.landmark?.label ?? landmark.landmark?.defName;
			}
			else if (condition is WorldCondition_Biome biome)
			{
				targetName = biome.biome?.label ?? biome.biome?.defName;
			}
			else if (condition is WorldCondition_TileMutator mutator)
			{
				targetName = mutator.mutator?.label ?? mutator.mutator?.defName;
			}
			else if (condition is WorldCondition_TotalSkill totalSkill)
			{
				targetName = totalSkill.skill?.label ?? totalSkill.skill?.defName;
			}
			return targetName.NullOrEmpty() ? typeName : "ConditionWithTarget".Translate(typeName, targetName);
		}

		private bool ValidTile(PlanetTile tile)
		{
			return (this.blacklist == null || !this.blacklist.Contains(Find.World.grid[tile].PrimaryBiome))
				&& (this.worldConditions.NullOrEmpty() || !this.worldConditions.Exists(condition => !condition.Satisfied(tile)));
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
		public List<WorldCondition> worldConditions = new List<WorldCondition>();
		public SlateRef<PlanetTile> tile;
    }
}
