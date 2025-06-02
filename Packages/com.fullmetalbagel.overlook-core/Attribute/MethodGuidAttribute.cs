using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Overlook;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class MethodGuidAttribute : Attribute
{
    public Guid Id { get; }
    public Type? DelegateType { get; init; } = null;

    [SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
    public MethodGuidAttribute(string id)
    {
        Id = Guid.Parse(id);
    }
}

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
public static class MethodGuidUtils
{
    public static IReadOnlyDictionary<Guid, MethodInfo> IdMethodMap { get; }
    public static IReadOnlyDictionary<Guid, Delegate> IdDelegateMap { get; }
    public static IReadOnlyDictionary<MethodInfo, Guid> MethodIdMap { get; }

    static MethodGuidUtils()
    {
        var methods =
            from assembly in AppDomain.CurrentDomain.GetAssemblies()
            where assembly.GetCustomAttributes<OverlookAssemblyAttribute>().Any()
            from type in assembly.GetTypes()
            where type.GetCustomAttributes<TypeGuidAttribute>().Any()
            from method in type.GetMethods()
            from attribute in method.GetCustomAttributes<MethodGuidAttribute>()
            select (method, attribute)
        ;
        Dictionary<Guid, Delegate> idDelegateMap = new();
        Dictionary<Guid, MethodInfo> idMethodMap = new();
        Dictionary<MethodInfo, Guid> methodIdMap = new();
        foreach (var (method, attribute) in methods)
        {
            if (!method.IsStatic)
            {
                Debug.Log($"only static methods are supported yet: {method.DeclaringType.FullName}.{method.Name}");
                continue;
            }

            idMethodMap.Add(attribute.Id, method);
            methodIdMap.Add(method, attribute.Id);
            if (attribute.DelegateType != null)
            {
                try
                {
                    var @delegate = method.CreateDelegate(attribute.DelegateType);
                    idDelegateMap.Add(attribute.Id, @delegate);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"invalid delegate: {method.Name}: {ex}");
                }
            }
        }

        IdDelegateMap = idDelegateMap;
        IdMethodMap = idMethodMap;
        MethodIdMap = methodIdMap;
    }

    public static Delegate GetMethod(Guid id)
    {
        return IdDelegateMap[id];
    }

    public static T GetMethod<T>(Guid id) where T : Delegate
    {
        var @delegate = IdDelegateMap[id];
        Debug.Assert(@delegate is T);
        return (T)@delegate;
    }

    public static IEnumerable<Guid> FindMethodIdByDelegate<T>() where T : Delegate
    {
        foreach (var (id, @delegate) in IdDelegateMap)
        {
            if (@delegate is T) yield return id;
        }
    }

    public static IEnumerable<MethodInfo> FindMethodInfoByDelegate<T>() where T : Delegate
    {
        return FindMethodIdByDelegate<T>().Select(id => IdMethodMap[id]);
    }
}
