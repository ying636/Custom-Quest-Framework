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
            GameComponent_MapManager component = GameComponent_MapManager.Instance
                ?? throw new System.InvalidOperationException("[CQF] Map hibernation component is unavailable.");
            foreach (Map map in Current.Game.Maps)
            {
                root.AddChild(new DebugActionNode("Hibernate map " + map.GetUniqueLoadID(),
                    action: () => component.Hibernate(map)));
            }
            foreach (Map map in component.HibernatingMaps)
            {
                root.AddChild(new DebugActionNode("Activate map " + map.GetUniqueLoadID(),
                    action: () => Current.Game.CurrentMap = component.Activate(map)));
            }
            return root;
        }
    }
}
