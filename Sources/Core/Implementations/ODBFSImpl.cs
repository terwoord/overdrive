using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TerWoord.OverDriveStorage.Utilities;

namespace TerWoord.OverDriveStorage.Implementations
{
    public partial class ODBFSImpl: IDisposable
    {
        public const uint ODBFSMagic = 0x0DBF5001;

        public static void Format(IBlockStore blockStore, uint blockSize)
        {
            if (blockSize % blockStore.BlockSize != 0)
            {
                throw new Exception("Virtual BlockSize should be multiple of backend blocksize!");
            }
            var xRawBlockBuff = new byte[blockSize];
            var xRawSeg = new ArraySegment<byte>(xRawBlockBuff);
            ByteConverter.WriteBytes(ODBFSMagic, xRawBlockBuff, 0);
            ByteConverter.WriteBytes(blockSize, xRawBlockBuff, 4);
            var xRawBlocksPerVBlock = blockSize / blockStore.BlockSize;
            var xIds = new ulong[xRawBlocksPerVBlock];
            for(uint i = 0; i < xRawBlocksPerVBlock;i++)
            {
                xIds[i] = i;
            }
            blockStore.Store(xIds, xRawSeg);
        }

        public ODBFSImpl(IBlockStore blockStore)
        {
            if (blockStore == null)
            {
                throw new ArgumentNullException("blockStore");
            }
            _blockStore = blockStore;

            var xBuff = new byte[blockStore.BlockSize];
            blockStore.Retrieve(0, new ArraySegment<byte>(xBuff));

            if (ByteConverter.ReadUInt32(xBuff, 0) != ODBFSMagic)
            {
                throw new Exception("Blockstore doesn't contain an ODBFS system!");
            }

            _virtualBlockSize = ByteConverter.ReadUInt32(xBuff, 4);
            if (_virtualBlockSize % blockStore.BlockSize != 0)
            {
                throw new Exception("Corrupt data!");
            }
            _rawBlocksPerVirtualBlock = _virtualBlockSize / blockStore.BlockSize;
            _metaGroupItemsPerVirtualBlock = (_virtualBlockSize - 32) / 32;
            LoadConfiguration();
        }

        private void RetrieveVirtualBlock(ulong index, ArraySegment<byte> buffer)
        {
            _blockStore.Retrieve(index*_rawBlocksPerVirtualBlock, _rawBlocksPerVirtualBlock, buffer);
        }

        private void StoreVirtualBlock(ulong index, ArraySegment<byte> buffer)
        {
            _blockStore.Store(index * _rawBlocksPerVirtualBlock, _rawBlocksPerVirtualBlock, buffer);
        }

        private bool _disposed;
        public void Dispose()
        {
            if(_disposed)
            {
                return;
            }
            FlushData();
            GC.SuppressFinalize(this);
            _blockStore.Dispose();
            _disposed = true;
        }

        private void FlushData()
        {
            using (_openBlocksLock.EnterReadLock())
            {
                if ((from item in _openBlocks
                     where item.Value.Count > 0
                     select item).Any())
                {
                    throw new Exception("There are still open blocks!");
                }

                SaveConfiguration();
            }
        }

        private readonly IBlockStore _blockStore;
        private readonly uint _virtualBlockSize;
        private readonly uint _rawBlocksPerVirtualBlock;
        private readonly uint _metaGroupItemsPerVirtualBlock; // note, is (VirtualBlockSize - 32) / MetaGroupItemSize(=32)

        private readonly ReaderWriterLocker _configurationLock = new ReaderWriterLocker();
        private readonly Dictionary<Guid, VirtualBlockConfig> _blocks = new Dictionary<Guid, VirtualBlockConfig>();
        private readonly List<MetaGroupHeader> _metaHeaders = new List<MetaGroupHeader>();

        private readonly ReaderWriterLocker _openBlocksLock = new ReaderWriterLocker();
        private readonly Dictionary<Guid, List<VBlockContentStore>> _openBlocks=new Dictionary<Guid,List<VBlockContentStore>>();

        public uint BlockSize
        {
            get
            {
                return _virtualBlockSize;
            }
        }

        private void LoadConfiguration()
        {
            using(_configurationLock.EnterWriteLock())
            {
                var xBuff = new byte[_virtualBlockSize];
                var xBuffSeg = new ArraySegment<byte>(xBuff);
                var xNextMetaGroupBlock = 0UL;
                do
                {
                    RetrieveVirtualBlock(xNextMetaGroupBlock, xBuffSeg);
                    var xMetaGroupHeader = new MetaGroupHeader
                                           {
                                               BlockId = xNextMetaGroupBlock
                                           };
                    var xStartOffset = 0;
                    if (xNextMetaGroupBlock == 0)
                    {
                        xStartOffset = 8;
                    }

                    xNextMetaGroupBlock = ByteConverter.ReadUInt64(xBuff, xStartOffset);
                    xStartOffset = 32;
                    var xParts = new List<VirtualBlockPart>((int)_metaGroupItemsPerVirtualBlock);
                    for (uint i = 0; i < _metaGroupItemsPerVirtualBlock; i++)
                    {
                        var xVBlockId = ByteConverter.ReadGuid(xBuff, xStartOffset);
                        if (xVBlockId != Guid.Empty)
                        {
                            var xVBlockPart = new VirtualBlockPart
                                              {
                                                  TotalPartCount = ByteConverter.ReadUInt16(xBuff, xStartOffset + 16),
                                                  PartIndex = ByteConverter.ReadUInt16(xBuff, xStartOffset + 18),
                                                  FirstDataBlockNumber = ByteConverter.ReadUInt64(xBuff, xStartOffset + 20),
                                                  BlockCount = ByteConverter.ReadUInt32(xBuff, xStartOffset + 28)
                                              };
                            VirtualBlockConfig xBlockConfig;
                            if(!_blocks.TryGetValue(xVBlockId, out xBlockConfig))
                            {
                                xBlockConfig = new VirtualBlockConfig();
                                xBlockConfig.Identifier = xVBlockId;
                                xBlockConfig.Parts=new List<VirtualBlockPart>(4);
                                _blocks.Add(xVBlockId, xBlockConfig);
                            }
                            xBlockConfig.Parts.Add(xVBlockPart);
                        }
                        xStartOffset += 32;
                    }
                    xMetaGroupHeader.Parts = xParts.ToArray();
                    _metaHeaders.Add(xMetaGroupHeader);
                } while (xNextMetaGroupBlock != 0);

                if (_blocks.Count > 0)
                {
                    using (_openBlocksLock.EnterWriteLock())
                    {
                        foreach (var xItem in _blocks)
                        {
                            xItem.Value.TotalLength = (from item in xItem.Value.Parts
                                                       select (ulong)item.BlockCount).Sum();
                            _openBlocks.Add(xItem.Key, new List<VBlockContentStore>());
                        }
                    }
                }
            }
        }

        public IEnumerable<Guid> GetVirtualBlocks()
        {
            using(_configurationLock.EnterReadLock())
            {
                return _blocks.Keys.ToArray();
            }
        }

        public void CreateNewBlock(Guid id, ulong blockCount)
        {
            if (blockCount > UInt32.MaxValue)
            {
                throw new Exception("VirtualBlocks larger than 2^32-1 are not yet supported!");
            }
            using(_configurationLock.EnterWriteLock())
            {
                if (_blocks.ContainsKey(id))
                {
                    throw new Exception("Block already exists!");
                }
                if (blockCount > uint.MaxValue)
                {
                    throw new NotImplementedException("More than 2^32-1 blocks not yet supported at create-time!");
                }

                MetaGroupHeader xGroupHeader = null;
                foreach (var xMetaGroup in _metaHeaders)
                {
                    if (xMetaGroup.Parts.Length < _metaGroupItemsPerVirtualBlock)
                    {
                        xGroupHeader = xMetaGroup;
                        break;
                    }
                }
                if (xGroupHeader == null)
                {
                    throw new Exception("No GroupHeader found with a free slot!");
                }

                var xNextPostValues = (from item in _metaHeaders
                                       where item.Parts.Length > 0
                                       select (from subItem in item.Parts
                                               select subItem.FirstDataBlockNumber + subItem.BlockCount).Max()).ToArray();
                ulong xNextPos;
                if (xNextPostValues.Any())
                {
                    xNextPos = xNextPostValues.Max();
                }
                else
                {
                    xNextPos = (from item in _metaHeaders
                                select item.BlockId + 1).Max();
                }

                if ((((xNextPos + blockCount) * _virtualBlockSize) / _blockStore.BlockSize) > _blockStore.BlockCount)
                {
                    throw new Exception("No free space found!");
                }

                var xNewPartsList = new List<VirtualBlockPart>(xGroupHeader.Parts);

                var xNewPart = new VirtualBlockPart
                               {
                                   BlockCount = (uint)blockCount,
                                   FirstDataBlockNumber = xNextPos,
                                   PartIndex = 0,
                                   TotalPartCount = 1
                               };
                var xVBlock = new VirtualBlockConfig
                              {
                                  Identifier = id,
                                  Parts = new List<VirtualBlockPart>
                                          {
                                              xNewPart
                                          },
                                  TotalLength = blockCount
                              };
                xNewPart.VirtualBlock = xVBlock;
                xNewPartsList.Add(xNewPart);
                xGroupHeader.Parts = xNewPartsList.ToArray();
                _blocks.Add(id, xVBlock);
            }
            using(_openBlocksLock.EnterWriteLock())
            {
                _openBlocks.Add(id, new List<VBlockContentStore>());
            }
        }

        public IBlockStore OpenBlock(Guid id)
        {
            return new VBlockContentStore(this, id);
        }

        private void SaveConfiguration()
        {
            using (_configurationLock.EnterReadLock())
            {
                for(int i = 0; i < _metaHeaders.Count;i++)
                {
                    // for now, each iteration gets a new buffer, to ensure it's empty
                    var xBuff = new byte[_virtualBlockSize];
                    var xBuffSeg = new ArraySegment<byte>(xBuff);

                
                    var xCurrentHeader = _metaHeaders[i];
                    MetaGroupHeader xNextHeader = null;
                    if (i < (_metaHeaders.Count - 1))
                    {
                        xNextHeader = _metaHeaders[i + 1];
                    }

                    var xStartOffset = 0;
                    if (xCurrentHeader.BlockId == 0)
                    {
                        // emit store header, consisting of magic bytes and blocksize
                        ByteConverter.WriteBytes(ODBFSMagic, xBuff, 0);
                        ByteConverter.WriteBytes(_virtualBlockSize, xBuff, 4);
                        xStartOffset = 8;
                    }

                    if (xNextHeader != null)
                    {
                        ByteConverter.WriteBytes(xNextHeader.BlockId, xBuff, xStartOffset);
                    }
                    xStartOffset = 32;
                    for (int j = 0; j < xCurrentHeader.Parts.Length; j++)
                    {
                        var xVBlockPart = xCurrentHeader.Parts[j];
                        ByteConverter.WriteBytes(xVBlockPart.VirtualBlock.Identifier, xBuff, xStartOffset + 0);
                        ByteConverter.WriteBytes(xVBlockPart.TotalPartCount, xBuff, xStartOffset + 16);
                        ByteConverter.WriteBytes(xVBlockPart.PartIndex, xBuff, xStartOffset + 18);
                        ByteConverter.WriteBytes(xVBlockPart.FirstDataBlockNumber, xBuff, xStartOffset + 20);
                        ByteConverter.WriteBytes(xVBlockPart.BlockCount, xBuff, xStartOffset + 28);
                        xStartOffset += 32;
                    }
                    StoreVirtualBlock(xCurrentHeader.BlockId, xBuffSeg);
                }
            }
        }
    }
}