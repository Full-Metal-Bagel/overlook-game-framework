using System;
using System.Collections.Generic;
using Game;

namespace RelEcs
{
    public readonly record struct EntityMeta(int TableId, int Row)
    {
        public static EntityMeta Invalid => new(-1, -1);
    }

    public readonly record struct Identity(int Id, int Generation = 1)
    {
        public static Identity None = new(0, 0);
        public static Identity Any = new(int.MaxValue, 0);
        public override string ToString() => $"{Id}({Generation})";
    }

    public class Pool<T>
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
            Debug.Assert(identity.Id < _size);
            Debug.Assert(identity.Generation == _generations[identity.Id]);
            _resources[identity.Id] = _invalidValue;
            // TODO(dan): wrap around
            _generations[identity.Id] += 1;
            _unusedIds.Enqueue(identity.Id);
        }

        public T this[Identity identity]
        {
            get => Get(identity);
            set => Set(identity, value);
        }

        private T Get(Identity identity)
        {
            Debug.Assert(identity.Id < _size);
            // return identity.Generation == _generations[identity.Id] ? _resources[identity.Id] : _invalidValue;
            if (identity.Generation == _generations[identity.Id])
            {
                return _resources[identity.Id];
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
            Debug.Assert(identity.Id < _size);
            if (identity.Generation == _generations[identity.Id])
            {
                _resources[identity.Id] = item;
            }
            else
            {
                Debug.LogError($"Invalid identity {identity}");
            }
        }

        public bool IsAlive(Identity identity)
        {
            return _generations[identity.Id] == identity.Generation;
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
}
