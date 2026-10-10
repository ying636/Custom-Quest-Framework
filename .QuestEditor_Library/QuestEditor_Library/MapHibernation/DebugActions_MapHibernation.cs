using LudeonTK;
using Verse;

namespace QuestEditor_Library
{
    public static class DebugActions_MapHibernation
    {
        [DebugAction("CQF", "CQF_MapHibernation", allowedGameStates = AllowedGameStates.Playing)]
        public static DebugActionNode MapHibernation()
        {
            DebugActionNode root = new DebugActionNode("CQF_MapHibernation".Translate());
            GameComponent_MapHibernation component = GameComponent_MapHibernation.Instance
                ?? throw new System.InvalidOperationException("[CQF] Map hibernation component is unavailable.");
            foreach (Map map in Current.Game.Maps)
            {
                root.AddChild(new DebugActionNode("CQF_HibernateMap".Translate() + " " + map.GetUniqueLoadID(),
                    action: () => component.Hibernate(map)));
            }
            foreach (Map map in component.HibernatingMaps)
            {
                root.AddChild(new DebugActionNode("CQF_ActivateMap".Translate() + " " + map.GetUniqueLoadID(),
                    action: () => Current.Game.CurrentMap = component.Activate(map)));
            }
            return root;
        }
    }
}
