using System.IO;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFContentPaths
    {
        public static string Quests => Path.Combine(root ?? throw new System.InvalidOperationException("CQF content root is unavailable."), "Quests");

        public static void Initialize(string directory)
        {
            root = directory;
        }

        private static string? root;
    }
}
