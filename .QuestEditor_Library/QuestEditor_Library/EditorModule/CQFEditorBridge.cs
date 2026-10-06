using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFEditorBridge
    {
        public static bool IsLoaded => module != null;
        public static ICQFEditorModule? Module => module;

        public static void Register(string key, Type drawer, string methodName)
        {
            MethodInfo method = drawer.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException(drawer.FullName, methodName);
            methods.Add(key, method);
        }

        public static void Attach(ICQFEditorModule value)
        {
            if (module != null) throw new InvalidOperationException("CQF editor already initialized.");
            if (value.ApiVersion != ApiVersion) throw new InvalidOperationException("CQF_Editor_VersionMismatch");
            try
            {
                value.Initialize();
                module = value;
            }
            catch
            {
                methods.Clear();
                throw;
            }
        }

        public static object? Invoke(string key, object? instance, object?[] arguments, Type[]? genericTypes = null)
        {
            if (module == null) throw new InvalidOperationException("CQF_Editor_NotLoaded".Translate());
            if (!methods.TryGetValue(key, out MethodInfo method)) throw new MissingMethodException("CQF editor: " + key);
            if (genericTypes != null) method = method.MakeGenericMethod(genericTypes);
            int offset = instance == null ? 0 : 1;
            object?[] values = new object?[arguments.Length + offset];
            if (offset == 1) values[0] = instance;
            Array.Copy(arguments, 0, values, offset, arguments.Length);
            try
            {
                return method.Invoke(null, values);
            }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
            finally
            {
                Array.Copy(values, offset, arguments, 0, arguments.Length);
            }
        }

        public const int ApiVersion = 1;
        private static ICQFEditorModule? module;
        private static readonly Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
    }
}
