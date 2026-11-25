#if RIMWORLD_1_7
using Verse;

namespace QuestEditor_Library;

internal readonly record struct CQFCrossMapStep(Map Source, Map Destination, IntVec3 Departure, IntVec3 Arrival, Thing? Portal);
#endif
