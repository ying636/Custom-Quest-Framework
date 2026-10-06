using System;
using Verse;
namespace QuestEditor_Library
{
    public static class CQFAIBridge
    {
        public static bool IsLoaded => CQFEditorBridge.IsLoaded && openWindow != null;
        public static Action<CustomMapDataDef>? GenerateMap { get; private set; }
        public static void Register(Action<CQFAIEditorContext?> open, Action<CustomMapDataDef> generateMap)
        {
            if (open == null) throw new ArgumentNullException(nameof(open));
            if (generateMap == null) throw new ArgumentNullException(nameof(generateMap));
            if (openWindow != null) throw new InvalidOperationException("CQF AI editor entry already registered.");
            openWindow = open;
            GenerateMap = generateMap;
        }
        public static void Open(CQFAIEditorContext? context = null)
        {
            if (!IsLoaded)
            {
                Messages.Message("CQF_AI_NotLoaded".Translate(), RimWorld.MessageTypeDefOf.RejectInput);
                return;
            }
            openWindow!(context);
        }

        private static Action<CQFAIEditorContext?>? openWindow;
    }
}
