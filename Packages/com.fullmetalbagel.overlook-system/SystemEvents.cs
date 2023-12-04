#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Game
{
    public interface ISystemEvents
    {
        void Tick(int systemIndex, int currentFrame);
    }

    public sealed class SystemEvents<T> : ISystemEvents
    {
        private struct EventData
        {
            public T Event { get; init; }
            public int StartFrame { get; set; }
            public int MaxLastingFrames { get; init; }
            public int SystemIndex { get; set; }
        }

        private readonly CircularBuffer<EventData> _events;

        public int PendingCount { get; private set; } = 0;
        public int Count => _events.Count - PendingCount;

        private readonly Lazy<int> _threadId = new(() => Environment.CurrentManagedThreadId);

        public SystemEvents() : this(typeof(T).GetCustomAttribute<SystemEventAttribute>()?.InitCapacity ?? 4)
        {
        }

        public SystemEvents(int capacity)
        {
            _events = new(capacity);
        }

        public T this[int index]
        {
            get
            {
                Debug.Assert(Environment.CurrentManagedThreadId == _threadId.Value);
                if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
                return _events[index].Event;
            }
        }

        public void Append(T @event, int lastingFrames = 1)
        {
            Debug.Assert(lastingFrames >= 1);
            Debug.Assert(Environment.CurrentManagedThreadId == _threadId.Value);
            var data = new EventData { Event = @event, MaxLastingFrames = lastingFrames };
            _events.Push(data);
            PendingCount++;
        }

        public void Tick(int systemIndex, int currentFrame)
        {
            Debug.Assert(systemIndex >= 0);
            Debug.Assert(currentFrame >= 0);
            Debug.Assert(Environment.CurrentManagedThreadId == _threadId.Value);
            var pendingStart = _events.Count - PendingCount;
            for (var i = pendingStart; i < _events.Count; i++)
            {
                ref var data = ref _events[i];
                data.StartFrame = currentFrame;
                data.SystemIndex = systemIndex;
            }

            var popCount = 0;
            while (popCount < pendingStart)
            {
                ref var data = ref _events.Peek();
                if (data.SystemIndex != systemIndex) break;
                _events.Pop();
                var lastingFrames = currentFrame - data.StartFrame;
                if (lastingFrames < data.MaxLastingFrames) _events.Push(data);
                popCount++;
            }
            PendingCount = 0;
        }

        public Enumerator GetEnumerator()
        {
            Debug.Assert(Environment.CurrentManagedThreadId == _threadId.Value);
            return new Enumerator(this);
        }

        [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
        public ref struct Enumerator
        {
            private readonly SystemEvents<T> _systemEvents;
            private int _currentIndex;

            public Enumerator(SystemEvents<T> systemEvents)
            {
                _systemEvents = systemEvents;
                _currentIndex = -1; // Start before the first element
            }

            public T Current
            {
                get
                {
                    Debug.Assert(Environment.CurrentManagedThreadId == _systemEvents._threadId.Value);
                    if (_currentIndex < 0 || _currentIndex >= _systemEvents.Count)
                        throw new InvalidOperationException();
                    return _systemEvents[_currentIndex];
                }
            }

            public bool MoveNext()
            {
                Debug.Assert(Environment.CurrentManagedThreadId == _systemEvents._threadId.Value);
                if (_currentIndex < _systemEvents.Count - 1)
                {
                    _currentIndex++;
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                Debug.Assert(Environment.CurrentManagedThreadId == _systemEvents._threadId.Value);
                _currentIndex = -1;
            }
        }
    }
}
