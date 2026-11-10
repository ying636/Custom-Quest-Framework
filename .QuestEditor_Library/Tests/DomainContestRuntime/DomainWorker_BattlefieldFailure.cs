using QuestEditor_Library;

namespace CQF.DomainContestRuntimeChecks;

public sealed class DomainWorker_BattlefieldFailure : DomainWorker
{
    public override bool SupportsContests => true;

    public override void ContestStarted()
    {
        if (FailNext)
        {
            FailNext = false;
            throw new InvalidOperationException("CQF_EXPECTED_BATTLEFIELD_ROLLBACK");
        }
    }

    public static bool FailNext;
}
