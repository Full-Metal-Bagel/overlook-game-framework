using System;
using System.Diagnostics.CodeAnalysis;
using Sirenix.OdinInspector;

namespace Overlook.System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class SystemPropertyAttribute : PropertyGroupAttribute
{
    public Type System { get; }

    [SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
    public SystemPropertyAttribute(Type system, float order = 0.0f) : base(system.Name, order)
    {
        System = system;
    }
}
