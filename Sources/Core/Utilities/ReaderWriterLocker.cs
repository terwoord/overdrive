using System;
using System.Threading;

namespace TerWoord.OverDriveStorage
{
    public class ReaderWriterLocker
    {
        private class WriteUnlocker : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            public WriteUnlocker(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
            }

            private bool _disposed;
            void IDisposable.Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                _lock.ExitWriteLock();
                GC.SuppressFinalize(this);
            }
        }

        private class ReadUnlocker : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            public ReadUnlocker(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
            }

            private bool _disposed;
            void IDisposable.Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                _lock.ExitReadLock();
                GC.SuppressFinalize(this);
            }
        }

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public ReaderWriterLocker()
        {
        }

        public IDisposable EnterWriteLock()
        {
            try
            {
                _lock.EnterWriteLock();
            }
            catch (LockRecursionException)
            {
                return null;
            }
            return new WriteUnlocker(_lock);
        }

        public IDisposable EnterReadLock()
        {
            try
            {
                _lock.EnterReadLock();
            }
            catch (LockRecursionException)
            {
                return null;
            }
            return new ReadUnlocker(_lock);
        }
    }
}