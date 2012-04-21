using System;
using System.IO;
using System.Linq;

namespace TerWoord.OverDriveStorage
{
    public interface IUsageCountStore : IDisposable
    {
        ulong EntryCount
        {
            get;
        }

        void Increment(ulong index);

        void Decrement(ulong index);

        bool HasEntriesWhichReachedZero
        {
            get;
        }

        ulong[] GetZeroReachedEntries();

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
    }
}