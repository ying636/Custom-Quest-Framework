using QuestEditor_Library;
using Verse;

public sealed class ProbeAnchor : Building_Domain
{
    public int Removals { get; private set; }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish) => Removals++;
}
