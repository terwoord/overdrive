using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TerWoord.OverDriveStorage.Legacy.Implementations
{
    public abstract class BaseBlockStore: IBlockStore
    {
        private bool _disposed = false;
        protected bool IsDisposed
        {
            get
            {
                return _disposed;
            }
        }
        
        protected virtual void DoDispose()
        {
            
        }

        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("DeduplicatingBlockStore");
            }
        }

        public void Dispose()
        {
            if(_disposed)
            {
                return;
            }
            GC.SuppressFinalize(this);
            _disposed = true;
            DoDispose();
        }

        public abstract void Store(ulong index, ArraySegment<byte> buffer);

        public virtual void Store(ulong[] indices, ArraySegment<byte> buffer)
        {
            CheckDisposed();
            if(buffer.Count!=(indices.Length*BlockSize))
            {
                throw new Exception("Buffer size not correct!");
            }
            for(int i = 0; i < indices.Length;i++)
            {
                Store(indices[i], new ArraySegment<byte>(buffer.Array, (int)(buffer.Offset + (i * BlockSize)), (int)BlockSize));
            }
        }

        public abstract void Retrieve(ulong index, ArraySegment<byte> buffer);

        public virtual void Retrieve(ulong[] indices, ArraySegment<byte> buffer)
        {
            CheckDisposed();
            if (buffer.Count != (indices.Length * BlockSize))
            {
                throw new Exception("Buffer size not correct!");
            }
            for (int i = 0; i < indices.Length; i++)
            {
                Retrieve(indices[i], new ArraySegment<byte>(buffer.Array, (int)(buffer.Offset + (i * BlockSize)), (int)BlockSize));
            }
        }

        public abstract uint BlockSize
        {
            get;
        }

        public abstract ulong BlockCount
        {
            get;
        }

        public abstract void DumpCacheInfo(StreamWriter output, string linePrefix);
        public virtual void PreloadCaches()
        {

        }

        public virtual string Id
        {
            get;
            set;
        }
    }
}