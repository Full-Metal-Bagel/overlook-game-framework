using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Overlook.Ecs;

[SuppressMessage("Design", "CA1040:Avoid empty interfaces")]
public interface IFixedSize { }

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct Fixed8Bytes : IFixedSize
{
    [FieldOffset(0)] public unsafe fixed byte Value[8];
}
