using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Overlook.Pool;

public interface IObjectPool
{
    IObjectPoolPolicy Policy { get; }
    int RentedCount { get; }
    int PooledCount { get; }
    object Rent();
    void Recycle(object instance);
}

public interface IObjectPool<T> where T : class
{
    IObjectPoolPolicy Policy { get; }
    int RentedCount { get; }
    int PooledCount { get; }
    T Rent();
    void Recycle(T instance);
}

public interface IObjectPoolCallback
{
    void OnRent();
    void OnRecycle();
}

public sealed class ObjectPool<T> : IObjectPool, IObjectPool<T>, IDisposable where T : class
{
    private readonly ConcurrentQueue<T> _pool = new();

    private int _rentedCount;
    public int RentedCount => _rentedCount;
    public int PooledCount => _pool.Count;
    public IObjectPoolPolicy Policy { get; }

    public ObjectPool(IObjectPoolPolicy policy)
    {
        Debug.Assert(policy.InitCount >= 0);
        Debug.Assert(policy.MaxCount >= 0);
        Policy = policy;
        for (var i = 0; i < policy.InitCount; i++)
        {
            var instance = policy.Create();
            _pool.Enqueue((T)instance);
        }
    }

    object IObjectPool.Rent() => Rent();
    void IObjectPool.Recycle(object instance) => Recycle((T)instance);

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    public T Rent()
    {
        Debug.Assert(Policy.MaxCount >= 0, "pool had been disposed already");
        var currentCount = _rentedCount;
        Interlocked.Increment(ref _rentedCount);
        if (!_pool.TryDequeue(out var instance)) instance = Expand(currentCount);
#if OVERLOOK_DEBUG
        _trackers.Add(instance, new Tracker());
#endif
        if (instance is IObjectPoolCallback callback) callback.OnRent();
        return instance;
    }

    public void Recycle(T instance)
    {
#if OVERLOOK_DEBUG
        if (Policy.MaxCount < 0) Debug.LogWarning("pool had been disposed already");

        if (_trackers.TryGetValue(instance, out var tracker))
        {
            _trackers.Remove(instance);
            tracker.Dispose();
        }
        else
        {
            Debug.LogError($"{instance.GetType().Name} has already been recycled, or not rent from this pool", instance as UnityEngine.Object);
            return;
        }
#endif
        Interlocked.Decrement(ref _rentedCount);
        // TODO: lock to avoid additional "Add" on multi-threaded scenario for precise control the max size of pool?
        if (_pool.Count < Policy.MaxCount)
        {
            _pool.Enqueue(instance);
            if (instance is IObjectPoolCallback callback) callback.OnRecycle();
        }
        else if (instance is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private T Expand(int currentCount)
    {
        var returnInstance = (T)Policy.Create();
        var targetCount = Math.Clamp(Policy.Expand(currentCount), 1, Policy.MaxCount);
        // TODO: lock to avoid additional "Add" on multi-threaded scenario for precise control the max size of pool?
        for (var i = currentCount + 1; i < targetCount; i++)
        {
            var instance = Policy.Create();
            _pool.Enqueue((T)instance);
        }
        return returnInstance;
    }

    public void Dispose()
    {
        while (_pool.TryDequeue(out var instance))
        {
            if (instance is IDisposable disposable)
                disposable.Dispose();
        }
    }

#if OVERLOOK_DEBUG
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<T, Tracker> _trackers = new();

    // https://github.com/dotnet/aspnetcore/blob/main/src/ObjectPool/src/LeakTrackingObjectPool.cs
    private sealed class Tracker : IDisposable
    {
        private readonly StackTrace _stack = new(skipFrames: 2, fNeedFileInfo: true);
        private bool _disposed;
        private Guid _id = Guid.NewGuid();

        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public void PrintLeakInfo()
        {
            Debug.LogWarning($"{typeof(T).Name}({_id}) was leaked. Created at: {Environment.NewLine}{_stack}");
        }

        ~Tracker()
        {
            if (!_disposed && !Environment.HasShutdownStarted)
            {
                PrintLeakInfo();
            }
        }
    }
#endif
}
