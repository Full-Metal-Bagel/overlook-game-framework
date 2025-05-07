using System;

namespace Overlook.Pool;

public interface IObjectPoolPolicy
{
    object Create();
    public int InitCount => 0;
    public int MaxCount => int.MaxValue >> 2;
    public int Expand(int size) => size * 2;
    void OnRent(object instance) => (instance as IObjectPoolCallback)?.OnRent();
    void OnRecycle(object instance) => (instance as IObjectPoolCallback)?.OnRecycle();
    void OnDispose(object instance) => (instance as IDisposable)?.Dispose();
}
