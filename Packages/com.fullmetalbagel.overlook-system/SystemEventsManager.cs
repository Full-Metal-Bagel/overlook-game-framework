#nullable enable

using System;
using System.Collections.Generic;

namespace Overlook.System;

public sealed class SystemEventsManager
{
    private readonly Dictionary<Type, ISystemEvents> _typeEventsMap = new();

    public void Append<T>(T @event, int lastingFrames = 1) where T : unmanaged
    {
        GetOrCreateSystemEvents<T>().Append(@event, lastingFrames);
    }

    public SystemEvents<T> GetOrCreateSystemEvents<T>() where T : unmanaged
    {
        if (!_typeEventsMap.TryGetValue(typeof(T), out var events))
        {
            events = new SystemEvents<T>();
            _typeEventsMap[typeof(T)] = events;
        }
        return (SystemEvents<T>)events;
    }

    public void Tick(int systemIndex, int currentFrame)
    {
        foreach (var events in _typeEventsMap.Values) events.Tick(systemIndex, currentFrame);
    }
}
