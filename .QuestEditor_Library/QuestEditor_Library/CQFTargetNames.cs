using System.Collections.Generic;

namespace QuestEditor_Library
{
    public static class CQFTargetNames
    {
        public static IReadOnlyList<string> Reserved => reserved;

        private static readonly string[] reserved = { "Interviewee", "Interviewer", "CustomThing", "Trigger", "Captured", "Position", "Inner", "Target" };
    }
}
