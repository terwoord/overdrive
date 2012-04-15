using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerWoord.OverDriveStorage.Legacy.Utilities;

namespace TerWoord.OverDriveStorage.Legacy
{
    public interface IHashManager: IDisposable
    {
        void AddBlock(ulong blockIndex, uint crc);
        void RemoveBlock(ulong blockIndex, uint crc);
        RawLinkedList<ulong> GetAllBlocksWithCRC32(uint crc);
    }

    public interface IExperimentalHashManager : IDisposable
    {
        void OpenCrcGroup(byte group);
        void CloseCrcGroup(byte group);

        void AddBlock(ulong blockIndex, uint crc);
        void RemoveBlock(ulong blockIndex, uint crc);
        RawList<ulong?> GetAllBlocksWithCRC32(uint crc);
        void DoneWithAllBlocksList(uint crc);

    }
}