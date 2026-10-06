using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFSignalBook
    {
        public static IReadOnlyList<string> Signals { get; private set; } = Array.AsReadOnly(Array.Empty<string>());
        public static int Revision { get; private set; }
        public static string CurrentConfigurationName { get; private set; } = string.Empty;
        public static string DefaultConfigurationName { get; private set; } = string.Empty;
        public static bool Modified { get; private set; }
        public static CQFSignalBookConfigurationStore Store => store ??= new CQFSignalBookConfigurationStore(Path.Combine(GenFilePaths.ConfigFolderPath, "CQF_SignalBook"));

        public static void Add(string signal)
        {
            Replace(signals.Concat(new[] { signal }));
        }

        public static void Edit(int index, string signal)
        {
            string[] replacement = (string[])signals.Clone();
            replacement[index] = signal;
            Replace(replacement);
        }

        public static void Remove(int index)
        {
            if (index < 0 || index >= signals.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            Replace(signals.Where((_, position) => position != index));
        }

        public static void Save(string name)
        {
            Store.Save(name, signals);
            CurrentConfigurationName = name;
            Modified = false;
        }

        public static void Load(string name)
        {
            string[] replacement = Store.Load(name);
            Replace(replacement);
            CurrentConfigurationName = name;
            Modified = false;
        }

        public static void SetDefault(string name)
        {
            Store.SetDefault(name);
            DefaultConfigurationName = name ?? string.Empty;
        }

        public static void LoadDefault()
        {
            try
            {
                string name = Store.DefaultConfigurationName;
                if (name.Length > 0)
                {
                    Load(name);
                }
                DefaultConfigurationName = name;
            }
            catch (Exception exception)
            {
                ReportError(exception);
            }
        }

        public static void ReportError(Exception exception)
        {
            Log.Error("[CQF] Signal book: " + exception);
            string detail = exception.Message.CanTranslate() ? exception.Message.Translate().ToString() : exception.Message;
            Messages.Message("CQF_SignalBook_Error".Translate() + ": " + detail, MessageTypeDefOf.RejectInput, false);
        }

        public static void Replace(IEnumerable<string> replacement)
        {
            string[] values = CQFSignalBookConfigurationStore.ValidateSignals(replacement);
            if (signals.SequenceEqual(values))
            {
                return;
            }
            signals = values;
            Signals = Array.AsReadOnly(signals);
            Modified = true;
            Revision++;
        }

        private static string[] signals = Array.Empty<string>();
        private static CQFSignalBookConfigurationStore? store;
    }
}
