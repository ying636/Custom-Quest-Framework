using System;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFEditorBridge
    {
        public static bool IsLoaded => module != null;
        public static ICQFEditorModule? Module => module;
        public static ICQFEditorModule Editor => module ?? throw new InvalidOperationException("CQF_Editor_NotLoaded".Translate());

        public static void Attach(ICQFEditorModule value)
        {
            if (module != null)
                throw new InvalidOperationException("CQF editor already initialized.");
            if (value.ApiVersion != ApiVersion)
                throw new InvalidOperationException("CQF_Editor_VersionMismatch");
            value.Initialize();
            module = value;
        }

        public const int ApiVersion = 2;
        private static ICQFEditorModule? module;
    }
}
