using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Unity.Collections
{
    public enum Allocator
    {
        Invalid = 0,
        None = 1,
        Temp = 2,
        TempJob = 3,
        Persistent = 4,
        AudioKernel = 5,
        FirstUserIndex = 64, // 0x00000040
    }
}

namespace UnityEngine
{
    public enum LogType
    {
        Error,
        Assert,
        Warning,
        Log,
        Exception,
    }

    public class Object
    {
        public string name { get; set; } = "";
    }
    
    public class GameObject : Object { }
    
    public static class Debug
    {
        public static void Log(object message, Object? _ = null)
        {
            System.Console.WriteLine(message);
        }

        public static void LogWarning(object message, Object? _ = null)
        {
            System.Console.WriteLine($"Warning: {message}");
        }

        public static void LogError(object message, Object? _ = null)
        {
            System.Console.WriteLine($"Error: {message}");
        }

        public static void LogException(System.Exception exception, Object? _ = null)
        {
            System.Console.WriteLine($"Exception: {exception}");
        }

        public static void Assert(bool condition, Object? _ = null)
        {
            System.Diagnostics.Debug.Assert(condition);
        }
        
        public static void Assert(bool condition, string message, Object? _ = null)
        {
            System.Diagnostics.Debug.Assert(condition, message);
        }

        public static void AssertFormat(bool condition, string message, Object? _ = null)
        {
            System.Diagnostics.Debug.Assert(condition, message);
        }
    }
}

namespace UnityEngine.Pool
{
    public static class ListPool<T>
    {
        static readonly ConcurrentQueue<List<T>> s_pool = new();
        
        public static List<T> Get()
        {
            return s_pool.TryDequeue(out List<T> list) ? list : new List<T>();
        }

        public static void Release(List<T> list)
        {
            list.Clear();
            s_pool.Enqueue(list);
        }
    }

    public class ObjectPool<T>
    {
        private readonly System.Func<T> m_CreateFunc;
        private readonly System.Action<T> m_ActionOnGet;
        private readonly System.Action<T> m_ActionOnRelease;
        private readonly bool m_CollectionCheck;
        private readonly int m_DefaultCapacity;
        private readonly int m_MaxSize;
        
        private readonly ConcurrentQueue<T> m_Queue = new();
        private readonly HashSet<T>? m_ActiveItems;

        public ObjectPool(System.Func<T> createFunc, 
            System.Action<T> actionOnGet = null!, 
            System.Action<T> actionOnRelease = null!, 
            bool collectionCheck = false,
            int defaultCapacity = 10,
            int maxSize = 10000)
        {
            m_CreateFunc = createFunc;
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
            m_CollectionCheck = collectionCheck;
            m_DefaultCapacity = defaultCapacity;
            m_MaxSize = maxSize;
            
            m_ActiveItems = collectionCheck ? new HashSet<T>() : null;
        }

        public T Get()
        {
            T item;
            if (m_Queue.TryDequeue(out item))
            {
                m_ActionOnGet?.Invoke(item);
                if (m_CollectionCheck)
                {
                    m_ActiveItems!.Add(item);
                }
                return item;
            }

            item = m_CreateFunc();
            m_ActionOnGet?.Invoke(item);
            if (m_CollectionCheck)
            {
                m_ActiveItems!.Add(item);
            }
            return item;
        }

        public void Release(T item)
        {
            if (m_CollectionCheck)
            {
                if (!m_ActiveItems!.Remove(item))
                {
                    throw new System.InvalidOperationException("Trying to release an object that was not pooled");
                }
            }

            m_ActionOnRelease?.Invoke(item);
            
            if (m_Queue.Count < m_MaxSize)
            {
                m_Queue.Enqueue(item);
            }
        }
    }
}
