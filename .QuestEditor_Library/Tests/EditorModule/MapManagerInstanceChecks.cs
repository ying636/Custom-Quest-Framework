using System.Runtime.CompilerServices;
using QuestEditor_Library;
using Verse;

internal static class MapManagerInstanceChecks
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
            GameComponent_MapManager component = new GameComponent_MapManager(active);
            active.components.Add(component);
            for (int update = 0; update < 16; update++) component.GameComponentUpdate();
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            for (int update = 0; update < 256; update++) component.GameComponentUpdate();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Check(allocated == 0, "idle map manager allocates zero bytes across 256 updates (actual: " + allocated + ")");
            GameComponent_MapManager temporary = new GameComponent_MapManager(other);
            other.components.Add(temporary);
            Check(ReferenceEquals(GameComponent_MapManager.Instance, component), "active component recovered after another game overwrites cache");
            Current.Game = other;
            Check(ReferenceEquals(GameComponent_MapManager.Instance, temporary), "instance follows current game instead of previous game");
            temporary.DisposeMaps();
            Current.Game = active;
            Check(ReferenceEquals(GameComponent_MapManager.Instance, component), "active component recovered after cached game disposal");
            Current.Game = null!;
            Check(GameComponent_MapManager.Instance == null, "no active game exposes no map manager instance");
            Current.Game = active;
            Check(ReferenceEquals(GameComponent_MapManager.Instance, component), "returning to game preserves the registered component");
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Current.Game.components = new List<GameComponent>();
            Check(GameComponent_MapManager.Instance == null && Current.Game.components.Count == 0, "missing component stays diagnosable without creating replacement state");
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
