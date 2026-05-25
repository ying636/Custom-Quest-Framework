using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using Verse.AI.Group;
using System.Xml.Linq;
using System.Reflection;

namespace QuestEditor_Library
{
    [HarmonyPatch(typeof(DirectXmlSaver), "XElementFromObject",new Type[] {typeof(object), typeof(Type), typeof(string), typeof(FieldInfo), typeof(bool) })]
    public class Patch_SaveXml
    {
        [HarmonyPostfix]
        public static void PostFix(object obj, Type expectedType, string nodeName,ref XElement __result) 
        {
            if (obj is IntVec3 pos)
            {
                __result = new XElement(nodeName, $"({pos.x},{pos.y},{pos.z})");
            }
        }
        [HarmonyPrefix]
        public static bool PreFix(object obj, Type expectedType, string nodeName, ref XElement __result)
        {
            if (obj is ISaveable save)
            {
                __result = save.SaveToXElement(nodeName);
                return false;
            }
            return true;
        }
    }
}
