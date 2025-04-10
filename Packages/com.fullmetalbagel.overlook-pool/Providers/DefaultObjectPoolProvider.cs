namespace Overlook.Pool;

public sealed class DefaultObjectPoolProvider<T> : IObjectPoolProvider where T : class, new()
{
    public IObjectPool CreatePool() => new ObjectPool<T, Policy>();

    private readonly record struct Policy : IObjectPoolPolicy
    {
        public object Create() => new T();
    }
}
