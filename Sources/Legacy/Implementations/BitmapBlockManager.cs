using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TerWoord.OverDriveStorage.Legacy.Utilities;

namespace TerWoord.OverDriveStorage.Legacy.Implementations
{
    public class BitmapBlockManager: IBlockManager
    {
        private readonly FileBitmap mBitmap;

        public BitmapBlockManager(Stream bitmap, ulong storageBlockCount, uint blockSize)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }
            var xExpectedBitmapSize = storageBlockCount * blockSize;
            if ((ulong)bitmap.Length != xExpectedBitmapSize)
            {
                throw new Exception(string.Format("Wrong Bitmap stream size! (Expected = {0}, Actual = {1})", xExpectedBitmapSize, bitmap.Length));
            }
            if (xExpectedBitmapSize < blockSize)
            {
                mBitmap = new FileBitmap(bitmap, 1, (uint)xExpectedBitmapSize);
            }
            else
            {
                mBitmap = new FileBitmap(bitmap, storageBlockCount, blockSize);
            }
        }

        public BitmapBlockManager(IBlockStore baseStore, Stream bitmap)
        {
            if (baseStore == null)
            {
                throw new ArgumentNullException("baseStore");
            }
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }
            var xExpectedBitmapSize = ((baseStore.BlockCount * baseStore.BlockSize) / 8U);
            if((ulong)bitmap.Length != xExpectedBitmapSize)
            {
                throw new Exception(string.Format("Wrong Bitmap stream size! (Expected = {0}, Actual = {1})", xExpectedBitmapSize, bitmap.Length));
            }
            if (xExpectedBitmapSize < baseStore.BlockSize)
            {
                mBitmap = new FileBitmap(bitmap, 1, (uint)xExpectedBitmapSize);
            }
            else
            {
                mBitmap = new FileBitmap(bitmap, baseStore.BlockCount / 8, baseStore.BlockSize);
            }
        }

        private bool mDisposed;
        public void Dispose()
        {
            if(mDisposed)
            {
                return;
            }
            mDisposed = true;
            GC.SuppressFinalize(this);
            mBitmap.Dispose();
        }

        public ulong Reserve()
        {
            return mBitmap.GetAndReserveNextFreeBit();
        }

        public bool TryReserve(out ulong value)
        {
            return mBitmap.TryGetAndReserveNextFreeBit(out value);
        }

        public ulong[] Reserve(int count)
        {
            var xResult = new ulong[count];
            for(int i = 0; i < count;i++)
            {
                xResult[i] = Reserve();
            }
            return xResult;
        }

        public bool IsReserved(ulong index)
        {
            return mBitmap[index];
        }

        public void Free(ulong index)
        {
            mBitmap[index] = false;
        }

        public void MarkReserved(ulong index)
        {
            mBitmap[index] = true;
        }

        public void PreloadCaches()
        {
            mBitmap.PreloadCaches();
        }

        private string mId;
        public string Id
        {
            get
            {
                return mId;
            }
            set
            {
                mId = value;
                mBitmap.Id = value + "-Bitmap";
            }
        }
    }
}