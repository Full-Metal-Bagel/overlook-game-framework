using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Overlook.Ecs;

public readonly record struct EntityMeta(int TableId, int Row)
{
    public static EntityMeta Invalid => new(-1, -1);
}

[StructLayout(LayoutKind.Explicit)]
public readonly record struct Identity(
    [field: FieldOffset(sizeof(int))] int Index,
    [field: FieldOffset(0)] int Generation = 1)
{
    [field: FieldOffset(0)] public ulong Id { get; } = unchecked((ulong)Index) << 32 | unchecked((uint)Generation);
    public static Identity None { get; } = new(0, 0);
    public static Identity Any { get; } = new(int.MaxValue, 0);
    public override string ToString() => $"{Index}({Generation})";
}

internal class Pool<T>
{
    private T[] _resources;
    private int[] _generations;
    private readonly Queue<int> _unusedIds = new(32);
    private readonly T _invalidValue;
    private int _size;

    public Pool(int capacity, T invalidValue)
    {
        _resources = new T[capacity];
        _generations = new int[capacity];
        _invalidValue = invalidValue;
    }

    public Identity Add(T item)
    {
        Debug.Assert(_resources.Length == _generations.Length);
        if (_unusedIds.TryDequeue(out int index))
        {
            Debug.Assert(index < _resources.Length);
            _resources[index] = item;
        }
        else
        {
            if (_size == _resources.Length)
            {
                Array.Resize(ref _resources, _resources.Length << 1);
                Array.Resize(ref _generations, _generations.Length << 1);
            }
            index = _size;
            _resources[index] = item;
            _generations[index] = 1;
            _size += 1;
        }
        return new Identity(index, _generations[index]);
    }

    public void Remove(Identity identity)
    {
        Debug.Assert(identity.Index < _size);
        Debug.Assert(identity.Generation == _generations[identity.Index]);
        _resources[identity.Index] = _invalidValue;
        // TODO(dan): wrap around
        _generations[identity.Index] += 1;
        _unusedIds.Enqueue(identity.Index);
    }

    public T this[Identity identity]
    {
        get => Get(identity);
        set => Set(identity, value);
    }

    private T Get(Identity identity)
    {
        Debug.Assert(identity.Index < _size);
        if (identity.Generation == _generations[identity.Index])
        {
            return _resources[identity.Index];
        }
        else
        {
            Debug.LogError($"Invalid identity {identity}");
            // TODO(dan): maybe return null or throw exception instead?
            return _invalidValue;
        }
    }

    private void Set(Identity identity, T item)
    {
        Debug.Assert(identity.Index < _size);
        if (identity.Generation == _generations[identity.Index])
        {
            _resources[identity.Index] = item;
        }
        else
        {
            Debug.LogError($"Invalid identity {identity}");
        }
    }

    public bool IsAlive(Identity identity)
    {
        return _generations[identity.Index] == identity.Generation;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public ref struct Enumerator
    {
        private readonly Pool<T> _pool;
        private int _currentIndex;

        public Enumerator(Pool<T> pool)
        {
            _pool = pool;
            _currentIndex = -1;
        }

        public T Current
        {
            get
            {
                if (_currentIndex < 0 || _currentIndex >= _pool._size)
                {
                    throw new InvalidOperationException();
                }
                return _pool._resources[_currentIndex];
            }
        }

        public bool MoveNext()
        {
            if (_currentIndex < _pool._size - 1)
            {
                _currentIndex += 1;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _currentIndex = -1;
        }
    }
}
