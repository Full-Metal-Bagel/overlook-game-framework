using System;
using OneShot;

namespace Overlook.System;

/// <summary>
/// Factory for creating systems programmatically from a Type.
/// Use this when you want to register systems directly in code
/// </summary>
public sealed record SystemFactory(Type SystemType, byte TickStage = 0, int TickTimes = -1, string Name = "", bool Enable = true) : ISystemFactory
{
    public string SystemName => string.IsNullOrWhiteSpace(Name) ? SystemType.Name : Name;
    public void Dispose() { }

    public ISystem Resolve(Container container, int systemIndex)
    {
        var systemContainer = container.CreateChildContainer();
        systemContainer.Register(SystemType).With(systemIndex).As<ISystem>();
        return systemContainer.Resolve<ISystem>();
    }
}

/// <summary>
/// Factory for creating systems programmatically from a Type.
/// Use this when you want to register systems directly in code
/// </summary>
public sealed record SystemFactory<T>(byte TickStage = 0, int TickTimes = -1, string Name = "", bool Enable = true) : ISystemFactory
    where T : ISystem
{
    public string SystemName => string.IsNullOrWhiteSpace(Name) ? typeof(T).Name : Name;
    public void Dispose() { }

    public ISystem Resolve(Container container, int systemIndex)
    {
        var systemContainer = container.CreateChildContainer();
        systemContainer.Register<T>().With(systemIndex).As<ISystem>();
        return systemContainer.Resolve<ISystem>();
    }
}
