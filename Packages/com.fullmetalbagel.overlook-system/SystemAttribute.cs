#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Game
{
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(IGameSystem))]
#pragma warning disable CA1711
    public sealed class GameSystemAttribute : PreserveAttribute
#pragma warning restore CA1711
    {
        public Guid Id { get; }
#pragma warning disable CA1019
        public GameSystemAttribute(string id)
#pragma warning restore CA1019
        {
            Id = Guid.Parse(id);
        }
    }

    public static class SystemsUtils
    {
        public static readonly IReadOnlyDictionary<Guid, (Type type, GameSystemAttribute attribute)> IdTypeAttributeMap;
        public static readonly IReadOnlyDictionary<Type, Guid> TypeIdMap;

#pragma warning disable CA1810
        static SystemsUtils()
#pragma warning restore CA1810
        {
            var systemGroupAndTypes =
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                from attribute in type.GetCustomAttributes<GameSystemAttribute>()
                select (type, attribute)
            ;
            Dictionary<Guid, (Type type, GameSystemAttribute attribute)> dict = new Dictionary<Guid, (Type type, GameSystemAttribute attribute)>();
            foreach (var item in systemGroupAndTypes)
            {
                if (dict.ContainsKey(item.attribute.Id))
                {
                    UnityEngine.Debug.Log($"Duplicate system id: {item.attribute.Id}");
                }
                else
                {
                    dict[item.attribute.Id] = item;
                }
            }
            IdTypeAttributeMap = dict;
            TypeIdMap = IdTypeAttributeMap.ToDictionary(t => t.Value.type, t => t.Key);
        }
    }
}
