using LudeonTK;
using Verse;

namespace QuestEditor_Library;

public static class DebugActions_CompositeMaps
{
    [DebugAction("CQF", "CQF_CompositeMap", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static List<DebugActionNode> Generate()
    {
        return DefDatabase<CQFCompositeMapDef>.AllDefsListForReading.Select(definition =>
            new DebugActionNode(definition.defName.CanTranslate() ? definition.defName.Translate().ToString() : definition.LabelCap.ToString(), DebugActionType.ToolMap, () =>
            {
                Map source = Find.CurrentMap;
                IntVec3 cell = UI.MouseCell();
                GameComponent_MapManager manager = GameComponent_MapManager.Instance
                    ?? throw new InvalidOperationException("[CQF] Map manager is unavailable.");
                LongEventHandler.QueueLongEvent(() =>
                {
                    CQFCompositeMapInstance instance = manager.GenerateCompositeMap(definition, source, cell);
                    manager.ViewCompositeMap(instance);
                }, "GenerateSubMap", doAsynchronously: false, exceptionHandler: null);
            })).ToList();
    }
}
