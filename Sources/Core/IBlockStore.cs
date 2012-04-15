using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TerWoord.OverDriveStorage
{
    /// <summary>
    /// Allows the implementor to manage a raw block store.
    /// </summary>
    public interface IBlockStore: IDisposable
    {
        /// <summary>
        /// Stores updated data to a data block.
        /// </summary>
        /// <param name="buffer">
        /// The block of data to be stored.
        /// </param>
        /// <param name="index">
        /// The index of the existing block to overwrite.
        /// </param>
        void Store(ulong index, ArraySegment<byte> buffer);
        void Store(ulong[] indices, ArraySegment<byte> buffer);

        /// <summary>
        /// Retrieves a data block.
        /// </summary>
        /// <param name="index">The index of the block to retrieve.</param>
        /// <param name="buffer">A buffer into which to retrieve the data block.</param>
        void Retrieve(ulong index, ArraySegment<byte> buffer);
        void Retrieve(ulong[] indices, ArraySegment<byte> buffer);

        /// <summary>
        /// Gets the current block size of the store. The block size of a store is always 
        /// consistent across the store; however, the store may choose to use variable block sizes 
        /// internally. The <c>BlockSize</c> refers to the constant block size used externally.
        /// </summary>
        uint BlockSize { get; }

        /// <summary>
        /// Gets the maximum number of blocks of this store. 
        /// </summary>
        ulong BlockCount
        {
            get;
        }

        void DumpCacheInfo(StreamWriter output, string linePrefix);

        /// <summary>
        /// Fills caches from disk
        /// </summary>
        void PreloadCaches();

        string Id
        {
            get;
            set;
        }
    } // interface
}