using System;
using System.Collections.Generic;
using System.Linq;

namespace Overlook.Pool;

public interface IObjectPoolProviderFactory
{
    IObjectPoolProvider? CreateProvider(Type type);
}

public interface IAssemblyObjectPoolProviderFactory : IObjectPoolProviderFactory
{
    int Priority { get; }
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class OverridePoolPolicyAttribute<T, TPolicy> : Attribute, IAssemblyObjectPoolProviderFactory
    where T : class
    where TPolicy : unmanaged, IObjectPoolPolicy
{
    public IObjectPoolProvider? CreateProvider(Type type)
    {
        return type == typeof(T) ? new CustomObjectPoolProvider<T, TPolicy>() : null;
    }

    public int Priority => 0;
}

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class OverrideGenericCollectionPoolPolicyAttribute<TPolicy> : Attribute, IAssemblyObjectPoolProviderFactory
    where TPolicy : unmanaged, IObjectPoolPolicy
{
    public Type GenericCollectionType { get; }

    public OverrideGenericCollectionPoolPolicyAttribute(Type genericCollectionType)
    {
        GenericCollectionType = genericCollectionType;
    }

    public IObjectPoolProvider? CreateProvider(Type type)
    {
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != GenericCollectionType) return null;
        var collectionType = type.GetInterfaces().SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
        if (collectionType == null) return null;

        var elementType = collectionType.GetGenericArguments()[0];
        var providerType = typeof(GenericCollectionPoolProvider<,,>).MakeGenericType(type, elementType, typeof(TPolicy));
        return (IObjectPoolProvider)Activator.CreateInstance(providerType);
    }

    public int Priority => 100;
}

[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class RegisterDefaultCollectionPoolAttribute : Attribute, IAssemblyObjectPoolProviderFactory
{
    public IObjectPoolProvider? CreateProvider(Type type)
    {
        var collectionType = type.GetInterfaces().SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
        if (collectionType == null) return null;

        var elementType = collectionType.GetGenericArguments()[0];
        var providerType = typeof(DefaultCollectionPoolProvider<,>).MakeGenericType(type, elementType);
        return (IObjectPoolProvider)Activator.CreateInstance(providerType);
    }

    public int Priority => 1000;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class OverridePoolPolicyAttribute<TPolicy> : Attribute, IObjectPoolProviderFactory
    where TPolicy : unmanaged, IObjectPoolPolicy
{
    public IObjectPoolProvider CreateProvider(Type type)
    {
        var providerType = typeof(CustomObjectPoolProvider<,>).MakeGenericType(type, typeof(TPolicy));
        return (IObjectPoolProvider)Activator.CreateInstance(providerType);
    }
}
