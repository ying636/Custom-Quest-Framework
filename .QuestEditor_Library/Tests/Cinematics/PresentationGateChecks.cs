using System.Runtime.CompilerServices;
using QuestEditor_Library;
using Verse;

internal static class PresentationGateChecks
{
    public static int Run()
    {
        int checks = 0;
        Game firstGame = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        Game secondGame = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        CQFPresentationGate gate = CQFPresentationGate.For(firstGame);
        CQFPresentationGate otherGame = CQFPresentationGate.For(secondGame);
        object first = new string('x', 1);
        object sameValue = new string('x', 1);
        object third = new();
        Check(ReferenceEquals(gate, CQFPresentationGate.For(firstGame)), "same game shares its presentation gate");
        Check(!ReferenceEquals(gate, otherGame) && !otherGame.IsBusy, "new game starts with an independent gate");
        Check(!gate.IsBusy && !gate.IsHeldBy(first), "empty gate has no owner");
        Check(gate.TryAcquire(first) && gate.IsHeldBy(first) && gate.IsBusy, "first owner acquires the gate");
        Check(gate.TryAcquire(first), "repeated acquire by the holder is idempotent");
        Check(first.Equals(sameValue) && !gate.TryAcquire(sameValue) && !gate.IsHeldBy(sameValue), "ownership uses object identity");
        gate.Release(sameValue);
        Check(gate.IsHeldBy(first), "releasing a queued owner preserves the active owner");
        gate.Release(third);
        Check(gate.IsHeldBy(first), "unrelated release cannot clear another owner");
        Check(!gate.TryAcquire(sameValue) && !gate.TryAcquire(sameValue) && !gate.TryAcquire(third), "busy requests queue without duplicate entries");
        gate.Release(first);
        Check(gate.IsBusy && !gate.TryAcquire(third), "later waiter cannot overtake the queue");
        Check(gate.TryAcquire(sameValue), "first waiter receives released presentation access");
        gate.Release(first);
        Check(gate.IsHeldBy(sameValue), "stale previous owner cannot release a new owner");
        gate.Release(sameValue);
        Check(gate.TryAcquire(third), "second waiter follows without a duplicate blocking it");
        gate.Release(third);
        gate.Release(third);
        Check(!gate.IsBusy, "repeated release leaves the gate empty");
        Check(gate.TryAcquire(first) && !gate.TryAcquire(sameValue) && !gate.TryAcquire(third), "cancellation scenario queues both waiters");
        gate.Release(sameValue);
        gate.Release(first);
        Check(gate.TryAcquire(third), "cancelled head waiter cannot deadlock the queue");
        Check(otherGame.TryAcquire(first), "occupied old game never blocks the new game");
        gate.Release(third);
        Check(otherGame.IsHeldBy(first), "old game cleanup cannot release the new game");
        otherGame.Release(first);
        return checks;

        void Check(bool passed, string name)
        {
            if (!passed) throw new InvalidOperationException("Presentation gate check failed: " + name);
            checks++;
        }
    }
}
