using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TerWoord.OverDriveStorage.Legacy.Utilities;

namespace TerWoord.OverDriveStorage.Legacy.Implementations
{
    public class SimpleUsageCountStore: IUsageCountStore
    {
        private readonly IBlockStore _backend;
        public SimpleUsageCountStore(IBlockStore backend)
        {
            if (backend == null)
            {
                throw new ArgumentNullException("backend");
            }
            _backend = backend;
            _rawBlockUsagesPerBlock = backend.BlockSize / 8;
            _entryCount = backend.BlockCount * 8;
            _blockBuffPool = new ObjectPool<ArraySegment<byte>>(() =>new ArraySegment<byte>(new byte[_backend.BlockSize]));
        }

        private readonly uint _rawBlockUsagesPerBlock;
        private readonly ulong _entryCount;
        private readonly ObjectPool<ArraySegment<byte>> _blockBuffPool;
        
        private bool _disposed;
        public void Dispose()
        {
            if(_disposed)
            {
                return;
            }
            _disposed = true;
            GC.SuppressFinalize(this);
            _backend.Dispose();
        }

        public ulong EntryCount
        {
            get
            {
                return _entryCount;
            }
        }

        public void Increment(ulong index)
        {
            var buffSeg = _blockBuffPool.Acquire();
            var buff = buffSeg.Array;
            try
            {

                var blockUsagePage = index / _rawBlockUsagesPerBlock;
                var blockUsagePageOffset = (index % _rawBlockUsagesPerBlock) * 8;

                _backend.Retrieve(blockUsagePage, buffSeg);

                var usageCount = ByteConverter.ReadUInt64(buff, (int)blockUsagePageOffset);
                if (usageCount == ulong.MaxValue)
                {
                    throw new Exception("UsageCount overflow!");
                }

                usageCount++;

                ByteConverter.WriteBytes((uint)usageCount, buff, (int)blockUsagePageOffset);
                _backend.Store(blockUsagePage, buffSeg);
            }
            finally
            {
                _blockBuffPool.Release(buffSeg);
            }
        }

        public void Decrement(ulong index)
        {
            var buffSeg = _blockBuffPool.Acquire();
            var buff = buffSeg.Array;
            try
            {

                var blockUsagePage = index / _rawBlockUsagesPerBlock;
                var blockUsagePageOffset = (index % _rawBlockUsagesPerBlock) * 8;

                _backend.Retrieve(blockUsagePage, buffSeg);

                var usageCount = ByteConverter.ReadUInt64(buff, (int)blockUsagePageOffset);
                if (usageCount == 0)
                {
                    throw new Exception("Block with usagecount 0 was decremented, which means something's off!");
                }
                usageCount--;
                ByteConverter.WriteBytes((uint)usageCount, buff, (int)blockUsagePageOffset);
                _backend.Store(blockUsagePage, buffSeg);
                if (usageCount == 0)
                {
                    _entriesWhichReachedZero.Add(index);
                }
            }
            finally
            {
                _blockBuffPool.Release(buffSeg);
            }
        }

        private readonly List<ulong> _entriesWhichReachedZero=new List<ulong>(128); 

        public bool HasEntriesWhichReachedZero
        {
            get
            {
                return _entriesWhichReachedZero.Count > 0;
            }
        }

        public ulong[] GetZeroReachedEntries()
        {
            var xResult = _entriesWhichReachedZero.ToArray();
            _entriesWhichReachedZero.Clear();
            return xResult;
        }

        public void DumpCacheInfo(StreamWriter output, string linePrefix)
        {
            _backend.DumpCacheInfo(output, linePrefix);
        }

        public void PreloadCaches()
        {
            _backend.PreloadCaches();
        }

        private string mId;
        public string Id
        {
            get
            {
                return mId;
            }
            set
            {
                mId = value;
                _backend.Id = value + "-Backend";
            }
        }
    }
}