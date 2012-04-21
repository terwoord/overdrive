using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerWoord.OverDriveStorage.Utilities;

namespace TerWoord.OverDriveStorage.Implementations
{
    public class BlockDividingBlockStore: BaseBlockStore
    {
        private readonly IBlockStore _baseStore;
        public BlockDividingBlockStore(IBlockStore baseStore, uint blockSize)
        {
            if (baseStore == null)
            {
                throw new ArgumentNullException("baseStore");
            }
            _baseStore = baseStore;

            if (baseStore.BlockSize % blockSize != 0)
            {
                throw new Exception("Unsupported division. BaseStore.BlockSize % blockSize should be 0");
            }
            _blockSize = blockSize;
            _blockCount = (_baseStore.BlockCount * _baseStore.BlockSize) / blockSize;
            _blocksPerBackendBlock = _baseStore.BlockSize / blockSize;

            _bufferPool = new ObjectPool<ArraySegment<byte>>(() => new ArraySegment<byte>(new byte[_baseStore.BlockSize]));
        }

        private readonly ObjectPool<ArraySegment<byte>> _bufferPool;

        public override void Store(ulong index, ArraySegment<byte> buffer)
        {
            var xBuff = _bufferPool.Acquire();
            try
            {
                var xBackendBlock = index / _blocksPerBackendBlock;
                _baseStore.Retrieve(xBackendBlock, xBuff);

                var xBackendOffset = (int)((index % _blocksPerBackendBlock) * _blockSize);

                Buffer.BlockCopy(buffer.Array, buffer.Offset, xBuff.Array, xBuff.Offset + xBackendOffset, (int)_blockSize);

                _baseStore.Store(xBackendBlock, xBuff);
            }
            finally
            {
                _bufferPool.Release(xBuff);
            }
        }

        public override void Retrieve(ulong index, ArraySegment<byte> buffer)
        {
            var xBuff = _bufferPool.Acquire();
            try
            {
                var xBackendBlock = index / _blocksPerBackendBlock;
                _baseStore.Retrieve(xBackendBlock, xBuff);

                var xBackendOffset = (int)((index % _blocksPerBackendBlock) * _blockSize);

                Buffer.BlockCopy(xBuff.Array, xBackendOffset, buffer.Array, buffer.Offset, (int)_blockSize);
            }
            finally
            {
                _bufferPool.Release(xBuff);
            }
        }

        private uint _blockSize;
        private ulong _blockCount;
        private uint _blocksPerBackendBlock;

        public override uint BlockSize
        {
            get
            {
                return _blockSize;
            }
        }

        public override ulong BlockCount
        {
            get
            {
                return _blockCount;
            }
        }

        public override void DumpCacheInfo(System.IO.StreamWriter output, string linePrefix)
        {
            throw new NotImplementedException();
        }
    }
}