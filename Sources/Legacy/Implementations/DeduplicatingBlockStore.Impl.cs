using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerWoord.OverDriveStorage.Legacy.Utilities;
using System.Threading;

namespace TerWoord.OverDriveStorage.Legacy.Implementations
{
    partial class DeduplicatingBlockStore
    {
        private readonly ObjectPool<ArraySegment<byte>> _blockBufferPool;

        public int MaxDedupCandidates = 0;
        public override void Store(ulong index, ArraySegment<byte> buffer)
        {
            CheckDisposed();
            var xCRC = Crc32.Compute(buffer);
            ulong? xAlreadyExistingBlockId = null;
            #region find deduplicated block
            var xAllBlocksWithSameCRC = _rawBlockHashManager.GetAllBlocksWithCRC32(xCRC);
            var xCandidateBlockBuff = _blockBufferPool.Acquire();
            try
            {
                unsafe
                {
                    fixed (byte* xBlockPtr = &buffer.Array[buffer.Offset])
                    {
                        fixed (byte* xCandidateBuffPtr = &xCandidateBlockBuff.Array[0])
                        {
                            var xItem = xAllBlocksWithSameCRC.First;
                            while (xItem != null)
                            {
                                var i = xItem.Value;
                                _rawBlockStore.Retrieve(i, xCandidateBlockBuff);
                                if (CompareBlocks(xBlockPtr, xCandidateBuffPtr, _blockSize))
                                {
                                    xAlreadyExistingBlockId = i;
                                    break;
                                }
                                xItem = xItem.Next;
                            }
                        }
                    }
                }
            }
            finally
            {
                _blockBufferPool.Release(xCandidateBlockBuff);
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
            var blockSeg = _blockBufferPool.Acquire();
            var blockBuff = blockSeg.Array;
            try
            {
                var virtualBlockStorePage = index / _virtualBlocksPerBlock;
                var virtualBlockStorePageOffset = (index % _virtualBlocksPerBlock) * 8;

                _virtualBlockStore.Retrieve(virtualBlockStorePage, blockSeg);

                if (_virtualBlockManager.IsReserved(index))
                {
                    // there's already a block in the given virtualblock, which means we have to decrement the UsageCoutn for the old block first
                    var oldRawBlockId = ByteConverter.ReadUInt64(blockBuff, (int)virtualBlockStorePageOffset);
                    _rawBlockUsageCountStore.Decrement(oldRawBlockId);
                }
                ByteConverter.WriteBytes((ulong)rawBlockId, blockBuff, (int)virtualBlockStorePageOffset);
                _virtualBlockStore.Store(virtualBlockStorePage, blockSeg);
                _virtualBlockManager.MarkReserved(index);
                _rawBlockUsageCountStore.Increment(rawBlockId);

                if (_rawBlockUsageCountStore.HasEntriesWhichReachedZero)
                {
                    var xEntries = _rawBlockUsageCountStore.GetZeroReachedEntries();
                    foreach (var xEntry in xEntries)
                    {
                        Console.WriteLine("Todo in DedupStore.SetRawBlockIdForVirtualBlock");
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
                _blockBufferPool.Release(blockSeg);
            }
        }

        public static bool CompareBlocks(byte[] buff, byte[] buff2)
        {
            unsafe
            {
                fixed (byte* xBuffPtr = buff)
                {
                    fixed (byte* xBuff2Ptr = buff2)
                    {
                        return CompareBlocks(xBuffPtr, xBuff2Ptr, (uint)buff.Length);
                    }
                }
            }
        }

        public static unsafe bool CompareBlocks(byte* xBuffPtr, byte* xBlockPtr, uint byteCount)
        {
            ulong* xBuffPtrLong = (ulong*)xBuffPtr;
            ulong* xBlockPtrLong = (ulong*)xBlockPtr;

            bool xMismatch = false;
            byteCount /= 8;
            for (int i = 0; i < byteCount; i++)
            {
                if (xBlockPtrLong[i] != xBuffPtrLong[i])
                {
                    xMismatch = true;
                    break;
                }
            }
            return !xMismatch;
        }

        public override void Retrieve(ulong index, ArraySegment<byte> buffer)
        {
            if(!IsReserved(index))
            {
                throw new Exception(string.Format("Cannot retrieve a free block! (Block index = {0})", index));
            }
            var seg = _blockBufferPool.Acquire();
            var buff = seg.Array;
            try
            {
                var virtualBlockStorePage = index / _virtualBlocksPerBlock;
                var virtualBlockStorePageOffset = (index % _virtualBlocksPerBlock) * 8;

                _virtualBlockStore.Retrieve(virtualBlockStorePage, seg);
                var rawBlockId = ByteConverter.ReadUInt64(buff, (int)virtualBlockStorePageOffset);
                _rawBlockStore.Retrieve(rawBlockId, buffer);
            }
            finally
            {
                _blockBufferPool.Release(seg);
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