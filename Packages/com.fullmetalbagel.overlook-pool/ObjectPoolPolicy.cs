namespace Overlook.Pool;

public interface IObjectPoolPolicy
{
    int InitCount { get; }
    int MaxCount { get; }
    int Expand(int size);
    object Create();
}

public sealed class DefaultObjectPoolPolicy<T> : IObjectPoolPolicy where T : class, new()
{
    public int InitCount => 0;
    public int MaxCount => int.MaxValue;
    public int Expand(int size) => size * 2;
    public object Create() => new();
}
