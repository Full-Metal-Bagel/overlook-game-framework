using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using UnityEngine;

namespace Game;

public interface IObjectPool
{
    int RentedCount { get; }
    int PooledCount { get; }
    int MaxCount { get; set; }
    Func<int /* current count */, int /* target count */> ExpandFunc { get; set; }

    object Rent();
    void Recycle(object instance);
}

public interface IObjectPool<T> where T : class
{
    int RentedCount { get; }
    int PooledCount { get; }
    int MaxCount { get; set; }
    Func<int /* current count */, int /* target count */> ExpandFunc { get; set; }

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

    private int _maxCount;
    public int MaxCount
    {
        get => _maxCount;
        set => _maxCount = value;
    }

    private int _rentedCount;
    public int RentedCount => _rentedCount;

    public int PooledCount => _pool.Count;

    public Func<int /* current count */, int /* target count */> ExpandFunc { get; set; }

    private readonly Func<T> _createFunc;
    private readonly Action<T> _onRentAction;
    private readonly Action<T> _onRecycleAction;

    public ObjectPool(Func<T> createFunc, Action<T>? onRentAction = null, Action<T>? onRecycleAction = null, int initCount = 0, int maxCount = int.MaxValue, Func<int, int>? expandFunc = null)
    {
        Debug.Assert(initCount >= 0);
        Debug.Assert(maxCount >= 0);
        _createFunc = createFunc;
        _onRentAction = onRentAction ?? (_ => { });
        _onRecycleAction = onRecycleAction ?? (_ => { });
        MaxCount = maxCount;
        ExpandFunc = expandFunc ?? (x => x + 1);
        for (var i = 0; i < initCount; i++)
        {
            var instance = _createFunc();
            _pool.Enqueue(instance);
        }
    }

    object IObjectPool.Rent() => Rent();
    void IObjectPool.Recycle(object instance) => Recycle((T)instance);

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    public T Rent()
    {
        Debug.Assert(MaxCount >= 0, "pool had been disposed already");
        var currentCount = _rentedCount;
        Interlocked.Increment(ref _rentedCount);
        if (!_pool.TryDequeue(out var instance)) instance = Expand(currentCount);
#if KGP_DEBUG
        _trackers.Add(instance, new Tracker());
#endif
        _onRentAction(instance);
        if (instance is IObjectPoolCallback callback) callback.OnRent();
        return instance;
    }

    public void Recycle(T instance)
    {
#if KGP_DEBUG
        if (MaxCount < 0) Debug.LogWarning("pool had been disposed already");

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
        if (_pool.Count < MaxCount)
        {
            _pool.Enqueue(instance);
            _onRecycleAction(instance);
            if (instance is IObjectPoolCallback callback) callback.OnRecycle();
        }
        else if (instance is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private T Expand(int currentCount)
    {
        var returnInstance = _createFunc();
        var targetCount = Mathf.Clamp(ExpandFunc(currentCount), 1, MaxCount);
        // TODO: lock to avoid additional "Add" on multi-threaded scenario for precise control the max size of pool?
        for (var i = currentCount + 1; i < targetCount; i++)
        {
            var instance = _createFunc();
            _pool.Enqueue(instance);
        }
        return returnInstance;
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _maxCount, -1);
        while (_pool.TryDequeue(out var instance))
        {
            if (instance is IDisposable disposable)
                disposable.Dispose();
        }
    }

#if KGP_DEBUG
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
