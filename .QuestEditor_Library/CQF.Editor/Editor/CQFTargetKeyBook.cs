using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFTargetKeyBook
    {
        public static IReadOnlyList<string> Keys { get; private set; } = Array.AsReadOnly(Array.Empty<string>());
        public static int Revision { get; private set; }
        public static string CurrentConfigurationName { get; private set; } = string.Empty;
        public static string DefaultConfigurationName { get; private set; } = string.Empty;
        public static bool Modified { get; private set; }
        public static CQFTargetKeyBookConfigurationStore Store => store ??= new CQFTargetKeyBookConfigurationStore(Path.Combine(GenFilePaths.ConfigFolderPath, "CQF_TargetKeyBook"));

        public static void Add(string key)
        {
            Replace(keys.Concat(new[] { key }));
        }

        public static void Edit(int index, string key)
        {
            string[] replacement = (string[])keys.Clone();
            replacement[index] = key;
            Replace(replacement);
        }

        public static void Remove(int index)
        {
            if (index < 0 || index >= keys.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            Replace(keys.Where((_, position) => position != index));
        }

        public static void Save(string name)
        {
            Store.Save(name, keys);
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
            Log.Error("[CQF] Target key book: " + exception);
            string detail = exception.Message.CanTranslate() ? exception.Message.Translate().ToString() : exception.Message;
            Messages.Message("CQF_TargetKeyBook_Error".Translate() + ": " + detail, MessageTypeDefOf.RejectInput, false);
        }

        public static void Replace(IEnumerable<string> replacement)
        {
            string[] values = CQFTargetKeyBookConfigurationStore.ValidateKeys(replacement);
            if (keys.SequenceEqual(values))
            {
                return;
            }
            keys = values;
            Keys = Array.AsReadOnly(keys);
            Modified = true;
            Revision++;
        }

        private static string[] keys = Array.Empty<string>();
        private static CQFTargetKeyBookConfigurationStore? store;
    }
}
