using OneShot;

namespace Overlook.System;

public sealed record InstanceSystemFactory(ISystem System, byte TickStage = 0, int TickTimes = -1, string Name = "", bool Enable = true) : ISystemFactory
{
    public string SystemName => string.IsNullOrWhiteSpace(Name) ? System.GetType().Name : Name;
    public void Dispose() { }
    public ISystem Resolve(Container container, int systemIndex) => System;
}
