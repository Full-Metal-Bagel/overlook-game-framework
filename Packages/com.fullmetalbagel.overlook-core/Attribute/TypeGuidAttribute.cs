using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Overlook;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
public sealed class TypeGuidAttribute : Attribute
{
    public Guid Id { get; }

    [SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
    public TypeGuidAttribute(string id)
    {
        Id = Guid.Parse(id);
    }
}

public static class TypeGuidUtils
{
    public static IReadOnlyDictionary<Guid, Type> IdTypeMap { get; }
    public static IReadOnlyDictionary<Type, Guid> TypeIdMap { get; }

    static TypeGuidUtils()
    {
        var systemGroupAndTypes =
            from assembly in AppDomain.CurrentDomain.GetAssemblies()
            where assembly.GetCustomAttributes<OverlookAssemblyAttribute>().Any()
            from type in assembly.GetTypes()
            from attribute in type.GetCustomAttributes<TypeGuidAttribute>()
            select (type, attribute)
        ;
        Dictionary<Guid, Type> idTypeMap = new();
        foreach (var item in systemGroupAndTypes)
        {
            if (idTypeMap.TryGetValue(item.attribute.Id, out var type))
            {
                Debug.LogError($"Duplicate type id: {type.FullName} {item.type} {item.attribute.Id}");
            }
            else
            {
                idTypeMap[item.attribute.Id] = item.type;
            }
        }

        IdTypeMap = idTypeMap;
        TypeIdMap = idTypeMap.ToDictionary(p => p.Value, p => p.Key);
    }
}
