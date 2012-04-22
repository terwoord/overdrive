using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TerWoord.OverDriveStorage.Implementations
{
    partial class ODBFSImpl
    {
        private class VBlockContentStore: BaseBlockStore
        {
            private readonly ODBFSImpl _odbfs;
            private readonly Guid _identifier;

            protected override void DoDispose()
            {
                base.DoDispose();
                using (_odbfs._openBlocksLock.EnterWriteLock())
                {
                    _odbfs._openBlocks[_identifier].Remove(this);
                }
            }

            public VBlockContentStore(ODBFSImpl odbfs, Guid identifier)
            {
                if (odbfs == null)
                {
                    throw new ArgumentNullException("odbfs");
                }
                _odbfs = odbfs;
                _identifier = identifier;
                using(_odbfs._openBlocksLock.EnterWriteLock())
                {
                    List<VBlockContentStore> xOpenBlocks;
                    if(!_odbfs._openBlocks.TryGetValue(identifier, out xOpenBlocks))
                    {
                        throw new Exception("Block not found!");
                    }
                    xOpenBlocks.Add(this);
                }
            }

            public override void Store(ulong index, ArraySegment<byte> buffer)
            {
                using(_odbfs._configurationLock.EnterReadLock())
                {
                    var xBlockConfig = _odbfs._blocks[_identifier];

                    if(xBlockConfig.Parts.Count>1)
                    {
                        throw new Exception("More than 1 blockpart not yet supported!");
                    }

                    _odbfs.StoreVirtualBlock(xBlockConfig.Parts[0].FirstDataBlockNumber + index, buffer);
                }
            }

            public override void Retrieve(ulong index, ArraySegment<byte> buffer)
            {
                using (_odbfs._configurationLock.EnterReadLock())
                {
                    var xBlockConfig = _odbfs._blocks[_identifier];

                    if (xBlockConfig.Parts.Count > 1)
                    {
                        throw new Exception("More than 1 blockpart not yet supported!");
                    }

                    _odbfs.RetrieveVirtualBlock(xBlockConfig.Parts[0].FirstDataBlockNumber + index, buffer);
                }
            }

            public override uint BlockSize
            {
                get
                {
                    return _odbfs._virtualBlockSize;
                }
            }

            public override ulong BlockCount
            {
                get
                {
                    using (_odbfs._configurationLock.EnterReadLock())
                    {
                        var xBlockConfig = _odbfs._blocks[_identifier];

                        return xBlockConfig.TotalLength;
                    }
                }
            }

            public override void DumpCacheInfo(StreamWriter output, string linePrefix)
            {
                throw new NotImplementedException();
            }
        }
    }
}
