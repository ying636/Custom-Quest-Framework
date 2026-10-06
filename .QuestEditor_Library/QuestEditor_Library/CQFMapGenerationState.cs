using System.Collections.Generic;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFMapGenerationState
    {
        public static bool DisgenerateByCore;
        public static bool ClearGenerationData = true;
        public static List<IntVec3> Cells = new List<IntVec3>();
    }
}
