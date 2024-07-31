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

    public class Pool<T> where T : unmanaged
    {
        private readonly List<T> _resources;
        private readonly List<int> _generations;
        private readonly Queue<int> _unusedIds = new(32);
        private readonly T _invalidValue;

        public Pool(int defaultCapacity, T invalidValue)
        {
            _resources = new List<T>(defaultCapacity);
            _generations = new List<int>(defaultCapacity);
            _invalidValue = invalidValue;
        }

        public Identity Add(T item)
        {
            Debug.Assert(_resources.Count == _generations.Count);
            if (_unusedIds.TryDequeue(out int index))
            {
                Debug.Assert(index < _resources.Count);
                _resources[index] = item;
            }
            else
            {
                index = _resources.Count;
                _resources.Add(item);
                _generations.Add(1);
            }
            return new Identity(index, _generations[index]);
        }

        public void Remove(Identity identity)
        {
            Debug.Assert(_resources.Count == _generations.Count);
            Debug.Assert(identity.Id < _resources.Count);
            Debug.Assert(identity.Generation == _generations[identity.Id]);
            _resources[identity.Id] = _invalidValue;
            // TODO(dan): wrap around
            _generations[identity.Id] += 1;
            _unusedIds.Enqueue(identity.Id);
        }

        public T Get(Identity identity)
        {
            Debug.Assert(_resources.Count == _generations.Count);
            Debug.Assert(identity.Id < _resources.Count);
            return identity.Generation == _generations[identity.Id] ? _resources[identity.Id] : _invalidValue;
        }

        public void Set(Identity identity, T item)
        {
            Debug.Assert(_resources.Count == _generations.Count);
            Debug.Assert(identity.Id < _resources.Count);
            if (identity.Generation == _generations[identity.Id])
            {
                _resources[identity.Id] = item;
            }
        }

        public bool IsAlive(Identity identity)
        {
            return _generations[identity.Id] == identity.Generation;
        }
    }
}
