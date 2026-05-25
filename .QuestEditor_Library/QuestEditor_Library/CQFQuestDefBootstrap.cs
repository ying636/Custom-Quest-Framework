using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static void LoadAll()
        {
            string questPath = Page_QuestEditor.Path;
            if (questPath.NullOrEmpty())
            {
                return;
            }
            LoadDefs(questPath, "//QuestScriptDef", DefDatabase<QuestScriptDef>.AllDefsListForReading, node => DirectXmlToObject.ObjectFromXml<QuestScriptDef>(node, false), def => DefDatabase<QuestScriptDef>.Add(def));
            LoadDefs(questPath + @"\Map", "//QuestEditor_Library.CustomMapDataDef", DefDatabase<CustomMapDataDef>.AllDefsListForReading, node => DirectXmlToObject.ObjectFromXml<CustomMapDataDef>(node, false), def => DefDatabase<CustomMapDataDef>.Add(def));
            LoadDefs(questPath + @"\DialogTree", "//QuestEditor_Library.DialogTreeDef", DefDatabase<DialogTreeDef>.AllDefsListForReading, node => DirectXmlToObject.ObjectFromXml<DialogTreeDef>(node, false), def => DefDatabase<DialogTreeDef>.Add(def));
            LoadDefs(questPath + @"\DialogTree", "//QuestEditor_Library.DialogManagerDef", DefDatabase<DialogManagerDef>.AllDefsListForReading, node => DirectXmlToObject.ObjectFromXml<DialogManagerDef>(node, false), def => DefDatabase<DialogManagerDef>.Add(def));
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
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
                }
            }
        }
    }
}
