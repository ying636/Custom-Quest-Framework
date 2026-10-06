using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFEditorLoader
    {
        public static string? LoadError { get; private set; }

        public static void Load(ModContentPack content, IReadOnlyList<string> folders)
        {
            if (folders.Count == 0 || CQFEditorBridge.IsLoaded) return;
            try
            {
                string[] files = folders.Select(folder => Path.Combine(folder, "CQF.Editor.dll")).Where(File.Exists).ToArray();
                if (files.Length == 0) throw new FileNotFoundException("CQF_Editor_MissingFile", string.Join("; ", folders));
                if (files.Length != 1) throw new InvalidDataException("Multiple CQF editor modules were selected.");
                string path = files[0];
                Assembly assembly = Assembly.LoadFrom(path);
                assembly.GetTypes();
                Type entry = assembly.GetType("QuestEditor_Library.CQFEditorModule", true);
                ICQFEditorModule module = (ICQFEditorModule)Activator.CreateInstance(entry);
                CQFEditorBridge.Attach(module);
                content.assemblies.loadedAssemblies.Add(assembly);
                GenTypes.ClearCache();
                LoadError = null;
            }
            catch (Exception error)
            {
                LoadError = error.ToString();
                if (error is ReflectionTypeLoadException types && types.LoaderExceptions != null)
                    LoadError += "\n" + string.Join("\n", Array.ConvertAll(types.LoaderExceptions, value => value.ToString()));
                Log.Error("CQF editor module load failed: " + LoadError);
            }
        }

    }
}
