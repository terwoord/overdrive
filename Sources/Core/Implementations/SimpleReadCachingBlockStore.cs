using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerWoord.OverDriveStorage.Utilities;
using System.IO;

namespace TerWoord.OverDriveStorage.Implementations
{
    public class SimpleReadCachingBlockStore: BaseBlockStore
    {
        private readonly IBlockStore _store;
        private readonly SimpleRecentItemCache<byte[]> _blockCache;

        private readonly ObjectPool<byte[]> _blockBuffPool;

        public SimpleReadCachingBlockStore(IBlockStore store, uint cacheCapacity = 512)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            _store = store;

            _blockCache = new SimpleRecentItemCache<byte[]>();
            _blockCache.CacheCapacity = cacheCapacity;
            _blockCache.ItemRemovedFromCache = ItemRemovedFromCache;
            _blockCache.OnCacheMiss = CacheMiss;
            _blockBuffPool = new ObjectPool<byte[]>(() => new byte[store.BlockSize]);
        }

        private void ItemRemovedFromCache(ulong index, byte[] value)
        {
            // do nothing
            _blockBuffPool.Release(value);
        }

        private byte[] CacheMiss(ulong index)
        {
            var buff = _blockBuffPool.Acquire();
            var seg = new ArraySegment<byte>(buff);
            _store.Retrieve(index, seg);
            return buff;
        }

        protected override void DoDispose()
        {
            _store.Dispose();
        }

        public override void Store(ulong index, ArraySegment<byte> buffer)
        {
            var buff = _blockBuffPool.Acquire();
            _store.Store(index, buffer);
            Buffer.BlockCopy(buffer.Array, buffer.Offset, buff, 0, buffer.Count);
            _blockCache[index] = buff;
        }

        public override void Retrieve(ulong index, ArraySegment<byte> buffer)
        {
            Buffer.BlockCopy(_blockCache[index], 0, buffer.Array, buffer.Offset, buffer.Count);
        }

        public override uint BlockSize
        {
            get
            {
                return _store.BlockSize;
            }
        }

        public override ulong BlockCount
        {
            get
            {
                return _store.BlockCount;
            }
        }

        public override void DumpCacheInfo(StreamWriter output, string linePrefix)
        {
            output.WriteLine("{0}Cache Capacity: {1}", linePrefix, _blockCache.CacheCapacity);
            output.WriteLine("{0}Cache Count: {1}", linePrefix, _blockCache.CacheCount);
            output.WriteLine("{0}BlockBufferPool Size: {1}", linePrefix, _blockBuffPool.Count);
        }

        public override void PreloadCaches()
        {
            base.PreloadCaches();
            _store.PreloadCaches();
        }
    }
}