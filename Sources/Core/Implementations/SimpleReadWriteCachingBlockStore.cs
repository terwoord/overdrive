using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerWoord.OverDriveStorage.Utilities;
using System.IO;

namespace TerWoord.OverDriveStorage.Implementations
{
    public class SimpleReadWriteCachingBlockStore:BaseBlockStore
    {
        private class CacheItem
        {
            public CacheItem(uint blockSize)
            {
                Bytes = new byte[blockSize];
                BytesSeg = new ArraySegment<byte>(Bytes);
            }
            public bool IsDirty;
            public readonly byte[] Bytes;
            public readonly ArraySegment<byte> BytesSeg;
        }
        private readonly IBlockStore _store;
        private readonly ObjectPool<CacheItem> _itemPool;
        private readonly SimpleRecentItemCache<CacheItem> _blockCache;
        private readonly uint _blockSize;

        public SimpleReadWriteCachingBlockStore(IBlockStore store, uint capacity = 512)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            _store = store;
            _blockSize = _store.BlockSize;

            _itemPool = new ObjectPool<CacheItem>(() => new CacheItem(_blockSize));

            _blockCache = new SimpleRecentItemCache<CacheItem>();
            _blockCache.CacheCapacity = capacity;
            _blockCache.ItemRemovedFromCache = ItemRemovedFromCache;
            _blockCache.OnCacheMiss = CacheMiss;
        }

        private void ItemRemovedFromCache(ulong index, CacheItem value)
        {
            if(value.IsDirty)
            {
                _store.Store(index, value.BytesSeg);
            }
            _itemPool.Release(value);
        }

        private CacheItem CacheMiss(ulong index)
        {
            var xItem = _itemPool.Acquire();
            _store.Retrieve(index, xItem.BytesSeg);
            xItem.IsDirty = false;
            return xItem;
        }

        protected override void DoDispose()
        {
            _blockCache.Dispose();
            _store.Dispose();
        }

        public override void Store(ulong index, ArraySegment<byte> buffer)
        {
            CacheItem cacheItem;
            if(!_blockCache.TryGetValue(index, out cacheItem))
            {
                cacheItem = _itemPool.Acquire();
            }
            Buffer.BlockCopy(buffer.Array, buffer.Offset, cacheItem.Bytes, 0, buffer.Count);
            cacheItem.IsDirty = true;
            _blockCache[index] = cacheItem;
        }

        public override void Retrieve(ulong index, ArraySegment<byte> buffer)
        {
            var cacheItem = _blockCache[index];
            Buffer.BlockCopy(cacheItem.Bytes, 0, buffer.Array, buffer.Offset, cacheItem.Bytes.Length);
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
        }

        public override void PreloadCaches()
        {
            base.PreloadCaches();
            var xMaxItems = Math.Min(_blockCache.CacheCapacity, _store.BlockCount);
            for (ulong i = 0; i < xMaxItems; i++)
            {
                _blockCache[i].ToString();
            }
        }
    }
}
