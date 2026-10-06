using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using Verse;

internal static class AICheckFixtures
{
    public static CustomMapDataDef Map()
    {
        CustomMapDataDef map = (CustomMapDataDef)RuntimeHelpers.GetUninitializedObject(typeof(CustomMapDataDef));
        foreach (FieldInfo field in typeof(CustomMapDataDef).GetFields(BindingFlags.Public | BindingFlags.Instance))
            if (!field.IsInitOnly && (typeof(IList).IsAssignableFrom(field.FieldType) || typeof(IDictionary).IsAssignableFrom(field.FieldType)))
                field.SetValue(map, Activator.CreateInstance(field.FieldType));
        map.defName = "CQF_Check_Map";
        map.size = new IntVec3(64, 1, 64);
        return map;
    }
}
