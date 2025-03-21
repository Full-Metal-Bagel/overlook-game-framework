#nullable enable

using System.Runtime.InteropServices;

namespace RelEcs;

public interface IFixedSize { }

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct Fixed8Bytes : IFixedSize
{
    [FieldOffset(0)] public unsafe fixed byte Value[8];
}
