using Verse;

namespace QuestEditor_Library;

public interface IDomainMapTransfer
{
    void DomainMapTransferred(Map previous, IntVec3 offset);
}
