namespace Overlook.Pool;

public sealed class DefaultObjectPoolProvider<T> : IObjectPoolProvider where T : class, new()
{
    public IObjectPool CreatePool() => new ObjectPool<T, DefaultObjectPoolPolicy<T>>();
}

public readonly record struct DefaultObjectPoolPolicy<T> : IObjectPoolPolicy where T : class, new()
{
    public object Create() => new T();
}
