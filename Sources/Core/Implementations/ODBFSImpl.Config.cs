using System;
using System.Collections.Generic;
using System.Linq;

namespace TerWoord.OverDriveStorage.Implementations
{
    partial class ODBFSImpl
    {
        private class VirtualBlockConfig
        {
            public Guid Identifier;
            public ulong TotalLength;

            public List<VirtualBlockPart> Parts;
        }

        private class VirtualBlockPart
        {
            public ulong FirstDataBlockNumber;
            public uint BlockCount;
            public VirtualBlockConfig VirtualBlock;
            public ushort PartIndex;
            public ushort TotalPartCount;
        }

        private class MetaGroupHeader
        {
            public ulong BlockId;
            public VirtualBlockPart[] Parts;
        }
    }
}