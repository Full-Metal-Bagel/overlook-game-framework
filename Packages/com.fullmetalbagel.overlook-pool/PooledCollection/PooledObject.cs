namespace Overlook.Pool;

public readonly ref struct PooledObject<T> where T : class, new()
{
    public T Value { get; }

    public PooledObject()
    {
        Value = StaticPools<T>.Rent();
    }

    public static implicit operator T(PooledObject<T> self) => self.Value;

    public void Dispose()
    {
        StaticPools<T>.Recycle(Value);
    }
}
