using QuestEditor_Library;

internal sealed class DomainDisposalProbeWorker : DomainWorker
{
    public override void Dispose()
    {
        Disposals++;
        if (ThrowOnDispose) throw new InvalidOperationException("CQF_Check_WorkerDisposalFailure");
    }

    public bool ThrowOnDispose;
    public int Disposals;
}
