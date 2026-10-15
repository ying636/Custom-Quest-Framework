using System.Runtime.CompilerServices;
using QuestEditor_Library;
using Verse;

internal static class MapHibernationInstanceChecks
{
    public static void Run()
    {
        Game previousGame = Current.Game;
        Game active = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        Game other = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        active.components = new List<GameComponent>();
        other.components = new List<GameComponent>();
        try
        {
            Current.Game = active;
            GameComponent_MapHibernation component = new GameComponent_MapHibernation(active);
            active.components.Add(component);
            GameComponent_MapHibernation temporary = new GameComponent_MapHibernation(other);
            other.components.Add(temporary);
            Check(ReferenceEquals(GameComponent_MapHibernation.Instance, component), "active component recovered after another game overwrites cache");
            Current.Game = other;
            Check(ReferenceEquals(GameComponent_MapHibernation.Instance, temporary), "instance follows current game instead of previous game");
            temporary.DisposeHibernatingMaps();
            Current.Game = active;
            Check(ReferenceEquals(GameComponent_MapHibernation.Instance, component), "active component recovered after cached game disposal");
            Current.Game = null!;
            Check(GameComponent_MapHibernation.Instance == null, "no active game exposes no hibernation instance");
            Current.Game = active;
            Check(ReferenceEquals(GameComponent_MapHibernation.Instance, component), "returning to game preserves the registered component");
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Current.Game.components = new List<GameComponent>();
            Check(GameComponent_MapHibernation.Instance == null && Current.Game.components.Count == 0, "missing component stays diagnosable without creating replacement state");
        }
        finally
        {
            Current.Game = previousGame;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }
}
