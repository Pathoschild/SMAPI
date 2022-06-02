// This temporary utility fixes an esoteric issue in XNA Framework where deserialization depends on
// the order of fields returned by Type.GetFields, but that order changes after Harmony/MonoMod use
// reflection to access the fields due to an issue in .NET Framework.
// https://twitter.com/0x0ade/status/1414992316964687873
//
// This will be removed when Harmony/MonoMod are updated to incorporate the fix.
//
// Special thanks to 0x0ade for submitting this workaround! Copy/pasted and adapted from MonoMod.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

// ReSharper disable once CheckNamespace -- Temporary hotfix submitted by the MonoMod author.
namespace MonoMod.Utils;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Temporary hotfix submitted by the MonoMod author.")]
[SuppressMessage("ReSharper", "PossibleNullReferenceException", Justification = "Temporary hotfix submitted by the MonoMod author.")]
static class MiniMonoModHotfix
{
    // .NET Framework can break member ordering if using Module.Resolve* on certain members.

    private static readonly object[] _NoArgs = Array.Empty<object>();
    private static readonly object?[] _CacheGetterArgs = { /* MemberListType.All */ 0, /* name apparently always null? */ null };

    private static readonly Type? t_RuntimeType =
        typeof(Type).Assembly
            .GetType("System.RuntimeType");

    private static readonly PropertyInfo? p_RuntimeType_Cache =
        typeof(Type).Assembly
            .GetType("System.RuntimeType")
            ?.GetProperty("Cache", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly MethodInfo? m_RuntimeTypeCache_GetFieldList =
        typeof(Type).Assembly
            .GetType("System.RuntimeType+RuntimeTypeCache")
            ?.GetMethod("GetFieldList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly MethodInfo? m_RuntimeTypeCache_GetPropertyList =
        typeof(Type).Assembly
            .GetType("System.RuntimeType+RuntimeTypeCache")
            ?.GetMethod("GetPropertyList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly ConditionalWeakTable<Type, CacheFixEntry> _CacheFixed = new();

    public static void Apply()
    {
        var harmony = new Harmony("MiniMonoModHotfix");

        harmony.Patch(
            original: typeof(Harmony).Assembly
                .GetType("HarmonyLib.MethodBodyReader", throwOnError: true)!
                .GetMethod("ReadOperand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            transpiler: new HarmonyMethod(typeof(MiniMonoModHotfix), nameof(ResolveTokenFix))
        );

        harmony.Patch(
            original: typeof(MonoMod.Utils.ReflectionHelper).Assembly
                .GetType("MonoMod.Utils.DynamicMethodDefinition+<>c__DisplayClass3_0", throwOnError: true)!
                .GetMethod("<_CopyMethodToDefinition>g__ResolveTokenAs|1", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            transpiler: new HarmonyMethod(typeof(MiniMonoModHotfix), nameof(ResolveTokenFix))
        );

    }

    private static IEnumerable<CodeInstruction> ResolveTokenFix(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo getRealDeclaringType = typeof(MiniMonoModHotfix).GetMethod(nameof(MiniMonoModHotfix.GetRealDeclaringType)) ?? throw new InvalidOperationException($"Can't get required method {nameof(MiniMonoModHotfix)}.{nameof(GetRealDeclaringType)}");
        MethodInfo fixReflectionCache = typeof(MiniMonoModHotfix).GetMethod(nameof(MiniMonoModHotfix.FixReflectionCache)) ?? throw new InvalidOperationException($"Can't get required method {nameof(MiniMonoModHotfix)}.{nameof(FixReflectionCache)}");

        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;

            if (instruction.operand is MethodInfo called)
            {
                switch (called.Name)
                {
                    case "ResolveType":
                        // type.FixReflectionCache();
                        yield return new CodeInstruction(OpCodes.Dup);
                        yield return new CodeInstruction(OpCodes.Call, fixReflectionCache);
                        break;

                    case "ResolveMember":
                    case "ResolveMethod":
                    case "ResolveField":
                        // member.GetRealDeclaringType().FixReflectionCache();
                        yield return new CodeInstruction(OpCodes.Dup);
                        yield return new CodeInstruction(OpCodes.Call, getRealDeclaringType);
                        yield return new CodeInstruction(OpCodes.Call, fixReflectionCache);
                        break;
                }
            }
        }
    }

    public static Type? GetRealDeclaringType(this MemberInfo member)
    {
        return member.DeclaringType ?? member.Module.GetModuleType();
    }

    public static void FixReflectionCache(this Type? type)
    {
        if (t_RuntimeType == null || p_RuntimeType_Cache == null || m_RuntimeTypeCache_GetFieldList == null || m_RuntimeTypeCache_GetPropertyList == null)
            return;

        for (; type != null; type = type.DeclaringType)
        {
            // All types SHOULD inherit RuntimeType, including those built at runtime.
            // One might never know what awaits us in the depths of reflection hell though.
            if (!t_RuntimeType.IsInstanceOfType(type))
                continue;

            CacheFixEntry entry = _CacheFixed.GetValue(type, rt =>
            {
                // All RuntimeTypes MUST have a cache, the getter is non-virtual, it creates on demand and asserts non-null.
                object cache = MiniMonoModHotfix.p_RuntimeType_Cache.GetValue(rt, MiniMonoModHotfix._NoArgs)!;
                Array properties = MiniMonoModHotfix._GetArray(cache, MiniMonoModHotfix.m_RuntimeTypeCache_GetPropertyList);
                Array fields = MiniMonoModHotfix._GetArray(cache, MiniMonoModHotfix.m_RuntimeTypeCache_GetFieldList);

                _FixReflectionCacheOrder<PropertyInfo>(properties);
                _FixReflectionCacheOrder<FieldInfo>(fields);

                return new CacheFixEntry(cache, properties, fields, needsVerify: false);
            });

            if (entry.NeedsVerify && !_Verify(entry, type))
            {
                lock (entry)
                {
                    _FixReflectionCacheOrder<PropertyInfo>(entry.Properties);
                    _FixReflectionCacheOrder<FieldInfo>(entry.Fields);
                }
            }

            entry.NeedsVerify = true;
        }
    }

    private static bool _Verify(CacheFixEntry entry, Type type)
    {
        // The cache can sometimes be invalidated.
        // TODO: Figure out if only the arrays get replaced or if the entire cache object gets replaced!
        object cache = p_RuntimeType_Cache!.GetValue(type, _NoArgs)!;
        if (entry.Cache != cache)
        {
            entry.Cache = cache;
            entry.Properties = _GetArray(cache, m_RuntimeTypeCache_GetPropertyList!);
            entry.Fields = _GetArray(cache, m_RuntimeTypeCache_GetFieldList!);
            return false;

        }

        Array properties = _GetArray(cache, m_RuntimeTypeCache_GetPropertyList!);
        if (entry.Properties != properties)
        {
            entry.Properties = properties;
            entry.Fields = _GetArray(cache, m_RuntimeTypeCache_GetFieldList!);
            return false;
        }

        Array fields = _GetArray(cache, m_RuntimeTypeCache_GetFieldList!);
        if (entry.Fields != fields)
        {
            entry.Fields = fields;
            return false;

        }

        // Cache should still be the same, no re-fix necessary.
        return true;
    }

    private static Array _GetArray(object cache, MethodInfo getter)
    {
        // Get and discard once, otherwise we might not be getting the actual backing array.
        getter.Invoke(cache, _CacheGetterArgs);
        return (Array)getter.Invoke(cache, _CacheGetterArgs)!;
    }

    private static void _FixReflectionCacheOrder<T>(Array orig) where T : MemberInfo
    {
        // Sort using a short-lived list.
        List<T> list = new List<T>(orig.Length);
        for (int i = 0; i < orig.Length; i++)
            list.Add((T)orig.GetValue(i)!);

        list.Sort((a, b) => a.MetadataToken - b.MetadataToken);

        for (int i = orig.Length - 1; i >= 0; --i)
            orig.SetValue(list[i], i);
    }

    private class CacheFixEntry
    {
        public object? Cache;
        public Array Properties;
        public Array Fields;
        public bool NeedsVerify;

        public CacheFixEntry(object? cache, Array properties, Array fields, bool needsVerify)
        {
            this.Cache = cache;
            this.Properties = properties;
            this.Fields = fields;
            this.NeedsVerify = needsVerify;
        }
    }
}
