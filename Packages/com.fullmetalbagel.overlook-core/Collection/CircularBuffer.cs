using System;
using System.Runtime.InteropServices;
#if UNITY_2020_1_OR_NEWER
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
#endif

namespace Overlook;

/// <summary>
/// High-performance circular buffer implementation optimized for single-thread scenarios.
/// Uses Marshal.AllocHGlobal for .NET applications and Unity.Collections.NativeArray for Unity games.
/// </summary>
/// <typeparam name="T">The struct type to store in the buffer</typeparam>
public sealed unsafe class CircularBuffer<T> : IDisposable where T : unmanaged
{
#if UNITY_2020_1_OR_NEWER
    private NativeArray<T> _nativeArray;
    private T* _buffer;
#else
    private T* _buffer;
#endif

    private int _capacity;
    private readonly Func<int, int> _expandFunction;

    private int _head;
    private int _tail;
    private int _count;

    private bool _disposed;

    /// <summary>
    /// Gets the capacity of the circular buffer.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the current number of elements in the buffer.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets whether the buffer is empty.
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Gets whether the buffer is full.
    /// </summary>
    public bool IsFull => Count == _capacity;

    /// <summary>
    /// Gets the remaining space in the buffer.
    /// </summary>
    public int Available => _capacity - Count;

    /// <summary>
    /// Initializes a new circular buffer with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the buffer.</param>
    /// <param name="expandFunction">Function that takes current capacity and returns new capacity (default: doubles the capacity).</param>
    public CircularBuffer(int capacity, Func<int, int>? expandFunction = null)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");

        _capacity = capacity;
        _expandFunction = expandFunction ?? (currentCapacity => Math.Max(currentCapacity * 2, currentCapacity + 1));

#if UNITY_2020_1_OR_NEWER
        // Use Unity's NativeArray for Unity applications
        _nativeArray = new NativeArray<T>(capacity, Allocator.Persistent);
        _buffer = (T*)_nativeArray.GetUnsafePtr();
#else
        // Use Marshal.AllocHGlobal for .NET applications
        _buffer = (T*)Marshal.AllocHGlobal(_capacity * sizeof(T));
#endif

        _head = 0;
        _tail = 0;
        _count = 0;
    }

    /// <summary>
    /// Adds an item to the end of the buffer.
    /// If the buffer is full, expands the capacity automatically.
    /// </summary>
    /// <param name="item">The item to add</param>
    public void Push(T item)
    {
        // Expand buffer if full
        if (_count >= _capacity)
        {
            ExpandCapacity();
        }

        _buffer[_tail] = item;
        _tail = (_tail + 1) % _capacity;
        _count++;
    }

    /// <summary>
    /// Tries to add an item to the buffer without overwriting.
    /// </summary>
    /// <param name="item">The item to add</param>
    public bool TryPush(T item)
    {
        if (_count >= _capacity) return false;

        _buffer[_tail] = item;
        _tail = (_tail + 1) % _capacity;
        _count++;
        return true;
    }

    /// <summary>
    /// Removes and returns the oldest item from the buffer.
    /// </summary>
    /// <returns>The oldest item</returns>
    /// <exception cref="InvalidOperationException">Thrown when buffer is empty</exception>
    public T Pop()
    {
        if (_count == 0)
            throw new InvalidOperationException("Buffer is empty");

        T item = _buffer[_head];
        _head = (_head + 1) % _capacity;
        _count--;
        return item;
    }

    /// <summary>
    /// Tries to remove and return the oldest item from the buffer.
    /// </summary>
    /// <param name="item">The removed item, or default if buffer is empty</param>
    /// <returns>True if item was removed, false if buffer is empty</returns>
    public bool TryPop(out T item)
    {
        if (_count == 0)
        {
            item = default;
            return false;
        }

        item = _buffer[_head];
        _head = (_head + 1) % _capacity;
        _count--;
        return true;
    }

    /// <summary>
    /// Returns the oldest item without removing it.
    /// </summary>
    /// <returns>The oldest item</returns>
    /// <exception cref="InvalidOperationException">Thrown when buffer is empty</exception>
    public ref T Peek()
    {
        if (_count == 0)
            throw new InvalidOperationException("Buffer is empty");

        return ref _buffer[_head];
    }

    /// <summary>
    /// Tries to return the oldest item without removing it.
    /// </summary>
    /// <param name="item">The oldest item, or default if buffer is empty</param>
    /// <returns>True if item was retrieved, false if buffer is empty</returns>
    public bool TryPeek(out T item)
    {
        if (_count == 0)
        {
            item = default;
            return false;
        }

        item = _buffer[_head];
        return true;
    }

    /// <summary>
    /// Gets an item at the specified index (0 = oldest item).
    /// </summary>
    /// <param name="index">The index (0-based from oldest item)</param>
    /// <returns>The item at the specified index</returns>
    public T this[int index]
    {
        get
        {
            var actualIndex = GetActualIndex(index);
            return _buffer[actualIndex];
        }
        set
        {
            var actualIndex = GetActualIndex(index);
            _buffer[actualIndex] = value;
        }
    }

    private int GetActualIndex(int index)
    {
        if (index < 0 || index >= _count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return (_head + index) % _capacity;
    }

    /// <summary>
    /// Clears all items from the buffer.
    /// </summary>
    public void Clear()
    {
        _head = 0;
        _tail = 0;
        _count = 0;
    }

    /// <summary>
    /// Expands the buffer capacity using the expand function.
    /// </summary>
    private void ExpandCapacity()
    {
        int newCapacity = _expandFunction(_capacity);
        if (newCapacity <= _capacity)
            throw new InvalidOperationException("Expand function must return a capacity greater than current capacity");
        ExpandToCapacity(newCapacity);
    }

    /// <summary>
    /// Copies buffer contents to an array.
    /// </summary>
    /// <returns>Array containing buffer contents in order (oldest to newest)</returns>
    public T[] ToArray()
    {
        if (_count == 0)
            return Array.Empty<T>();

        T[] result = new T[_count];
        for (int i = 0; i < _count; i++)
        {
            int actualIndex = (_head + i) % _capacity;
            result[i] = _buffer[actualIndex];
        }
        return result;
    }

    /// <summary>
    /// Copies buffer contents to a span.
    /// </summary>
    /// <param name="destination">The destination span</param>
    /// <returns>Number of items copied</returns>
    public int CopyTo(Span<T> destination)
    {
        int itemsToCopy = Math.Min(_count, destination.Length);

        for (int i = 0; i < itemsToCopy; i++)
        {
            int actualIndex = (_head + i) % _capacity;
            destination[i] = _buffer[actualIndex];
        }

        return itemsToCopy;
    }

    /// <summary>
    /// Pushes multiple items to the buffer.
    /// </summary>
    /// <param name="items">Items to push</param>
    /// <returns>Number of items added (always equals items.Length since buffer auto-expands)</returns>
    public void PushRange(ReadOnlySpan<T> items)
    {
        foreach (var t in items)
        {
            Push(t);
        }
    }

    /// <summary>
    /// Pops multiple items from the buffer.
    /// </summary>
    /// <param name="destination">Destination for popped items</param>
    /// <returns>Number of items actually popped</returns>
    public int PopRange(Span<T> destination)
    {
        int itemsToTake = Math.Min(_count, destination.Length);

        for (int i = 0; i < itemsToTake; i++)
        {
            destination[i] = Pop();
        }

        return itemsToTake;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the buffer (oldest to newest).
    /// </summary>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// High-performance struct enumerator that avoids GC allocations.
    /// </summary>
    public ref struct Enumerator
    {
        private readonly CircularBuffer<T> _buffer;
        private readonly int _count;
        private int _index;

        internal Enumerator(CircularBuffer<T> buffer)
        {
            _buffer = buffer;
            _count = buffer._count;
            _index = -1;
        }

        /// <summary>
        /// Gets the current element.
        /// </summary>
        public readonly T Current => _buffer[_index];

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns>True if the enumerator was successfully advanced; false if it has passed the end.</returns>
        public bool MoveNext()
        {
            _index++;
            return _index < _count;
        }

        /// <summary>
        /// Resets the enumerator to its initial position.
        /// </summary>
        public void Reset()
        {
            _index = -1;
        }
    }

    /// <summary>
    /// Releases unmanaged memory.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
#if UNITY_2020_1_OR_NEWER
            // Dispose Unity NativeArray
            if (_nativeArray.IsCreated)
            {
                _nativeArray.Dispose();
            }
#else
            // Free .NET unmanaged memory
            Marshal.FreeHGlobal((IntPtr)_buffer);
#endif
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer to ensure unmanaged memory is released.
    /// </summary>
    ~CircularBuffer()
    {
        Dispose();
    }

    /// <summary>
    /// Adds an item to the end of the buffer with the old overwrite behavior.
    /// If the buffer is full, overwrites the oldest item instead of expanding.
    /// </summary>
    /// <param name="item">The item to add</param>
    /// <returns>True if item was added without overwriting, false if oldest item was overwritten</returns>
    public bool PushWithOverwrite(T item)
    {
        bool wasNotFull = _count < _capacity;

        _buffer[_tail] = item;
        _tail = (_tail + 1) % _capacity;

        if (_count < _capacity)
        {
            _count++;
        }
        else
        {
            // Buffer is full, advance head (overwrite oldest)
            _head = (_head + 1) % _capacity;
        }

        return wasNotFull;
    }

    /// <summary>
    /// Manually expands the buffer capacity to the specified size.
    /// </summary>
    /// <param name="newCapacity">The new capacity (must be larger than current capacity)</param>
    public void ExpandTo(int newCapacity)
    {
        if (newCapacity <= _capacity)
            throw new ArgumentException("New capacity must be larger than current capacity", nameof(newCapacity));

        ExpandToCapacity(newCapacity);
    }

    /// <summary>
    /// Internal method to expand to a specific capacity.
    /// </summary>
    /// <param name="newCapacity">The target capacity</param>
    private void ExpandToCapacity(int newCapacity)
    {
        // Create new buffer with specified capacity
#if UNITY_2020_1_OR_NEWER
        var newNativeArray = new NativeArray<T>(newCapacity, Allocator.Persistent);
        T* newBuffer = (T*)newNativeArray.GetUnsafePtr();
#else
        T* newBuffer = (T*)Marshal.AllocHGlobal(newCapacity * sizeof(T));
#endif

        // Copy existing data to new buffer in correct order
        for (int i = 0; i < _count; i++)
        {
            int sourceIndex = (_head + i) % _capacity;
            newBuffer[i] = _buffer[sourceIndex];
        }

        // Dispose old buffer
#if UNITY_2020_1_OR_NEWER
        if (_nativeArray.IsCreated)
        {
            _nativeArray.Dispose();
        }
        _nativeArray = newNativeArray;
#else
        Marshal.FreeHGlobal((IntPtr)_buffer);
#endif

        // Update buffer reference and indices
        _buffer = newBuffer;
        _capacity = newCapacity;
        _head = 0;
        _tail = _count;
    }
}
