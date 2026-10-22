using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using QuestEditor_Library;
using RimWorld;
using Verse;

internal static class EntityBookRecordPerformance
{
    public static void Run(Func<int, Pawn> newPawn, Func<int, Quest> newQuest)
    {
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        GameComponent_EntityBook book = new(game);
        Current.Game = game;
        game.components = new() { book };
        Quest[] quests = { newQuest(701), newQuest(702), newQuest(703) };
        string[] sources = Enumerable.Range(0, 2000).Select(index => "spawn:CQF_Performance_" + index).ToArray();
        int identity = 10000;
        for (int index = 0; index < sources.Length; index++)
            book.RegisterBatch(sources[index], Enumerable.Range(0, 5).Select(_ => newPawn(identity++)).ToArray(), quests[index % quests.Length]);
        for (int index = 0; index < 1000; index++) _ = book.GetRecords(sources[index], quests[index % quests.Length]).Count;
        List<double> milliseconds = new();
        List<long> allocations = new();
        int checksum = 0;
        for (int sample = 0; sample < 5; sample++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Stopwatch timer = new();
            long before = GC.GetAllocatedBytesForCurrentThread();
            timer.Start();
            for (int iteration = 0; iteration < 20000; iteration++)
            {
                int index = iteration % sources.Length;
                checksum += book.GetRecords(sources[index], quests[index % quests.Length]).Count;
            }
            timer.Stop();
            allocations.Add(GC.GetAllocatedBytesForCurrentThread() - before);
            milliseconds.Add(timer.Elapsed.TotalMilliseconds);
        }
        if (checksum != 500000) throw new InvalidOperationException("Record lookup benchmark lost scoped members.");
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Sources = sources.Length,
            Records = book.Records.Count,
            LookupsPerSample = 20000,
            Samples = milliseconds.Count,
            Milliseconds = milliseconds,
            AllocatedBytes = allocations,
            MedianMilliseconds = milliseconds.OrderBy(value => value).ElementAt(milliseconds.Count / 2),
            MedianAllocatedBytes = allocations.OrderBy(value => value).ElementAt(allocations.Count / 2),
            Checksum = checksum
        }));
    }
}
