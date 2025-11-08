#nullable enable
using OneShot;

namespace Overlook.System;

public interface ISystemFactory
{
    string SystemName { get; }
    bool Enable { get; }
    byte TickStage { get; }
    int TickTimes { get; }
    // TODO: Consider adding ResolveAsync method in the future to support async asset loading
    ISystem Resolve(Container container, int systemIndex);
}
