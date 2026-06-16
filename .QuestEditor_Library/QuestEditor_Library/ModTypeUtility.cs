using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace QuestEditor_Library
{
    public static class ModTypeUtility
    {
        public static string GetModName(Type type, string? fallback = null)
        {
            if (type == null)
            {
                return fallback ?? "Unknown";
            }
            return GetModName(type.Assembly, fallback ?? type.Assembly.GetName().Name ?? type.Name);
        }

        public static string GetModName(Assembly assembly, string? fallback = null)
        {
            if (assembly == null)
            {
                return fallback ?? "Unknown";
            }
            if (TryGetModContentPack(assembly, out ModContentPack? mod))
            {
                return mod.Name ?? fallback ?? assembly.GetName().Name ?? "Unknown";
            }
            string result = fallback ?? assembly.GetName().Name ?? "Unknown";
            return result;
        }

        public static string GetModName(Def def, string? fallback = null)
        {
            if (def == null)
            {
                return fallback ?? "Unknown";
            }
            return def.modContentPack?.Name ?? fallback ?? "Unknown";
        }

        public static bool TryGetModContentPack(Type type, out ModContentPack? mod)
        {
            mod = null;
            return type != null && TryGetModContentPack(type.Assembly, out mod);
        }

        public static bool IsCQFType(Type type)
        {
            return TryGetModContentPack(type, out ModContentPack? mod) && IsCQFMod(mod);
        }

        public static bool IsCQFMod(ModContentPack mod)
        {
            return mod != null && mod.PackageIdPlayerFacing == CQFPackageId;
        }

        public static bool IsCQFDef(Def def)
        {
            return def != null && IsCQFMod(def.modContentPack);
        }

        public static bool TryGetModContentPack(Assembly assembly, out ModContentPack? mod)
        {
            mod = null;
            if (assembly == null)
            {
                return false;
            }
            if (assemblyModCache.TryGetValue(assembly, out mod))
            {
                return mod != null;
            }
            if (noModAssemblyCache.Contains(assembly))
            {
                return false;
            }
            foreach (ModContentPack loadedMod in LoadedModManager.RunningModsListForReading)
            {
                if (loadedMod.assemblies?.loadedAssemblies?.Contains(assembly) ?? false)
                {
                    assemblyModCache[assembly] = loadedMod;
                    mod = loadedMod;
                    return true;
                }
            }
            noModAssemblyCache.Add(assembly);
            return false;
        }

        private static readonly Dictionary<Assembly, ModContentPack> assemblyModCache = new Dictionary<Assembly, ModContentPack>();

        private static readonly HashSet<Assembly> noModAssemblyCache = new HashSet<Assembly>();

        private const string CQFPackageId = "HaiLuan.CustomQuestFramework";
    }
}
