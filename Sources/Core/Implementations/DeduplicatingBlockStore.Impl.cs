using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TerWoord.OverDriveStorage.Utilities;

namespace TerWoord.OverDriveStorage.Implementations
{
    partial class DeduplicatingBlockStore
    {
        private readonly ObjectPool<ArraySegment<byte>> _rawBlockBufferPool;
        private readonly ObjectPool<ArraySegment<byte>> _virtualBlockBufferPool;

        public int MaxDedupCandidates = 0;

        public override void Store(ulong index, ArraySegment<byte> buffer)
        {
            CheckDisposed();
            var xCRC = Crc32.Compute(buffer);
            ulong? xAlreadyExistingBlockId = null;

            #region find deduplicated block

            var xAllBlocksWithSameCRC = _rawBlockHashManager.GetAllBlocksWithCRC32(xCRC);
            var xCandidateBlockBuff = _rawBlockBufferPool.Acquire();
            try
            {
                foreach (var xItem in xAllBlocksWithSameCRC)
                {
                    _rawBlockStore.Retrieve(xItem, xCandidateBlockBuff);
                    if (CompareBlocks(buffer.Array, xCandidateBlockBuff.Array, buffer.Offset, 0, (int)_blockSize))
                    {
                        xAlreadyExistingBlockId = xItem;
                        break;
                    }
                }
            }
            finally
            {
                _rawBlockBufferPool.Release(xCandidateBlockBuff);
            }

            #endregion find deduplicated block

            if (!xAlreadyExistingBlockId.HasValue)
            {
                xAlreadyExistingBlockId = _rawBlockManager.Reserve();
                _rawBlockStore.Store(xAlreadyExistingBlockId.Value, buffer);
                _rawBlockHashManager.AddBlock(xAlreadyExistingBlockId.Value, xCRC);
            }
            SetRawBlockIdForVirtualBlock(index, xAlreadyExistingBlockId.Value, xCRC);
        }

        private void SetRawBlockIdForVirtualBlock(ulong index, ulong rawBlockId, uint blockCrc)
        {
            var blockSeg = _rawBlockBufferPool.Acquire();
            var blockBuff = blockSeg.Array;
            try
            {
                var virtSeg = _virtualBlockBufferPool.Acquire();
                try
                {
                    _virtualBlockStore.Retrieve(index, virtSeg);

                    if (_virtualBlockManager.IsReserved(index))
                    {
                        // there's already a block in the given virtualblock, which means we have to decrement the UsageCoutn for the old block first
                        var oldRawBlockId = ByteConverter.ReadUInt64(virtSeg.Array, 0);
                        _rawBlockUsageCountStore.Decrement(oldRawBlockId);
                    }
                    ByteConverter.WriteBytes((ulong)rawBlockId, virtSeg.Array, 0);
                    _virtualBlockStore.Store(index, virtSeg);
                    _virtualBlockManager.MarkReserved(index);
                    _rawBlockUsageCountStore.Increment(rawBlockId);

                    if (_rawBlockUsageCountStore.HasEntriesWhichReachedZero)
                    {
                        var xEntries = _rawBlockUsageCountStore.GetZeroReachedEntries();
                        foreach (var xEntry in xEntries)
                        {
                            // todo: somehow cache crc value of blocks
                            _rawBlockStore.Retrieve(xEntry, blockSeg);
                            var xCrc = Crc32.Compute(blockBuff);

                            _rawBlockHashManager.RemoveBlock(xEntry, xCrc);
                            _rawBlockManager.Free(xEntry);
                        }
                    }
                }
                finally
                {
                    _virtualBlockBufferPool.Release(virtSeg);
                }
            }
            finally
            {
                _rawBlockBufferPool.Release(blockSeg);
            }
        }

        public static bool CompareBlocks(byte[] buff, byte[] buff2, int buffOffset, int buff2Offset, int length)
        {
            if (buff == null)
            {
                throw new ArgumentNullException("buff");
            }
            if (buff2 == null)
            {
                throw new ArgumentNullException("buff2");
            }
            if (buff.Length > (buffOffset + length))
            {
                throw new ArgumentException("Buffer not long enough!", "buff");
            }
            if (buff2.Length > (buff2Offset + length))
            {
                throw new ArgumentException("Buffer not long enough!", "buff");
            }
            unchecked
            {
                for (int i = 0; i < length; i++)
                {
                    if (buff[buffOffset + i] != buff2[buff2Offset + i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override void Retrieve(ulong index, ArraySegment<byte> buffer)
        {
            if (!IsReserved(index))
            {
                throw new Exception(string.Format("Cannot retrieve a free block! (Block index = {0})", index));
            }
            var seg = _rawBlockBufferPool.Acquire();
            var buff = seg.Array;
            try
            {
                _virtualBlockStore.Retrieve(index, seg);
                var rawBlockId = ByteConverter.ReadUInt64(buff, 0);
                _rawBlockStore.Retrieve(rawBlockId, buffer);
            }
            finally
            {
                _rawBlockBufferPool.Release(seg);
            }
        }

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
                return _virtualBlockCount;
            }
        }

        public ulong Reserve()
        {
            throw new NotImplementedException();
        }

        public bool TryReserve(out ulong value)
        {
            throw new NotImplementedException();
        }

        public ulong[] Reserve(int count)
        {
            throw new NotImplementedException();
        }

        public bool IsReserved(ulong index)
        {
            return _virtualBlockManager.IsReserved(index);
        }

        public void MarkReserved(ulong index)
        {
            throw new NotImplementedException();
        }

        public void Free(ulong index)
        {
            throw new NotImplementedException();
        }
    }
}