using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace TerWoord.OverDriveStorage.Legacy.Utilities
{
    public class ObjectPool<T>
    {
        private readonly ConcurrentStack<T> _backend = new ConcurrentStack<T>();

        private readonly Func<T> _onCreateNewObject;

        public ObjectPool(Func<T> onCreateNewObject)
        {
            _onCreateNewObject = onCreateNewObject;
        }

        public T Acquire()
        {
            T xResult;
            if (_backend.TryPop(out xResult))
            {
                return xResult;
            }
            return _onCreateNewObject();
        }

        public void Release(T item)
        {
            _backend.Push(item);
        }

        public int Count
        {
            get
            {
                return _backend.Count;
            }
        }
    }
}