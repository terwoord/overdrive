using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerWoord.OverDriveStorage
{
    public interface IBlockManager : IDisposable
    {
        /// <summary>
        /// Reserves a single data block and returns the index for the reserved block.
        /// </summary>
        /// <returns>
        /// The index of the reserved block.
        /// </returns>
        ulong Reserve();
        bool TryReserve(out ulong value);

        /// <summary>
        /// Reserves the specified number of data blocks and returns indexes of the reserved blocks.
        /// </summary>
        /// <param name="count">The number of data blocks to reserve.</param>
        /// <returns>An array of indexes: one index per reserved block.</returns>
        ulong[] Reserve(int count);

        bool IsReserved(ulong index);
        void MarkReserved(ulong index);


        /// <summary>
        /// Frees the block referenced by the given index.
        /// </summary>
        /// <param name="index">The index of the data block to free.</param>
        void Free(ulong index);

        /// <summary>
        /// Fills caches from disk
        /// </summary>
        void PreloadCaches();

        string Id
        {
            get;
            set;
        }

        void Flush();

    } // interface
}