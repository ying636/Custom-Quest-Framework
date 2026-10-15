using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library;

public class DomainParticipant : IExposable
{
    public void ExposeData()
    {
        Scribe_References.Look(ref pawn, "pawn", saveDestroyedThings: true);
        Scribe_References.Look(ref originalLord, "originalLord");
        Scribe_Values.Look(ref originalPosition, "originalPosition");
        Scribe_Values.Look(ref drafted, "drafted");
        Scribe_Values.Look(ref transitionStunEnd, "transitionStunEnd");
    }

    public Pawn? pawn;
    public Lord? originalLord;
    public IntVec3 originalPosition;
    public bool drafted;
    public int transitionStunEnd;
}
