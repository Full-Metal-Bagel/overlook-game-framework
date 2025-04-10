using System;
using System.Collections.Generic;

namespace Overlook.Pool;

public interface IObjectPoolProviderAttribute
{
    IObjectPoolProvider? CreateProvider(Type type);
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class OverridePoolPolicyAttribute<T, TPolicy> : Attribute, IObjectPoolProviderAttribute
    where T : class
    where TPolicy : unmanaged, IObjectPoolPolicy
{
    public IObjectPoolProvider CreateProvider(Type type)
    {
        return new CustomObjectPoolProvider<T, TPolicy>();
    }
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RegisterCollectionPoolPolicyAttribute : Attribute, IObjectPoolProviderAttribute
    where T : class
    where TPolicy : unmanaged, IObjectPoolPolicy
{
    public IObjectPoolProvider CreateProvider(Type type)
    {
        type.GetInterface(nameof(ICollection<>));
        typeof(DefaultCollectionPoolProvider<,>)
        return ;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class OverridePoolPolicyAttribute<TPolicy> : Attribute, IObjectPoolProviderAttribute
    where TPolicy : unmanaged, IObjectPoolPolicy
{
    public IObjectPoolProvider CreateProvider(Type type)
    {
        var providerType = typeof(CustomObjectPoolProvider<,>).MakeGenericType(type, typeof(TPolicy));
        return (IObjectPoolProvider)Activator.CreateInstance(providerType);
    }
}
