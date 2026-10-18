using QuestEditor_Library;

public sealed class ReturnGateWorker : DomainWorker
{
    public int Attempts { get; private set; }

    public override bool PrepareReturn(bool visible)
    {
        Attempts++;
        return false;
    }
}
