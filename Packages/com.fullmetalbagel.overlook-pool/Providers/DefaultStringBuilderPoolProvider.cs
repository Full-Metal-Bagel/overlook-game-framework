using System.Text;

namespace Overlook.Pool;

public sealed class DefaultStringBuilderPoolProvider : IObjectPoolProvider
{
    public IObjectPool CreatePool() => new ObjectPool<StringBuilder, Policy>();

    private readonly record struct Policy : IObjectPoolPolicy
    {
        public object Create() => new StringBuilder();
        public void OnRecycle(object instance) => ((StringBuilder)instance).Clear();
        public void OnDispose(object instance) => ((StringBuilder)instance).Clear();
    }
}
