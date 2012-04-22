using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerWoord.OverDriveStorage;

namespace PerfTester
{
    // class to help in testing how much time is spend in the hashmanager
    public class TimeKeepHashManager: IHashManager<uint>
    {
        public void AddBlock(ulong blockIndex, uint crc)
        {
            throw new NotImplementedException();
        }

        public void RemoveBlock(ulong blockIndex, uint crc)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ulong> GetAllBlocksWithCRC32(uint crc)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
