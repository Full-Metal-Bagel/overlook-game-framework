#if OVERLOOK_ECS_USE_UNITY_COLLECTION

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

using TTableStorage = Overlook.Ecs.Experimental.NativeTableStorage<Overlook.Ecs.Fixed8Bytes>;

namespace Overlook.Ecs.Experimental;

public unsafe struct NativeTable : INativeDisposable
{
    public int Id { get; }

    public NativeBitArraySet Types { get; }

    private readonly TTableStorage* _tableStoragePtr;
    internal ref TTableStorage TableStorage => ref UnsafeUtility.AsRef<TTableStorage>(_tableStoragePtr);
    private NativeHashMap<int/*row*/, Identity> _rowIndexIdentityMap;
    internal NativeHashMap<int /*row*/, Identity> RowIndexIdentifyMap => _rowIndexIdentityMap;
    public int Count => _rowIndexIdentityMap.Count;
    public bool IsEmpty => Count == 0;

    public NativeTable(int id, NativeBitArraySet types, TTableStorage* tableStoragePtr, Allocator allocator) : this()
    {
        _tableStoragePtr = tableStoragePtr;
        Id = id;
        Types = NativeBitArraySet.Create(types, allocator);
        _rowIndexIdentityMap = new NativeHashMap<int, Identity>(32, allocator);
    }

    public void Dispose()
    {
        Types.Dispose();
        _rowIndexIdentityMap.Dispose();
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        return JobHandle.CombineDependencies(
            Types.Dispose(inputDeps),
            _rowIndexIdentityMap.Dispose(inputDeps)
        );
    }

    public int Add(Identity identity)
    {
        var row = TableStorage.RentRow();
        _rowIndexIdentityMap.Add(row, identity);
        return row;
    }

    public void Remove(int row)
    {
        Debug.Assert(_rowIndexIdentityMap.ContainsKey(row));
        _rowIndexIdentityMap.Remove(row);
        TableStorage.ReleaseRow(row);
    }

    internal NativeSlice<T> GetStorage<T>() where T : unmanaged
    {
        return GetStorage(StorageType.Create<T>()).SliceConvert<T>();
    }

    internal NativeSlice<byte> GetStorage(StorageType type)
    {
        return TableStorage.GetStorage(type);
    }

    public static int MoveEntry(Identity identity, int oldRow, ref NativeTable oldTable, ref NativeTable newTable)
    {
        if (oldTable.Id == newTable.Id)
        {
            return oldRow;
        }

        if (oldTable.Id == newTable.Id)
        {
            Debug.Assert(oldTable._rowIndexIdentityMap.ContainsKey(oldRow));
            Debug.Assert(!newTable._rowIndexIdentityMap.ContainsKey(oldRow));
            oldTable._rowIndexIdentityMap.Remove(oldRow);
            newTable._rowIndexIdentityMap.Add(oldRow, identity);
            return oldRow;
        }

        var newRow = newTable.Add(identity);
        foreach (var type in oldTable.Types)
        {
            if (type is { IsUnmanagedType: true, IsTag: false } && newTable.Types.Contains(type))
            {
                var size = type.UnmanagedTypeSize;
                var oldValue = oldTable.GetStorage(type).Slice(size * oldRow, size);
                var newValue = newTable.GetStorage(type).Slice(size * newRow, size);
                newValue.CopyFrom(oldValue);
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
}

#endif
