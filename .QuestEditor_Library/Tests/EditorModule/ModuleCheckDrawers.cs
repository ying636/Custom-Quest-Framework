using UnityEngine;

internal static class ModuleCheckDrawers
{
    public static int RefValue(object instance, ref float value)
    {
        value += 7f;
        return (int)value;
    }

    public static string GenericValue<T>(T value)
    {
        return typeof(T).Name + ":" + value;
    }

    public static void FailingValue()
    {
        throw new InvalidDataException("CQF_Check_Error");
    }
}
