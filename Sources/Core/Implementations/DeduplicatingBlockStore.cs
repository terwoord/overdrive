using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TerWoord.OverDriveStorage.Utilities;

namespace TerWoord.OverDriveStorage.Implementations
{
    public partial class DeduplicatingBlockStore : BaseBlockStore, IBlockManager, IBlockManagingStore
    {
        public DeduplicatingBlockStore(IBlockManager virtualBlockManager, IBlockStore virtualBlockStore, IBlockStore rawBlockStore, IBlockManager rawBlockManager, ulong virtualBlockCount, IUsageCountStore rawBlockUsageCountStore, IHashManager<uint> rawBlockHashManager)
        {
            if (virtualBlockManager == null)
            {
                throw new ArgumentNullException("virtualBlockManager");
            }
            if (virtualBlockStore == null)
            {
                throw new ArgumentNullException("virtualBlockStore");
            }
            if (rawBlockStore == null)
            {
                throw new ArgumentNullException("rawBlockStore");
            }
            if (rawBlockManager == null)
            {
                throw new ArgumentNullException("rawBlockManager");
            }
            if (rawBlockUsageCountStore == null)
            {
                throw new ArgumentNullException("rawBlockUsageCountStore");
            }
            if (rawBlockHashManager == null)
            {
                throw new ArgumentNullException("rawBlockHashManager");
            }
            _virtualBlockManager = virtualBlockManager;
            _virtualBlockStore = virtualBlockStore;
            _rawBlockStore = rawBlockStore;
            _rawBlockManager = rawBlockManager;
            _virtualBlockCount = virtualBlockCount;
            _rawBlockUsageCountStore = rawBlockUsageCountStore;
            _rawBlockHashManager = rawBlockHashManager;

            _blockSize = rawBlockStore.BlockSize;
            _virtualBlocksPerBlock = _blockSize / 8;

            // now do checks
            if (_virtualBlockStore.BlockSize != _blockSize)
            {
                throw new Exception("VirtualBlockStore.BlockSize != RawBlockStore.BlockSize");
            }
            if (_virtualBlockStore.BlockCount != (virtualBlockCount / _virtualBlocksPerBlock))
            {
                throw new Exception("VirtualBlockStore.BlockCount != (virtualBlockCount / virtualBlocksPerBlock)");
            }

            _blockBufferPool = new ObjectPool<ArraySegment<byte>>(() => new ArraySegment<byte>(new byte[_blockSize]));
        }

        private readonly IBlockManager _virtualBlockManager;
        private readonly IBlockStore _virtualBlockStore;
        private readonly IBlockStore _rawBlockStore;
        private readonly IBlockManager _rawBlockManager;
        private readonly ulong _virtualBlockCount;
        private readonly IUsageCountStore _rawBlockUsageCountStore;
        private readonly IHashManager<uint> _rawBlockHashManager;

        private readonly uint _blockSize;
        private readonly uint _virtualBlocksPerBlock;

        protected override void DoDispose()
        {
            _virtualBlockStore.Dispose();
            _virtualBlockManager.Dispose();
            _rawBlockStore.Dispose();
            _rawBlockManager.Dispose();
            _rawBlockUsageCountStore.Dispose();
            _rawBlockHashManager.Dispose();
        }

        public override void DumpCacheInfo(StreamWriter output, string linePrefix)
        {
            output.WriteLine("{0}RawBlockStore:", linePrefix);
            _rawBlockStore.DumpCacheInfo(output, linePrefix + "  ");
            output.WriteLine("{0}VirtualBlockStore:", linePrefix);
            _virtualBlockStore.DumpCacheInfo(output, linePrefix + "  ");
            output.WriteLine("{0}RawBlockUsageCountStore:", linePrefix);
            _rawBlockUsageCountStore.DumpCacheInfo(output, linePrefix + "  ");
            output.WriteLine("{0}Maximum number of deduplication candidates: {1}", linePrefix, MaxDedupCandidates);
        }

        public override void PreloadCaches()
        {
            base.PreloadCaches();
            _virtualBlockStore.PreloadCaches();
            _virtualBlockManager.PreloadCaches();
            _rawBlockStore.PreloadCaches();
            _rawBlockManager.PreloadCaches();
            _rawBlockUsageCountStore.PreloadCaches();
        }

        public override string Id
        {
            set
            {
                base.Id = value;
                _virtualBlockStore.Id = value + "-VirtualBlockStore";
                _virtualBlockManager.Id = value + "-VirtualBlockManager";
                _rawBlockStore.Id = value + "-RawBlockStore";
                _rawBlockManager.Id = value + "-RawBlockManager";
                _rawBlockUsageCountStore.Id = value + "-RawBlockUsageCounter";
            }
        }
    }
}