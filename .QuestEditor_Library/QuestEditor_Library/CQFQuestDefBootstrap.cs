using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    [StaticConstructorOnStartup]
    public static class CQFQuestDefBootstrap
    {
        static CQFQuestDefBootstrap()
        {
            try
            {
                LoadAll();
            }
            catch (System.Exception e)
            {
                Log.Error("CQF quest bootstrap load error: " + e);
            }
        }

        public static void HotLoadDialogTreeDef(DialogTreeDef currentDef)
        {
            ReplaceDef(currentDef, currentDef);
        }

        public static void HotLoadDialogManagerDef(DialogManagerDef currentDef)
        {
            ReplaceDef(currentDef, currentDef);
        }

        public static void HotLoadMainMapDef(MainMapDef currentDef)
        {
            ReplaceDef(currentDef, currentDef);
        }

        public static void HotLoadComplexPawnDef(ComplexPawnDef currentDef)
        {
            ReplaceDef(currentDef, currentDef);
        }

        private static void LoadAll()
        {
            string questPath = Page_QuestEditor.Path;
            if (questPath.NullOrEmpty())
            {
                return;
            }
            LoadDefs(questPath, "//QuestScriptDef", DefDatabase<QuestScriptDef>.AllDefsListForReading, node => DirectXmlToObject.ObjectFromXml<QuestScriptDef>(node, false), def => DefDatabase<QuestScriptDef>.Add(def));
            LoadDefs(questPath + @"\Map", "//QuestEditor_Library.CustomMapDataDef", DefDatabase<CustomMapDataDef>.AllDefsListForReading, node => DirectXmlToObject.ObjectFromXml<CustomMapDataDef>(node, false), def => DefDatabase<CustomMapDataDef>.Add(def));
            LoadDefs(questPath + @"\Map", "//QuestEditor_Library.MainMapDef", DefDatabase<MainMapDef>.AllDefsListForReading, node => DirectXmlToObject.ObjectFromXml<MainMapDef>(node, false), def => DefDatabase<MainMapDef>.Add(def));
            LoadDefs(questPath + @"\DialogTree", "//QuestEditor_Library.DialogTreeDef", DefDatabase<DialogTreeDef>.AllDefsListForReading, node => DirectXmlToObject.ObjectFromXml<DialogTreeDef>(node, false), def => DefDatabase<DialogTreeDef>.Add(def));
            LoadDefs(questPath + @"\DialogTree", "//QuestEditor_Library.DialogManagerDef", DefDatabase<DialogManagerDef>.AllDefsListForReading, node => DirectXmlToObject.ObjectFromXml<DialogManagerDef>(node, false), def => DefDatabase<DialogManagerDef>.Add(def));
            LoadDefs(questPath + @"\Pawn", "//QuestEditor_Library.ComplexPawnDef", DefDatabase<ComplexPawnDef>.AllDefsListForReading, node => DirectXmlToObject.ObjectFromXml<ComplexPawnDef>(node, false), def => DefDatabase<ComplexPawnDef>.Add(def));
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            foreach (LoadedDefInfo loadedDef in CQFQuestDefBootstrap.loadedDefs)
            {
                ValidateDef(loadedDef);
            }
        }

        private static void LoadDefs<T>(string path, string xpath, List<T> loadedDefs, System.Func<XmlNode, T> loadAction, System.Action<T> addAction) where T : Def
        {
            if (!Directory.Exists(path))
            {
                return;
            }
            foreach (FileInfo file in new DirectoryInfo(path).GetFiles("*.xml"))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(file.FullName);
                foreach (XmlNode xmlNode in xml.SelectNodes(xpath))
                {
                    T def = loadAction(xmlNode);
                    if (def == null || def.defName.NullOrEmpty() || loadedDefs.Any(d => d.defName == def.defName))
                    {
                        continue;
                    }
                    addAction(def);
                    CQFQuestDefBootstrap.loadedDefs.Add(new LoadedDefInfo(def, file.FullName));
                }
            }
        }

        private static void ReplaceDef<T>(T def, T currentDef) where T : Def
        {
            if (currentDef != null && DefDatabase<T>.AllDefsListForReading.Contains(currentDef))
            {
                RemoveDef(currentDef);
            }
            T existingDef = DefDatabase<T>.GetNamedSilentFail(def.defName);
            if (existingDef != null)
            {
                RemoveDef(existingDef);
            }
            DefDatabase<T>.Add(def);
            ValidateDef(new LoadedDefInfo(def, "hot load"));
        }

        private static void RemoveDef<T>(T def) where T : Def
        {
            CQFQuestDefBootstrap.removeMethodCache.TryGetValue(typeof(T), out MethodInfo removeMethod);
            if (removeMethod == null)
            {
                removeMethod = typeof(DefDatabase<>).MakeGenericType(typeof(T)).GetMethod("Remove", BindingFlags.NonPublic | BindingFlags.Static);
                CQFQuestDefBootstrap.removeMethodCache[typeof(T)] = removeMethod;
            }
            removeMethod?.Invoke(null, [def]);
        }

        private static void ValidateDef(LoadedDefInfo loadedDef)
        {
            foreach (string error in loadedDef.def.ConfigErrors())
            {
                Log.Error($"CQF loaded def config error in {loadedDef.source}: {loadedDef.def.GetType().Name} '{loadedDef.def.defName}': {error}");
            }
        }

        private readonly struct LoadedDefInfo
        {
            public LoadedDefInfo(Def def, string source)
            {
                this.def = def;
                this.source = source;
            }

            public readonly Def def;
            public readonly string source;
        }

        private static readonly List<LoadedDefInfo> loadedDefs = new List<LoadedDefInfo>();
        private static readonly Dictionary<System.Type, MethodInfo> removeMethodCache = new Dictionary<System.Type, MethodInfo>();
    }
}
