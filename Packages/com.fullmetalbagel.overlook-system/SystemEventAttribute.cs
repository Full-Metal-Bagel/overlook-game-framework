using System;

namespace Overlook.System;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public sealed class SystemEventAttribute : Attribute
{
    public int InitCapacity { get; set; }
}
