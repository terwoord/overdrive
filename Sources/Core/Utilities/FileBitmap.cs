using System;
using System.IO;
using System.Linq;

namespace TerWoord.OverDriveStorage.Utilities
{
    public class FileBitmap : IDisposable
    {
        private class CacheEntry
        {
            public CacheEntry(BitArray bits, byte[] bytes)
            {
                Bits = bits;
                Bytes = bytes;
                BytesIsStale = false;
                IsDirty = false;
            }

            public readonly BitArray Bits;
            public readonly byte[] Bytes;
            public bool BytesIsStale = false;
            public bool IsDirty;
        }

        private bool mDisposed = false;
        private readonly uint mBlockSize = 512;
        private readonly uint mBlockBitsPerBlock = 512 * 8;
        private readonly ulong mBlockCount;
        private readonly ulong mStoreBlockCount;
        private readonly Stream mBackend;
        private readonly SimpleRecentItemCache<CacheEntry> mCache = new SimpleRecentItemCache<CacheEntry>();

        public FileBitmap(string file, ulong storageBlockCount, uint blockSize)
            : this(new FileStream(file, FileMode.Open), storageBlockCount, blockSize)
        {
        }

        public FileBitmap(Stream stream, ulong storageBlockCount, uint blockSize)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (blockSize < 8 || (blockSize % 8 != 0))
            {
                throw new Exception("blockSize should be 8, or a multiple of 8");
            }
            //mBackend = new RawBlockStore(file, blockSize, blockCount);
            mBackend = stream;
            mBlockSize = blockSize;
            mBlockBitsPerBlock = blockSize * 8;
            mStoreBlockCount = storageBlockCount;
            mBlockCount = mBlockBitsPerBlock * storageBlockCount;

            mCache.OnCacheMiss = OnCacheMiss;
            mCache.ItemRemovedFromCache = OnCacheItemRemoved;
        }

        private void OnCacheItemRemoved(ulong key, CacheEntry entry)
        {
            if (entry.IsDirty)
            {
                if (entry.BytesIsStale)
                {
                    entry.Bits.CopyTo(entry.Bytes, 0);
                }
                mBackend.Position = (long)(key * mBlockSize);
                mBackend.Write(entry.Bytes, 0, (int)mBlockSize);
            }
        }

        private CacheEntry OnCacheMiss(ulong key)
        {
            var xBytes = new byte[mBlockSize];
            mBackend.Position = (long)(key * mBlockSize);
            mBackend.Read(xBytes, 0, (int)mBlockSize);

            var xResult = new CacheEntry(new BitArray(xBytes), xBytes);
            return xResult;
        }

        public static void CreateBitmap(string file, long blockCount, uint blockSize)
        {
            using (var xFile = new FileStream(file, FileMode.CreateNew))
            {
                xFile.SetLength(blockCount * blockSize);
            }
        }

        public void Dispose()
        {
            if (mDisposed)
            {
                throw new ObjectDisposedException("FileBitmap");
            }
            mDisposed = true;
            GC.SuppressFinalize(this);
            mCache.Dispose();
            mBackend.Dispose();
        }

        /// <summary>
        /// Number of Blocks of raw data to keep in memory
        /// </summary>
        public uint CacheCapacity
        {
            get
            {
                return mCache.CacheCapacity;
            }
            set
            {
                mCache.CacheCapacity = value;
            }
        }

        public uint CacheCount
        {
            get
            {
                return mCache.CacheCount;
            }
        }

        private ulong mZeroBitScanStartPageIdx = 0;
        private uint mZeroBitScanStartLongIdx = 0;

        private ulong mPreviousZeroBit = 0;

        public ulong ZeroBitsReturned = 0;

        private readonly object _lock = new object();

        public bool TryGetAndReserveNextFreeBit(out ulong result)
        {
            lock (_lock)
            {
                for (ulong xStoreBlockIdx = mZeroBitScanStartPageIdx; xStoreBlockIdx < mStoreBlockCount; xStoreBlockIdx++)
                {
                    ulong firstIndexOfZeroBit;
                    if (TryGetAndReserveFirstZeroBit(xStoreBlockIdx, out firstIndexOfZeroBit))
                    {
                        ZeroBitsReturned++;
                        mPreviousZeroBit = firstIndexOfZeroBit;
                        this[mPreviousZeroBit] = true;
                        result = mPreviousZeroBit;
                        return true;
                    }
                }
                if (mZeroBitScanStartPageIdx != 0)
                {
                    for (ulong xStoreBlockIdx = 0; xStoreBlockIdx < mZeroBitScanStartPageIdx; xStoreBlockIdx++)
                    {
                        ulong firstIndexOfZeroBit;
                        if (TryGetAndReserveFirstZeroBit(xStoreBlockIdx, out firstIndexOfZeroBit))
                        {
                            ZeroBitsReturned++;
                            mPreviousZeroBit = firstIndexOfZeroBit;
                            this[mPreviousZeroBit] = true;
                            result = mPreviousZeroBit;
                            return true;
                        }
                    }
                }
            }
            result = 0;
            return false;
        }

        public ulong GetAndReserveNextFreeBit()
        {
            ulong xResult;
            if (!TryGetAndReserveNextFreeBit(out xResult))
            {
                throw new Exception("Not found!");
            }
            return xResult;
        }

        private bool TryGetAndReserveFirstZeroBit(ulong xStoreBlockIdx, out ulong firstIndexOfZeroBit)
        {
            firstIndexOfZeroBit = 0;
            var xEntry = mCache[(ulong)xStoreBlockIdx];
            var xStoreBlockIdxBase = xStoreBlockIdx * mBlockBitsPerBlock;

            var xLongBuffStartScan = 0U;
            if (xStoreBlockIdx == mZeroBitScanStartPageIdx)
            {
                xLongBuffStartScan = mZeroBitScanStartLongIdx;
                xLongBuffStartScan = (xLongBuffStartScan / 8) * 8;
            }

            var xLongBuff = xEntry.Bits.Array;
            var xLongBuffCount = (mBlockSize / 8) - xLongBuffStartScan;
            if (mBlockSize % 8 != 0)
            {
                xLongBuffCount++;
            }
            var xTotalCount = xLongBuffStartScan + xLongBuffCount;
            for (uint xLongBuffIdx = xLongBuffStartScan; xLongBuffIdx < xTotalCount; xLongBuffIdx++)
            {
                var xLong = xLongBuff[xLongBuffIdx];
                if (xLong == ulong.MaxValue)
                {
                    continue;
                }
                var xBitIdxBase = xStoreBlockIdxBase + (xLongBuffIdx * 64UL);
                for (int i = 0; i < 64; i++)
                {
                    if ((xLong & (1UL << i)) == 0)
                    {
                        var xBitIdx = xBitIdxBase + (uint)i;
                        mZeroBitScanStartPageIdx = xStoreBlockIdx;
                        mZeroBitScanStartLongIdx = xLongBuffIdx;
                        firstIndexOfZeroBit = xBitIdx;
                        if (xBitIdx % mBlockBitsPerBlock == (mBlockBitsPerBlock - 1))
                        {
                            mZeroBitScanStartPageIdx++;
                            mZeroBitScanStartLongIdx = 0;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool this[ulong index]
        {
            get
            {
                //if (index >= mBlockCount)
                //{
                //    throw new ArgumentOutOfRangeException("index");
                //}
                lock (_lock)
                {
                    var xCacheBitmapIdx = index / mBlockBitsPerBlock;
                    var xBitmapIdx = index % mBlockBitsPerBlock;
                    var xEntry = mCache[xCacheBitmapIdx];
                    return xEntry.Bits[(int)xBitmapIdx];
                }
            }
            set
            {
                //if (index >= mBlockCount)
                //{
                //    throw new ArgumentOutOfRangeException("index");
                //}

                lock (_lock)
                {
                    var xCacheBitmapIdx = index / mBlockBitsPerBlock;
                    var xBitmapIdx = index % mBlockBitsPerBlock;

                    var xCacheEntry = mCache[xCacheBitmapIdx];
                    xCacheEntry.Bits[(int)xBitmapIdx] = value;
                    xCacheEntry.BytesIsStale = true;
                    xCacheEntry.IsDirty = true;
                }
            }
        }

        public void PreloadCaches()
        {
            for (int i = 0; i < mCache.CacheCapacity; i++)
            {
                mCache[(ulong)i].ToString();
            }
        }

        public void ClearCache()
        {
            mCache.Clear();
        }

        public string Id
        {
            get;
            set;
        }
    }
}