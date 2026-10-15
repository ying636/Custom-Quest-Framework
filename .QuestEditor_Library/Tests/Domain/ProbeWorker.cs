using QuestEditor_Library;

public sealed class ProbeWorker : DomainWorker
{
    public int Collapses { get; private set; }

    public override void Collapsed(string reason) => Collapses++;
}
