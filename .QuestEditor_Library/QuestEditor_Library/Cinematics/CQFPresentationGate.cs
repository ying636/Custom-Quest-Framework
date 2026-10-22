using System.Runtime.CompilerServices;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFPresentationGate
{
    private CQFPresentationGate() { }

    public bool IsBusy => holder != null || waiting.Count > 0;

    public static CQFPresentationGate For(Game game)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        return gates.GetValue(game, _ => new CQFPresentationGate());
    }

    public bool TryAcquire(object owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (ReferenceEquals(holder, owner)) return true;
        int index = waiting.FindIndex(candidate => ReferenceEquals(candidate, owner));
        if (holder != null || waiting.Count > 0 && index != 0)
        {
            if (index < 0) waiting.Add(owner);
            return false;
        }
        if (index == 0) waiting.RemoveAt(0);
        holder = owner;
        return true;
    }

    public void Release(object owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (ReferenceEquals(holder, owner)) holder = null;
        int index = waiting.FindIndex(candidate => ReferenceEquals(candidate, owner));
        if (index >= 0) waiting.RemoveAt(index);
    }

    public bool IsHeldBy(object owner)
        => owner != null && ReferenceEquals(holder, owner);

    private static readonly ConditionalWeakTable<Game, CQFPresentationGate> gates = new();
    private readonly List<object> waiting = new();
    private object? holder;
}
