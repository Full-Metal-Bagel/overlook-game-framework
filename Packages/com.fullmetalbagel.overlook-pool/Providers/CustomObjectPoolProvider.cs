namespace Overlook.Pool;

public sealed class CustomObjectPoolProvider<T, TPolicy> : IObjectPoolProvider
    where T : class
    where TPolicy : unmanaged, IObjectPoolPolicy
{
    public IObjectPool CreatePool() => new ObjectPool<T, TPolicy>();
}
