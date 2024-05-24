using System.Collections.Generic;
using Game;

namespace RelEcs
{
    public interface IComponentsBuilder
    {
        void CollectTypes<TCollection>(TCollection types) where TCollection : ICollection<StorageType>;
        void Build(ArchetypesBuilder archetypes, Identity entityIdentity);
    }

    [DisallowDefaultConstructor]
    public readonly ref struct ArchetypesBuilder
    {
        private readonly Archetypes _archetypes;

        public ArchetypesBuilder(Archetypes archetypes)
        {
            _archetypes = archetypes;
        }

        public void SetValue<T>(Identity identity, T value) where T : struct
        {
            var type = StorageType.Create(value.GetType());
            if (type.IsTag) return;

            var meta = _archetypes.GetEntityMeta(identity);
            var table = _archetypes.GetTable(meta.TableId);
            var storage = table.GetStorage<T>();
            storage[meta.Row] = value;
        }

        public void SetValue(Identity identity, object? value)
        {
            if (value == null) return;

            var type = StorageType.Create(value.GetType());
            if (type.IsValueType)
            {
                if (type.IsTag) return;

                var meta = _archetypes.GetEntityMeta(identity);
                var table = _archetypes.GetTable(meta.TableId);
                var storage = table.GetStorage(type);
                storage.SetValue(value, meta.Row);
            }
            else
            {
                var components = _archetypes.EntityReferenceTypeComponents[identity];
                components[type].Add(value);
            }
        }
    }
}
