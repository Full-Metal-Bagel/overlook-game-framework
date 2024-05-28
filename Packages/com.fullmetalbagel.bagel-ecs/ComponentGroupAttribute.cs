using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game;

namespace RelEcs
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ComponentGroupAttribute : Attribute
    {
        public Type GroupType { get; }
        public Type MemberType { get; }

        public ComponentGroupAttribute(Type groupType, Type memberType)
        {
            GroupType = groupType;
            MemberType = memberType;
        }
    }

    public static class ComponentGroups
    {
        public static IReadOnlyDictionary<Type/*group*/, IReadOnlyList<Type>/*members*/> Groups { get; }

        static ComponentGroups()
        {
            var groups = new Dictionary<Type, IReadOnlyList<Type>>(64);
            foreach (var grouping in AppDomain.CurrentDomain.GetAssemblies()
                 .SelectMany(assembly =>
                 {
                     try
                     {
                         return assembly.GetCustomAttributes<ComponentGroupAttribute>();
                     }
                     catch (Exception ex)
                     {
                         Debug.LogException(ex);
                         return Enumerable.Empty<ComponentGroupAttribute>();
                     }
                 })
                 .GroupBy(attribute => attribute.GroupType)
            )
            {
                groups[grouping.Key] = grouping
                    .Select(attribute => attribute.MemberType)
                    .Where(member => member.IsValueType) // TODO: warning? analyzer?
                    .ToArray()
                ;
            }
            Groups = groups;
        }
    }
}
