using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerWoord.OverDriveStorage.Utilities;

namespace TerWoord.OverDriveStorage.Implementations
{
    public class SmarterReadCachingBlockStore: BaseBlockStore
    {
        private readonly IBlockStore _backend;
        public SmarterReadCachingBlockStore(IBlockStore backend, uint cacheBlockSize, uint cacheCapacity = SimpleRecentItemCache<byte[]>.DefaultCacheSize)
        {
            if (backend == null)
            {
                throw new ArgumentNullException("backend");
            }

            _backend = backend;
            _cacheBlockSize = cacheBlockSize;
            if (cacheBlockSize % _backend.BlockSize != 0)
            {
                throw new Exception("Wrong CacheBlockSize value. Should be perfect multiple of backend block size");
            }
            _backendBlocksPerCacheBlock=cacheBlockSize/_backend.BlockSize;

            _bufferPool = new ObjectPool<byte[]>(() => new byte[cacheBlockSize]);

            _cache = new SimpleRecentItemCache<byte[]>(cacheCapacity);
            _cache.ItemRemovedFromCache = ItemRemovedFromCache;
            _cache.OnCacheMiss = CacheMiss;
        }

        private void ItemRemovedFromCache(ulong index, byte[] value)
        {
            // do nothing
            _bufferPool.Release(value);
        }

        private byte[] CacheMiss(ulong index)
        {
            var buff = _bufferPool.Acquire();
            var seg = new ArraySegment<byte>(buff);

            _backend.Retrieve(index * _backendBlocksPerCacheBlock, _backendBlocksPerCacheBlock, seg);

            return buff;
        }

        private readonly SimpleRecentItemCache<byte[]> _cache;
        
        private readonly uint _cacheBlockSize;
        private readonly uint _backendBlocksPerCacheBlock;

        private readonly ObjectPool<byte[]> _bufferPool;

        public override void Store(ulong index, ArraySegment<byte> buffer)
        {
            var xItem = _cache[index / _backendBlocksPerCacheBlock];
            _backend.Store(index, buffer);
            Buffer.BlockCopy(buffer.Array, buffer.Offset, xItem, (int)((index % _backendBlocksPerCacheBlock) * _backend.BlockSize), (int)_backend.BlockSize);
        }

        public override void Retrieve(ulong index, ArraySegment<byte> buffer)
        {
            var xItem = _cache[index / _backendBlocksPerCacheBlock];
            Buffer.BlockCopy(xItem, (int)((index % _backendBlocksPerCacheBlock) * _backend.BlockSize), buffer.Array, buffer.Offset, (int)_backend.BlockSize);
        }

        public override uint BlockSize
        {
            get
            {
                return _backend.BlockSize;
            }
        }

        public override ulong BlockCount
        {
            get
            {
                return _backend.BlockCount;
            }
        }

        public override void DumpCacheInfo(System.IO.StreamWriter output, string linePrefix)
        {
            
        }
    }
}