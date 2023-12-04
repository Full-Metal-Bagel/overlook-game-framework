#nullable enable

using System;

namespace Game
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class SystemEventAttribute : Attribute
    {
        public int InitCapacity { get; set; }
    }
}
