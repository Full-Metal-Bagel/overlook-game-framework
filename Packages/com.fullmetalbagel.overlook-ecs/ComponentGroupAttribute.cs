using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Overlook.Ecs;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ComponentGroupAttribute : Attribute
{
    public Type GroupType { get; }
    public Type MemberType { get; }
    public bool CreateInstance { get; set; } = true;

    public ComponentGroupAttribute(Type groupType, Type memberType)
    {
        GroupType = groupType;
        MemberType = memberType;
    }
}

public static class ComponentGroups
{
    public static IReadOnlyDictionary<Type/*group*/, IReadOnlyList<(Type memberType, bool createInstance)>/*members*/> Groups { get; }

    static ComponentGroups()
    {
        var groups = new Dictionary<Type, IReadOnlyList<(Type memberType, bool createInstance)>>(64);
        foreach (var grouping in AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(assembly =>
                     {
                         try
                         {
                             return assembly.GetCustomAttributes<ComponentGroupAttribute>();
                         }
                         catch (ArgumentNullException ex)
                         {
                             Debug.LogException(ex);
                             return Enumerable.Empty<ComponentGroupAttribute>();
                         }
                     })
                     .GroupBy(attribute => attribute.GroupType)
                )
        {
            groups[grouping.Key] = grouping
                    .Select(attribute => (attribute.MemberType, attribute.CreateInstance))
                    .ToArray()
                ;
        }
        Groups = groups;
    }
}
