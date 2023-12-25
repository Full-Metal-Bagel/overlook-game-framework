using System;

namespace Game
{
    public class PooledCollectionException : Exception
    {
        public PooledCollectionException()
        {
        }

        public PooledCollectionException(string message) : base(message)
        {
        }

        public PooledCollectionException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
