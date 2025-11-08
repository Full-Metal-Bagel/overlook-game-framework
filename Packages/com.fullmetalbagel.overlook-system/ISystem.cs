#nullable enable

namespace Overlook.System;

public interface ISystem
{
    void Tick();
}

public sealed class EmptySystem : ISystem
{
    public static EmptySystem Instance { get; } = new();
    public void Tick() { }
}
