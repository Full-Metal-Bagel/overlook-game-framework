#if OVERLOOK_ECS_USE_UNITY_COLLECTION

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using TTableStorage = Overlook.Ecs.Experimental.NativeTableStorage<Overlook.Ecs.Fixed8Bytes>;

namespace Overlook.Ecs.Experimental;

public unsafe struct NativeArchetypes : IDisposable
{
    private readonly record struct EntityMetaWithGeneration(EntityMeta Meta, int Generation = 1)
    {
        public static EntityMetaWithGeneration Invalid => new(EntityMeta.Invalid, 0);

        public int TableId => Meta.TableId;
        public int Row => Meta.Row;

        public EntityMetaWithGeneration(int tableId, int row, int generation)
            : this(new EntityMeta(tableId, row), generation)
        {
        }

        public static implicit operator EntityMeta(EntityMetaWithGeneration meta) => meta.Meta;
        public static implicit operator int(EntityMetaWithGeneration meta) => meta.Generation;
    }

    // Metadata for each entity
    private NativeList<EntityMetaWithGeneration> _metaList;
    private NativeQueue<int> _freeMetaIndices;

    // Tables and related collections
    private NativeList<NativeTable> _tables;

    // Use NativeBitArraySet directly as key
    private NativeHashMap<NativeBitArraySet, int> _typeTableMap;
    private UnsafePtrList<TTableStorage> _tableStorages;

    // TODO: profile
    private NativeHashMap<NativeBitArrayMask, NativeQuery> _queries;

    private readonly Allocator _allocator;

    public NativeArchetypes(Allocator allocator)
    {
        _allocator = allocator;

        // Initialize collections
        _metaList = new NativeList<EntityMetaWithGeneration>(512, allocator);
        _freeMetaIndices = new NativeQueue<int>(allocator);

        _tables = new NativeList<NativeTable>(32, allocator);
        _typeTableMap = new NativeHashMap<NativeBitArraySet, int>(32, allocator);

        // Initialize as UnsafePtrList
        _tableStorages = new UnsafePtrList<TTableStorage>(32, allocator);

        _queries = new NativeHashMap<NativeBitArrayMask, NativeQuery>(32, allocator);

        // Create first table with just Entity component
        var types = NativeBitArraySet.Create();
        types.Add(StorageType.Create<Entity>());
        AddTable(types, CreateTableStorage(types));
    }

    public void Dispose()
    {
        // Dispose all tables
        for (int i = 0; i < _tables.Length; i++)
        {
            _tables[i].Dispose();
        }

        // Dispose all table storages and free the memory
        for (int i = 0; i < _tableStorages.Length; i++)
        {
            var storagePtr = _tableStorages[i];
            storagePtr->Dispose();
            UnsafeUtility.Free(storagePtr, _allocator);
        }

        // Dispose all keys in the type table map
        var keyArray = _typeTableMap.GetKeyArray(Allocator.Temp);
        for (int i = 0; i < keyArray.Length; i++)
        {
            keyArray[i].Dispose();
        }
        keyArray.Dispose();

        // Dispose all query masks and table ID lists
        var queryKeyArray = _queries.GetKeyArray(Allocator.Temp);
        var queryValueArray = _queries.GetValueArray(Allocator.Temp);
        for (int i = 0; i < queryKeyArray.Length; i++)
        {
            queryKeyArray[i].Dispose();
            queryValueArray[i].TableIds.Dispose();
        }
        queryKeyArray.Dispose();
        queryValueArray.Dispose();

        // Dispose collections
        _metaList.Dispose();
        _freeMetaIndices.Dispose();
        _tables.Dispose();
        _typeTableMap.Dispose();
        _tableStorages.Dispose();
        _queries.Dispose();
    }

    public Entity Spawn()
    {
        // Create a new identity
        Identity identity;
        if (_freeMetaIndices.TryDequeue(out int index))
        {
            var generation = _metaList[index].Generation;
            identity = new Identity(index, generation); // Reuse index with new version
        }
        else
        {
            identity = new Identity(_metaList.Length, 1);
            _metaList.Add(EntityMetaWithGeneration.Invalid);
        }

        // Add to first table (entity-only table)
        var table = _tables[0];
        int row = table.Add(identity);

        // Update metadata
        var meta = new EntityMetaWithGeneration(table.Id, row, identity.Generation);
        _metaList[identity.Index] = meta;

        // Set the entity component
        var entityStorage = table.GetStorage<Entity>();
        var entity = new Entity(identity);
        entityStorage[row] = entity;
        return entity;
    }

    public void Despawn(Identity identity)
    {
        if (!IsAlive(identity)) return;

        // Get entity metadata
        var metaIndex = identity.Index;
        var meta = _metaList[metaIndex];

        // Remove from table
        var table = _tables[meta.TableId];
        table.Remove(meta.Row);

        // Clear metadata and mark as available, with incremented generation
        var nextGeneration = meta.Generation + 1;
        _metaList[metaIndex] = new EntityMetaWithGeneration(EntityMeta.Invalid, nextGeneration);
        _freeMetaIndices.Enqueue(metaIndex);
    }

    public readonly bool IsAlive(Identity identity)
    {
        if (identity.Index >= _metaList.Length || identity.Index < 0) return false;
        return _metaList[identity.Index] == identity.Generation;
    }

    public void AddComponent<T>(Identity identity, T data) where T : unmanaged
    {
        if (!IsAlive(identity))
            throw new ArgumentException($"Entity {identity} is not alive", nameof(identity));

        var type = StorageType.Create<T>();
        var (table, row) = AddComponentType(identity, type);

        if (!type.IsTag)
        {
            var storage = table.GetStorage<T>();
            storage[row] = data;
        }
    }

    internal readonly EntityMeta GetEntityMeta(in Identity identity) => _metaList[identity.Index];

    public void RemoveComponent<T>(Identity identity) where T : unmanaged
    {
        if (!IsAlive(identity))
            throw new ArgumentException($"Entity {identity} is not alive", nameof(identity));

        RemoveComponentType(identity, StorageType.Create<T>());
    }

    public readonly ref T GetComponent<T>(Identity identity) where T : unmanaged
    {
        if (!IsAlive(identity))
            throw new ArgumentException($"Entity {identity} is not alive", nameof(identity));

        var meta = _metaList[identity.Index];
        var table = _tables[meta.TableId];
        var storage = table.GetStorage<T>();
        return ref UnsafeUtility.ArrayElementAsRef<T>(storage.GetUnsafePtr(), meta.Row);
    }

    public readonly bool HasComponent<T>(Identity identity) where T : unmanaged
    {
        if (!IsAlive(identity))
            throw new ArgumentException($"Entity {identity} is not alive", nameof(identity));

        var type = StorageType.Create<T>();
        var meta = _metaList[identity.Index];
        var table = _tables[meta.TableId];
        return table.Types.Contains(type);
    }

    private (NativeTable table, int row) AddComponentType(Identity identity, StorageType type)
    {
        if (!IsAlive(identity))
            throw new ArgumentException($"Entity {identity} is not alive", nameof(identity));

        var meta = _metaList[identity.Index];
        var oldTable = _tables[meta.TableId];

        // Create a new set of types that includes the new type
        using var newTypes = NativeBitArraySet.Create(oldTable.Types, Allocator.Temp);
        newTypes.Add(type);

        // Get or create new table
        NativeTable newTable;
        int tableIndex;
        if (!TryGetTableByTypes(newTypes, out tableIndex))
        {
            var persistentNewTypes = NativeBitArraySet.Create(newTypes, _allocator);
            var tableStorage = type.IsTag ? _tableStorages[oldTable.TableStorage.Id] : CreateTableStorage(newTypes);
            newTable = AddTable(persistentNewTypes, tableStorage);
        }
        else
        {
            newTable = _tables[tableIndex];
        }

        // Move the entity to the new table
        int newRow = NativeTable.MoveEntry(identity, meta.Row, ref oldTable, ref newTable);

        // Update metadata
        meta = meta with { Meta = new EntityMeta(newTable.Id, newRow) };
        _metaList[identity.Index] = meta;

        return (newTable, newRow);
    }

    private void RemoveComponentType(Identity identity, StorageType type)
    {
        if (!IsAlive(identity))
            throw new ArgumentException($"Entity {identity} is not alive", nameof(identity));

        var meta = _metaList[identity.Index];
        var oldTable = _tables[meta.TableId];

        if (!oldTable.Types.Contains(type))
        {
            throw new Exception($"Cannot remove non-existent component {type.Type.Name} from entity {identity}");
        }

        // Create a new set of types that excludes the type to remove
        using var newTypes = NativeBitArraySet.Create(oldTable.Types, Allocator.Temp);
        newTypes.Remove(type);

        // Get or create new table
        NativeTable newTable;
        int tableIndex;
        if (!TryGetTableByTypes(newTypes, out tableIndex))
        {
            var persistentNewTypes = NativeBitArraySet.Create(newTypes, _allocator);
            var tableStorage = type.IsTag ? _tableStorages[oldTable.TableStorage.Id] : CreateTableStorage(newTypes);
            newTable = AddTable(persistentNewTypes, tableStorage);
        }
        else
        {
            newTable = _tables[tableIndex];
        }

        // Move the entity to the new table
        int newRow = NativeTable.MoveEntry(identity, meta.Row, ref oldTable, ref newTable);

        // Update metadata
        meta = meta with { Meta = new EntityMeta(newTable.Id, newRow) };
        _metaList[identity.Index] = meta;
    }

    private TTableStorage* CreateTableStorage(NativeBitArraySet types)
    {
        // Allocate memory for new storage and create it
        var storageId = _tableStorages.Length;
        var storagePtr = (TTableStorage*)UnsafeUtility.Malloc(
            UnsafeUtility.SizeOf<TTableStorage>(),
            UnsafeUtility.AlignOf<TTableStorage>(),
            _allocator);

        *storagePtr = new TTableStorage(storageId, types, _allocator);

        // Add the pointer to the list
        _tableStorages.Add(storagePtr);
        return storagePtr;
    }

    private NativeTable AddTable(NativeBitArraySet types, TTableStorage* storagePtr)
    {
        int tableId = _tables.Length;

        // Create the table with the storage
        var table = new NativeTable(tableId, types, storagePtr, _allocator);
        _tables.Add(table);

        // Create a persistent copy of the types for the key
        var persistentTypes = NativeBitArraySet.Create(types);

        // Add to type-table map using the set directly
        _typeTableMap.Add(persistentTypes, tableId);

        // Update existing queries
        foreach (var t in _queries)
        {
            if (t.Key.IsMaskCompatibleWith(persistentTypes))
                t.Value.TableIds.Add(tableId);
        }

        return table;
    }

    private bool TryGetTableByTypes(NativeBitArraySet types, out int tableIndex)
    {
        // Use the set directly for lookup
        return _typeTableMap.TryGetValue(types, out tableIndex);
    }

    public NativeQuery GetQuery(NativeBitArrayMask mask)
    {
        if (_queries.TryGetValue(mask, out var query))
        {
            mask.Dispose();
            return query;
        }

        var tables = new NativeList<int>(_tables.Length, _allocator);
        for (int i = 0; i < _tables.Length; i++)
        {
            if (mask.IsMaskCompatibleWith(_tables[i].Types))
            {
                tables.Add(i);
            }
        }
        query = new NativeQuery(this, tables);
        _queries.Add(mask, query);
        return query;
    }

    public readonly NativeTable GetTable(int tableId)
    {
        return _tables[tableId];
    }

    public readonly Entity GetEntity(int tableId, int row)
    {
        return _tables[tableId].GetStorage<Entity>()[row];
    }
}

#endif
