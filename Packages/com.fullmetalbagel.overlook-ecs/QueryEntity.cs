using System;

namespace Overlook.Ecs;

public interface IQueryEntity
{
    public bool Has<T>();
    public bool Has(Type type);
    public ref T Get<T>() where T : unmanaged;
    public T GetObject<T>() where T : class;
}

public readonly struct QueryEntity : IQueryEntity
{
    public Entity Entity { get; init; }
    [OptionalOnInit] public Query Query { get; init; }

    public bool Has<T>()
    {
        return Query.Has<T>(Entity);
    }

    public bool Has(Type type)
    {
        return Query.Has(Entity, type);
    }

    public ref T Get<T>() where T : unmanaged
    {
        return ref Query.Get<T>(Entity);
    }

    public T GetObject<T>() where T : class
    {
        return Query.GetObject<T>(Entity);
    }

    public static implicit operator Entity(QueryEntity self) => self.Entity;
}