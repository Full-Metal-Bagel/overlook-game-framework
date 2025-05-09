#if OVERLOOK_ECS_USE_UNITY_COLLECTION

using System;
using Unity.Collections;

namespace Overlook.Ecs.Experimental;

public readonly record struct NativeQueryEntity(Entity Entity, NativeQuery Query)
{
    public bool Has<T>() where T : unmanaged => Query.Has<T>(Entity);
    public ref T Get<T>() where T : unmanaged => ref Query.Get<T>(Entity);
}

public readonly struct NativeQuery : IQuery<NativeQuery.Enumerator, NativeQueryEntity>
{
    public static NativeQuery Empty => new(default, new NativeList<int>(0, Allocator.Temp));

    private readonly NativeArchetypes _archetypes;
    public NativeList<int> TableIds { get; }

    public NativeQuery(NativeArchetypes archetypes, NativeList<int> tableIds)
    {
        _archetypes = archetypes;
        TableIds = tableIds;
    }

    public bool Contains(Entity entity)
    {
        if (!_archetypes.IsAlive(entity.Identity)) return false;
        var meta = _archetypes.GetEntityMeta(entity.Identity);
        int tableId = meta.TableId;

        for (int i = 0; i < TableIds.Length; i++)
        {
            if (TableIds[i] == tableId)
                return true;
        }
        return false;
    }

    public bool Has<T>(Entity entity) where T : unmanaged
    {
        return _archetypes.HasComponent<T>(entity.Identity);
    }

    public ref T Get<T>(Entity entity) where T : unmanaged
    {
        return ref _archetypes.GetComponent<T>(entity.Identity);
    }

    public void ForEach(Action<Entity> action)
    {
        foreach (var queryEntity in this)
        {
            action(queryEntity.Entity);
        }
    }

    public NativeQueryEntity Single()
    {
        using var enumerator = GetEnumerator();
        if (!enumerator.MoveNext()) throw new NoElementsException();
        var entity = enumerator.Current;
        if (enumerator.MoveNext()) throw new MoreThanOneElementsException();
        return entity;
    }

    public NativeQueryEntity SingleOrDefault()
    {
        using var enumerator = GetEnumerator();
        if (!enumerator.MoveNext()) return new NativeQueryEntity { Entity = Entity.None, Query = Empty };
        var entity = enumerator.Current;
        if (enumerator.MoveNext()) throw new MoreThanOneElementsException();
        return entity;
    }

    public NativeQueryEntity First()
    {
        using var enumerator = GetEnumerator();
        if (!enumerator.MoveNext()) throw new NoElementsException();
        return enumerator.Current;
    }

    public Enumerator GetEnumerator() => new(_archetypes, TableIds, this);

    public struct Enumerator : IQueryEnumerator<NativeQueryEntity>, IDisposable
    {
        private NativeList<NativeQueryEntity> _cachedEntities;
        private int _index;

        public Enumerator(NativeArchetypes archetypes, NativeList<int> tableIds, NativeQuery query)
        {
            // Pre-allocate with a reasonable capacity
            _cachedEntities = new NativeList<NativeQueryEntity>(128, Allocator.Temp);
            _index = -1;

            // Cache all matching entities upfront
            for (int tableIndex = 0; tableIndex < tableIds.Length; tableIndex++)
            {
                var table = archetypes.GetTable(tableIds[tableIndex]);
                foreach (var t in table.RowIndexIdentifyMap)
                {
                    var entity = table.GetStorage<Entity>()[t.Key];
                    _cachedEntities.Add(new NativeQueryEntity { Entity = entity, Query = query });
                }
            }
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _cachedEntities.Length;
        }

        public NativeQueryEntity Current => _cachedEntities[_index];

        public void Dispose()
        {
            if (_cachedEntities.IsCreated)
            {
                _cachedEntities.Dispose();
            }
        }
    }
}

#endif
