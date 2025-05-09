﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Overlook.Ecs;

[SuppressMessage("Design", "CA1040:Avoid empty interfaces")]
public interface ITaggedComponent
{
}

public interface ITaggedComponent<out T> : ITaggedComponent
{
    T Component { get; }
}

public static class TaggedComponentExtension
{
    public static void AddTaggedComponent<T>(this World world, Entity entity, [DisallowNull] T component, Type tagType) where T : class
    {
        var tagged = component.CreateTaggedComponent(tagType);
        // HACK: set tag as value type component?
        world.AddObjectComponent(entity, tagged);
    }

    public static object CreateTaggedComponent<T>([DisallowNull] this T component, Type tagType) where T : class
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        Debug.Assert(component is not ITaggedComponent, $"{tagType}: tag of tag is not supporting yet");
        Debug.Assert(typeof(ITaggedComponent).IsAssignableFrom(tagType), $"{tagType} must implement {typeof(ITaggedComponent)}<T>");
        // Debug.Assert(tagType is { IsGenericTypeDefinition: true, IsValueType: true }, $"{tagType} must be a `struct` with one and only one type parameter");
        // TODO: optimize by using a delegate creator instead of constructor?
        var concreteTagType = tagType.IsGenericType ? tagType.MakeGenericType(component.GetType()) : tagType;
        var ctor = concreteTagType.GetConstructor(new[] { component.GetType() });
        Debug.Assert(ctor != null, $"{tagType}:tagType t component={component.GetType()}");
        return ctor!.Invoke(new object[] { component });
    }

    public static bool TryGetObjectComponent<TComponent, TTag>(this World world, Entity entity, out TComponent? component, TTag _ = default!)
        where TComponent : class
        where TTag : class, ITaggedComponent<TComponent>
    {
        Debug.Assert(typeof(TTag) is { IsGenericType: true } && typeof(TTag).GetGenericArguments().Length == 1, $"{typeof(TTag)} must be a `struct` with one and only one type parameter");
        component = null;
        foreach (var (storageType, entityComponent) in world.Archetypes.GetObjectComponents(entity.Identity))
        {
            if (storageType.Type.IsTagTypeOf<TComponent>(typeof(TTag).GetGenericTypeDefinition()))
            {
                component = ((ITaggedComponent<TComponent>)entityComponent).Component;
                return true;
            }
        }
        return false;
    }

    public static T? FindUnwrappedComponent<T>(this World world, Entity entity, Type tagGenericDefinition) where T : class
    {
        foreach (var (storageType, components) in world.Archetypes.GetObjectComponents(entity.Identity))
        {
            if (storageType.Type.IsTagTypeOf<T>(tagGenericDefinition))
            {
                return ((ITaggedComponent<T>)components[0]).Component;
            }
        }
        return null;
    }

    [Pure]
    internal static bool IsTagTypeOf<T>(this Type concreteType, Type tagGenericTypeDefinition) where T : class
    {
        // HACK: tricky way to check "covariant" converting on the tag struct
        //       vote for proper covariant of struct here:
        //       https://github.com/dotnet/csharplang/discussions/2498
        if (!concreteType.IsGenericType) return concreteType == tagGenericTypeDefinition;
        return typeof(ITaggedComponent<T>).IsAssignableFrom(concreteType) &&
               concreteType.GetGenericTypeDefinition() == tagGenericTypeDefinition;
    }

    public static void FindUnwrappedComponents<T>(this World world, Entity entity, ICollection<T> resultComponents, Type tagGenericDefinition) where T : class
    {
        foreach (var (storageType, components) in world.Archetypes.GetObjectComponents(entity.Identity))
        {
            if (storageType.Type.IsTagTypeOf<T>(tagGenericDefinition))
            {
                foreach (ITaggedComponent<T> component in components)
                {
                    resultComponents.Add(component.Component);
                }
            }
        }
    }

    public static void RemoveComponentsIncludingTagged<T>(this World world, Entity entity) where T : class
    {
        var archetypes = world.Archetypes;
        using var types = new Pool.PooledList<Type>(32);
        foreach (var (storageType, _) in world.Archetypes.GetObjectComponents(entity.Identity))
        {
            var type = storageType.Type;
            types.Value.Add(type);
        }
        foreach (var type in types.Value)
        {
            if (typeof(T).IsAssignableFrom(type))
            {
                archetypes.RemoveComponent(entity.Identity, type);
            }
            else if (typeof(ITaggedComponent).IsAssignableFrom(type) && typeof(T).IsAssignableFrom(type.GetGenericArguments()[0]))
            {
                archetypes.RemoveComponent(entity.Identity, type);
            }
        }
    }
}
