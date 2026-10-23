using LudeonTK;
using Verse;

namespace QuestEditor_Library
{
    public static class DebugActions_MapHibernation
    {
        [DebugAction("CQF", "CQF_MapHibernation", allowedGameStates = AllowedGameStates.Playing)]
        public static DebugActionNode MapHibernation()
        {
            DebugActionNode root = new DebugActionNode("Map hibernation");
            root.childGetter = () =>
            {
                GameComponent_MapManager component = GameComponent_MapManager.Instance
                    ?? throw new System.InvalidOperationException("[CQF] Map hibernation component is unavailable.");
                List<DebugActionNode> actions = new List<DebugActionNode>();
                foreach (Map map in Current.Game.Maps)
                {
                    actions.Add(new DebugActionNode("Hibernate map " + map.GetUniqueLoadID(),
                        action: () => component.Hibernate(map)));
                }
                foreach (Map map in component.HibernatingMaps)
                {
                    actions.Add(new DebugActionNode("Activate map " + map.GetUniqueLoadID(),
                        action: () => Current.Game.CurrentMap = component.Activate(map)));
                }
                return actions;
            };
            return root;
        }
    }
}
