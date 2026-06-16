using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public static class DebugTools
	{
        [DebugAction("QuestEditor", "Clear mutant", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld, requiresOdyssey = true)]
        private static void ClearMutant()
        {
            PlanetTile tile = GenWorld.MouseTile(false);
            if (tile.Valid)
            {
                foreach (var mutator in tile.Tile.Mutators.ToList().ListFullCopy())
                {
					tile.Tile.RemoveMutator(mutator);
                };
                Find.World.renderer.GetLayer<WorldDrawLayer_Terrain>(tile.Layer).RegenerateNow();
            }
        }
        [DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.Playing)]
        private static void GetQuestTag()
        {
            if (UI.MouseCell().GetThingList(Find.CurrentMap).First() is Thing thing)
            {
               StringBuilder building = new StringBuilder(thing.Label);
				building.AppendLine();
				if (thing.questTags != null)
				{
                    foreach (var item in thing.questTags)
                    {
						building.AppendLine(item);
                    }
                }
				else 
				{
                    building.AppendLine("No tag");
                }
				Log.Message(building.ToString().Trim());
            }
        }
        [DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.Playing)]
		private static void GetQuestFromTarget()
		{
			if (UI.MouseCell().GetThingList(Find.CurrentMap).First() is Thing thing) 
			{
				Log.Message(GameTools.GetQuestFromThing(thing)?.name ?? "Null quest");
			}
		}
		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.Playing)]
		private static void GetCoreRect()
		{
			if (UI.MouseCell().InBounds(Find.CurrentMap) && UI.MouseCell().GetFirstThing<ZoneCore>(Find.CurrentMap) is ZoneCore core)
			{
				List<DebugMenuOption> options = new List<DebugMenuOption>();
				foreach (CustomMapDataDef def in DefDatabase<CustomMapDataDef>.AllDefsListForReading)
				{
					options.Add(new DebugMenuOption(def.label, DebugMenuOptionMode.Action, () =>
					{
						List<DebugMenuOption> options2 = new List<DebugMenuOption>();
						options2.Add(new DebugMenuOption("Base", DebugMenuOptionMode.Action, () =>
						{
							DebugTools.cells.AddRange(core.GetRect(core.CoreRotation, def.size.ToIntVec2, core.Position));
						}));
						def.extraDataByOrigin.ToList().ForEach(d3 =>
						{
							options2.Add(new DebugMenuOption(d3.Key.ToString() + d3.Value.rot.ToStringHuman(), DebugMenuOptionMode.Action, () =>
							{
								DebugTools.cells.AddRange(core.GetRect(core.CoreRotation, d3.Value.size.ToIntVec2, core.Position));
							}));
							d3.Value.extraDataByDirection.ToList().ForEach(d2 =>
							{
								options2.Add(new DebugMenuOption(d3.Key.ToString() + d2.Key.ToStringHuman(), DebugMenuOptionMode.Action, () =>
								{
									DebugTools.cells.AddRange(core.GetRect(core.CoreRotation, d2.Value.size.ToIntVec2, core.Position));
								}));
							});
						});
						Find.WindowStack.Add(new Dialog_DebugOptionListLister(options2));
					}));
				}
				Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
			}
		}
		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.Playing)]
		private static void GetCoreSize()
		{
			if (UI.MouseCell().GetThingList(Find.CurrentMap).Find(t => t is ZoneCore) is ZoneCore thing)
			{
				Log.Message(thing.size.ToString());
			}
		}
		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void GetCustomMapDataInformation()
		{
			List<DebugMenuOption> options = new List<DebugMenuOption>();
			foreach (CustomMapDataDef def in DefDatabase<CustomMapDataDef>.AllDefsListForReading)
			{
				options.Add(new DebugMenuOption(def.label, DebugMenuOptionMode.Action, () =>
				{
					List<DebugMenuOption> options2 = new List<DebugMenuOption>();
					options2.Add(new DebugMenuOption("Base", DebugMenuOptionMode.Action, () =>
					{
						Log.Message(def.GetInformation());
					}));
					def.extraDataByOrigin.ToList().ForEach(d3 =>
					{
						options2.Add(new DebugMenuOption(d3.Key.ToString() + d3.Value.rot.ToStringHuman(), DebugMenuOptionMode.Action, () =>
						{
							Log.Message(d3.Value.GetInformation());
						}));
						d3.Value.extraDataByDirection.ToList().ForEach(d2 =>
						{
							options2.Add(new DebugMenuOption(d3.Key.ToString() + d2.Key.ToStringHuman(), DebugMenuOptionMode.Action, () =>
							{
								Log.Message(d2.Value.GetInformation());
							}));
						});
					});
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(options2));
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
		}
		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void ClearGenerationCells()
		{
			GenStep_CustomMap.disgenerate = new List<IntVec3>();
			DebugTools.cells = new List<IntVec3>();
		}
		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void ShowCells()
		{
			GameComponent_Editor.showCells = true;
		}
		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void ShowReplaceDic()
		{
		 Log.Message(GenStep_CustomMap.replaceData.ToString());
		}
		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void AddDialog()
		{
			List<DebugMenuOption> options = new List<DebugMenuOption>();
			foreach (DialogManagerDef def in DefDatabase<DialogManagerDef>.AllDefsListForReading)
			{
				options.Add(new DebugMenuOption(def.defName, DebugMenuOptionMode.Tool, () =>
				{
					IntVec3 pos = UI.MouseCell();
					if (pos.InBounds(Find.CurrentMap) && pos.GetFirstPawn(Find.CurrentMap) is Pawn target)
					{
						GameComponent_Editor.Instance.AddDialog(target,def);
					}
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
		}
		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void DisclearGenerationData()
		{
			DebugTools.clearGenerationData = !DebugTools.clearGenerationData;
			Log.Message(DebugTools.clearGenerationData.ToString());
		}
		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void DisgenerateByCore()
		{
			CQFEditorTools.disgenerateByCore = !CQFEditorTools.disgenerateByCore;
			Log.Message(CQFEditorTools.disgenerateByCore.ToString());
		}
		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void GetMapInformation()
		{
			Map map = Find.CurrentMap;
			MapComponent_CustomMapData comonent = Find.CurrentMap.GetComponent<MapComponent_CustomMapData>();
			StringBuilder information = new StringBuilder();
			information.AppendLine("自定义地图：");
			information.AppendLine($"地图：{comonent.map.GetUniqueLoadID()},，入口：{(Find.CurrentMap.Parent as MapParent_Custom)?.entrance?.ThingID}，出口：{(Find.CurrentMap.Parent as MapParent_Custom)?.exit?.ThingID}");
			comonent.subMaps.ForEach(m => GetSubMapText(m,ref information));
            information.AppendLine(map.Parent.Spawned.ToString());
            information.AppendLine(map.Parent.Tile.ToString());
            information.AppendLine("派系" +map.ParentFaction?.Name);
            Log.Message(information.ToString().Trim());
		}

		public static void GetSubMapText(MapParent_Custom map,ref StringBuilder information) 
		{
			information.AppendLine($"地图：{map.GetUniqueLoadID()}，入口：{map.entrance?.ThingID}，出口：{map.exit?.ThingID}");
			foreach (MapParent_Custom m in map.Map.GetComponent<MapComponent_CustomMapData>().subMaps) 
			{
				GetSubMapText(m, ref information);
			}
		}

		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void GetComponentInformation()
		{
			GameComponent_Editor component = GameComponent_Editor.Instance;
			StringBuilder information = new StringBuilder();
			information.AppendLine("ExecutiveRequests".Translate());
			foreach (ExecutiveRequest request in component.Request)
			{
				information.AppendLine("---");
				information.AppendLine(request.ToString());
				information.AppendLine("---");
			}
			information.AppendLine("QuestDatas".Translate());
			foreach (KeyValuePair<int,QuestData> data in component.Datas) 
			{
				information.AppendLine("Index".Translate() + data.Key);
				information.AppendLine("Data".Translate());
				information.AppendLine(data.Value.ToString());
			}
			information.AppendLine("Dialogs".Translate());
			foreach (KeyValuePair<Thing, DialogManagerDef> tree in component.Dialogs)
			{
				information.AppendLine("Target".Translate() + tree.Key + "," + "Dialog".Translate() + tree.Value.ToString());
			}
			information.AppendLine("GlobalDatabase".Translate());
			information.AppendLine(component.GlobalDatabase.ToString());
			Log.Message(information.ToString().Trim());
		}
		[DebugAction("QuestEditor", null, false, false,
			actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void GetGameConditionInformation()
		{ 
			StringBuilder information = new StringBuilder();
			var map = Find.CurrentMap;
			if (map.GameConditionManager.ActiveConditions.Find(c => c is GameCondition_Actions) 
			    is GameCondition_Actions c)
			{
				information.AppendLine(c.Label);
				information.AppendLine("--Action--");
				foreach (var cqfAction in c.actions)
				{
					information.AppendLine(cqfAction.SaveToXElement(cqfAction.GetType().Name).ToString());
				}
			}
			Log.Message(information.ToString().Trim());
		}
		[DebugAction("QuestEditor", null, false, false,
			actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void TriggerCustomGameCondition()
		{ 
			StringBuilder information = new StringBuilder();
			var map = Find.CurrentMap;
			List<DebugMenuOption> options = new List<DebugMenuOption>();
			foreach (var activeCondition in map.GameConditionManager.ActiveConditions)
			{
				if (activeCondition is GameCondition_Actions c)
				{
					options.Add(new DebugMenuOption(c.Label, DebugMenuOptionMode.Action, () => { c.Trigger(); }));	
				}
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
		}
		[DebugAction("QuestEditor", null, false, false,
			actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void EndCustomGameCondition()
		{ 
			StringBuilder information = new StringBuilder();
			var map = Find.CurrentMap;
			List<DebugMenuOption> options = new List<DebugMenuOption>();
			foreach (var activeCondition in map.GameConditionManager.ActiveConditions)
			{
				if (activeCondition is GameCondition_Actions c)
				{
					options.Add(new DebugMenuOption(c.Label, DebugMenuOptionMode.Action, () => { c.End(); }));	
				}
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
		}
		[DebugAction("QuestEditor", null, false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
		private static void GenerateCustomMapData()
		{
			List<DebugMenuOption> options = new List<DebugMenuOption>();
			DefDatabase<CustomMapDataDef>.AllDefsListForReading.ForEach(d =>
			{
				options.Add(new DebugMenuOption(d.label, DebugMenuOptionMode.Action, () =>
				{
					List<DebugMenuOption> options2 = new List<DebugMenuOption>();
					options2.Add(new DebugMenuOption("Base", DebugMenuOptionMode.Tool, () =>
					{
						d.GenerateByCore(UI.MouseCell(), Find.CurrentMap, null, true, true);
					}));
					d.extraDataByOrigin.ToList().ForEach(d3 =>
					{
						options2.Add(new DebugMenuOption(d3.Key.ToString() + d3.Value.rot.ToStringHuman(), DebugMenuOptionMode.Tool, () =>
						{
							d3.Value.GenerateByCore(UI.MouseCell(), Find.CurrentMap,null, true, true);
						}));	
						d3.Value.extraDataByDirection.ToList().ForEach(d2 =>
					{
						options2.Add(new DebugMenuOption(d3.Key.ToString() + d2.Key.ToStringHuman(), DebugMenuOptionMode.Tool, () =>
						{
							d2.Value.GenerateByCore(UI.MouseCell(), Find.CurrentMap, null, true, true);
						}));
					});
					});
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(options2));

				}));
			});
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
		}

		public static List<IntVec3> cells = new List<IntVec3>();
		public static bool clearGenerationData = true;
	}
}

