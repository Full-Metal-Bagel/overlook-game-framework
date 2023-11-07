#pragma warning disable CS0618 // Type or member is obsolete

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RelEcs
{
    [Obsolete("use `ITaggedComponent<T>` instead")]
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
        public static void AddTaggedComponent<T>(this World world, Entity entity, [DisallowNull] T component, Type tagType)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            Debug.Assert(component is not ITaggedComponent, $"{tagType}: tag of tag is not supporting yet");
            Debug.Assert(typeof(ITaggedComponent).IsAssignableFrom(tagType), $"{tagType} must implement {typeof(ITaggedComponent)}<T>");
            Debug.Assert(tagType is { IsGenericTypeDefinition: true, IsValueType: true }, $"{tagType} must be a `struct` with one and only one type parameter");
            // TODO: optimize by using a delegate creator instead of constructor?
            var concreteTagType = tagType.MakeGenericType(component.GetType());
            var ctor = concreteTagType.GetConstructor(new[] { component.GetType() });
            Debug.Assert(ctor != null);
            var tag = ctor.Invoke(new object[] { component });
            // HACK: set tag as object component?
            world.Archetypes.AddObjectComponent(entity.Identity, tag);
        }

        public static bool TryGetObjectComponent<TComponent, TTag>(this World world, Entity entity, out TComponent? component, TTag _ = default)
            where TComponent : class
            where TTag : struct, ITaggedComponent<TComponent>
        {
            Debug.Assert(typeof(TTag) is { IsGenericType: true } && typeof(TTag).GetGenericArguments().Length == 1, $"{typeof(TTag)} must be a `struct` with one and only one type parameter");
            var archetypes = world.Archetypes;
            var meta = archetypes._meta[entity.Identity.Id];
            var table = archetypes._tables[meta.TableId];

            component = null;

            foreach (var (storageType, storage) in table)
            {
                var type = storageType.Type;
                // HACK: tricky way to check "covariant" converting on the tag struct
                //       vote for proper covariant of struct here:
                //       https://github.com/dotnet/csharplang/discussions/2498
                if (type.IsGenericType &&
                    typeof(ITaggedComponent<TComponent>).IsAssignableFrom(type) &&
                    type.GetGenericTypeDefinition() == typeof(TTag).GetGenericTypeDefinition())
                {
                    // TODO: how to avoid boxing for accessing tag `struct`?
                    component = ((ITaggedComponent<TComponent>)storage.GetValue(meta.Row)).Component;
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<T> FindUnwrappedComponents<T>(this World world, Entity entity)
        {
            var archetypes = world.Archetypes;
            var meta = archetypes._meta[entity.Identity.Id];
            var table = archetypes._tables[meta.TableId];

            foreach (var (storageType, storage) in table)
            {
                var type = storageType.Type;
                if (typeof(T).IsAssignableFrom(type))
                {
                    yield return ((T[])storage)[meta.Row];
                }
                else if (typeof(ITaggedComponent).IsAssignableFrom(type) && typeof(T).IsAssignableFrom(type.GetGenericArguments()[0]))
                {
                    var value = ((ITaggedComponent<T>[])storage)[meta.Row].Component;
                    yield return value;
                }
            }
        }

        public static void RemoveComponentsIncludingTagged<T>(this World world, Entity entity)
        {
            var archetypes = world.Archetypes;
            var meta = archetypes._meta[entity.Identity.Id];
            var table = archetypes._tables[meta.TableId];

            foreach (var (storageType, _) in table)
            {
                var type = storageType.Type;
                if (typeof(T).IsAssignableFrom(type))
                {
                    archetypes.RemoveComponent(storageType, entity.Identity);
                }
                else if (typeof(ITaggedComponent).IsAssignableFrom(type) && typeof(T).IsAssignableFrom(type.GetGenericArguments()[0]))
                {
                    archetypes.RemoveComponent(storageType, entity.Identity);
                }
            }
        }
    }
}
