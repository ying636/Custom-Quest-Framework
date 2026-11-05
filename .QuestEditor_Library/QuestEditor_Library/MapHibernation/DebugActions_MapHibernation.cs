using LudeonTK;
using Verse;

namespace QuestEditor_Library
{
    public static class DebugActions_MapHibernation
    {
        [DebugAction("CQF", "CQF_MapHibernation", allowedGameStates = AllowedGameStates.Playing)]
        public static DebugActionNode MapHibernation()
        {
            DebugActionNode root = new DebugActionNode("Map hibernation", action: () =>
            {
                if (Current.ProgramState == ProgramState.Playing && GameComponent_MapManager.Instance == null)
                    Log.Error("[CQF] Map hibernation component is unavailable.");
            });
            root.visibilityGetter = () => Current.ProgramState == ProgramState.Playing && GameComponent_MapManager.Instance != null;
            root.childGetter = () =>
            {
                List<DebugActionNode> actions = new List<DebugActionNode>();
                if (Current.ProgramState != ProgramState.Playing) return actions;
                GameComponent_MapManager? component = GameComponent_MapManager.Instance;
                if (component == null)
                {
                    Log.Error("[CQF] Map hibernation component is unavailable.");
                    return actions;
                }
                foreach (Map map in component.Game.Maps)
                {
                    actions.Add(new DebugActionNode("Hibernate map " + map.GetUniqueLoadID(),
                        action: () =>
                        {
                            if (Current.ProgramState == ProgramState.Playing && GameComponent_MapManager.Instance == component)
                                component.Hibernate(map);
                        })
                    {
                        visibilityGetter = () => Current.ProgramState == ProgramState.Playing && GameComponent_MapManager.Instance == component
                    });
                }
                foreach (Map map in component.HibernatingMaps)
                {
                    actions.Add(new DebugActionNode("Activate map " + map.GetUniqueLoadID(),
                        action: () =>
                        {
                            if (Current.ProgramState == ProgramState.Playing && GameComponent_MapManager.Instance == component)
                                component.Game.CurrentMap = component.Activate(map);
                        })
                    {
                        visibilityGetter = () => Current.ProgramState == ProgramState.Playing && GameComponent_MapManager.Instance == component
                    });
                }
                return actions;
            };
            return root;
        }
    }
}
