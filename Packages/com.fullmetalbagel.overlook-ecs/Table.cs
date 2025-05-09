using System;
using System.Collections.Generic;
#if OVERLOOK_ECS_USE_UNITY_COLLECTION
using TSet = Overlook.Ecs.NativeBitArraySet;
#else
using TSet = Overlook.Ecs.SortedSetTypeSet;
#endif

namespace Overlook.Ecs;

public sealed class Table : IDisposable
{
    public int Id { get; }

    public TSet Types { get; }
    public TSet TypesInHierarchy { get; } = TSet.Create();

    public IList<Identity> Identities => _sortedIdentities.Values;
    public IList<int> Rows => _sortedIdentities.Keys;

    public int Count => _sortedIdentities.Count;
    public bool IsEmpty => Count == 0;

    internal TableStorage TableStorage { get; }
    private readonly SortedList<int /*row*/, Identity> _sortedIdentities = new(32);

    public Table(int id, TSet types, TableStorage tableStorage)
    {
        TableStorage = tableStorage;

        Id = id;
        Types = types;

        foreach (var type in types)
        {
            FillAllTypes(type, TypesInHierarchy);
        }

        // TODO: cache
        static void FillAllTypes(StorageType storageType, TSet set)
        {
            var type = storageType.Type;
            foreach (var interfaceType in type.GetInterfaces())
            {
                var @namespace = interfaceType.Namespace;
                // HACK: skip system interfaces
                if (string.IsNullOrEmpty(@namespace) || !@namespace.Equals(nameof(System), StringComparison.Ordinal))
                    set.Add(StorageType.Create(interfaceType));
            }

            while (type != null)
            {
                set.Add(StorageType.Create(type));
                type = type.BaseType;
            }
        }
    }

    public int Add(Identity identity)
    {
        var row = TableStorage.RentRow();
        _sortedIdentities.Add(row, identity);
        return row;
    }

    public void Remove(int row)
    {
        Debug.Assert(_sortedIdentities.ContainsKey(row), $"Cannot remove row {row} from table {Id} - row not found in sorted identities");
        _sortedIdentities.Remove(row);
        TableStorage.ReleaseRow(row);
    }

    internal T[] GetStorage<T>() where T : struct
    {
        return (T[])GetStorage(StorageType.Create<T>());
    }

    internal Array GetStorage(StorageType type)
    {
        return TableStorage.GetStorage(type);
    }

    public static int MoveEntry(Identity identity, int oldRow, Table oldTable, Table newTable)
    {
        if (ReferenceEquals(oldTable, newTable))
        {
            return oldRow;
        }

        if (ReferenceEquals(oldTable.TableStorage, newTable.TableStorage))
        {
            Debug.Assert(oldTable._sortedIdentities.ContainsKey(oldRow), $"Row {oldRow} not found in source table (ID: {oldTable.Id}) when moving entity {identity}");
            Debug.Assert(!newTable._sortedIdentities.ContainsKey(oldRow), $"Row {oldRow} already exists in destination table (ID: {newTable.Id}) when moving entity {identity}");
            oldTable._sortedIdentities.Remove(oldRow);
            newTable._sortedIdentities.Add(oldRow, identity);
            return oldRow;
        }

        var newRow = newTable.Add(identity);
        foreach (var (type, oldStorage) in oldTable.TableStorage.Storages)
        {
            if (type.IsValueType && newTable.TableStorage.Storages.TryGetValue(type, out var newStorage))
            {
                Array.Copy(oldStorage, oldRow, newStorage, newRow, 1);
            }
        }
        oldTable.Remove(oldRow);
        return newRow;
    }

    public override string ToString()
    {
        var s = $"Table {Id} ";
        foreach (var type in Types)
        {
            s += $"{type} ";
        }
        return s;
    }

    public void Dispose()
    {
        Types.Dispose();
        TypesInHierarchy.Dispose();
    }
}