using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerWoord.OverDriveStorage
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T">The type of hash to use</typeparam>
    public interface IHashManager<T> : IDisposable
    {
        void AddBlock(ulong blockIndex, T crc);

        void RemoveBlock(ulong blockIndex, T crc);

        IEnumerable<ulong> GetAllBlocksWithCRC32(T crc);
    }
}