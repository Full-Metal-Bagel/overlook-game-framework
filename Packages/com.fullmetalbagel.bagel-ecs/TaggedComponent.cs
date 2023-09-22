using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RelEcs
{
    public interface ITaggedComponent
    {
        object UntypedComponent { get; set; }
    }

    public abstract class TaggedComponent<T> : ITaggedComponent where T : class
    {
        public T Component { get; private set; } = default!;

        public object UntypedComponent
        {
            get => Component;
            set => Component = (T)value;
        }
    }

    public static class TaggedComponentExtension
    {
        public static void AddTaggedComponent<T>(this World world, Entity entity, Type tagType, [DisallowNull] T component) where T : class
        {
            // TODO: optimize?
            var taggedComponent = (ITaggedComponent)Activator.CreateInstance(tagType.MakeGenericType(component.GetType()));
            taggedComponent.UntypedComponent = component;
            world.AddComponent(entity, taggedComponent);
        }

        public static void AddTaggedComponent<T>(this World world, Entity entity, Type tagType) where T : class, new()
        {
            AddTaggedComponent(world, entity, tagType, new T());
        }

        public static void GetUnwrappedComponents(this World world, Entity entity, ICollection<object> components)
        {
            var archetypes = world._archetypes;
            var meta = archetypes.Meta[entity.Identity.Id];
            var table = archetypes.Tables[meta.TableId];

            foreach (var (storageType, storage) in table.Storages)
            {
                if (typeof(ITaggedComponent).IsAssignableFrom(storageType.Type))
                {
                    var value = Unwrap((ITaggedComponent)storage.GetValue(meta.Row));
                    components.Add(value);
                }
                else
                {
                    components.Add(storage.GetValue(meta.Row));
                }
            }
        }

        public static void GetUnwrappedComponents<T>(this World world, Entity entity, ICollection<T> components) where T : class
        {
            var archetypes = world._archetypes;
            var meta = archetypes.Meta[entity.Identity.Id];
            var table = archetypes.Tables[meta.TableId];

            foreach (var (storageType, storage) in table.Storages)
            {
                var type = storageType.Type;
                if (typeof(T).IsAssignableFrom(type))
                {
                    components.Add((T)storage.GetValue(meta.Row));
                }
                else if (typeof(ITaggedComponent).IsAssignableFrom(type))
                {
                    var value = Unwrap<T>((ITaggedComponent)storage.GetValue(meta.Row));
                    if (value != null) components.Add(value);
                }
            }
        }

        private static T? Unwrap<T>(ITaggedComponent wrapper) where T : class
        {
            var value = wrapper.UntypedComponent;
            while (value is not T && value is ITaggedComponent w)
            {
                value = w.UntypedComponent;
            }
            return value as T;
        }

        private static object Unwrap(ITaggedComponent wrapper)
        {
            var value = wrapper.UntypedComponent;
            while (value is ITaggedComponent w)
            {
                value = w.UntypedComponent;
            }
            return value;
        }
    }
}
