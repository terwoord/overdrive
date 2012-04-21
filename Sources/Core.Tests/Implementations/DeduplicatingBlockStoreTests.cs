using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerWoord.OverDriveStorage.Implementations;

namespace TerWoord.OverDriveStorage.Tests.Implementations
{
    [TestClass]
    public class DeduplicatingBlockStoreTests
    {
        [TestMethod]
        public void DoTest_4096()
        {
            using (var xStore = CreateStore(4096, 32768, 32768))
            {
                var xBlockBuffer = new byte[4096];
                var xBlockBufferSeg = new ArraySegment<byte>(xBlockBuffer);

                for (int i = 0; i < 4096; i++)
                {
                    xBlockBuffer[i] = (byte)i;
                }

                Assert.IsFalse(xStore.IsReserved(0));
                xStore.Store(0, xBlockBufferSeg);
                Assert.IsTrue(xStore.IsReserved(0));
                xStore.Store(1, xBlockBufferSeg);

                Assert.IsTrue(mRawBlockManager.IsReserved(0));
                Assert.IsFalse(mRawBlockManager.IsReserved(1));
            }
        }

        [TestMethod]
        public void DoTest_2048()
        {
            using (var xStore = CreateStore(2048, 32768, 32768))
            {
            }
        }

        [TestMethod]
        public void DoTest_512()
        {
            using (var xStore = CreateStore(512, 32768, 32768))
            {
            }
        }

        private const string StoreSubdir = "OverdriveStore";

        private IBlockManager mRawBlockManager;

        private DeduplicatingBlockStore CreateStore(uint blockSize, ulong virtualBlockCount, ulong rawBlockCount)
        {
            var xBaseDir = Path.Combine(Environment.CurrentDirectory, StoreSubdir);
            if (Directory.Exists(xBaseDir))
            {
                Directory.Delete(xBaseDir, true);
            }
            Directory.CreateDirectory(xBaseDir);

            var xRawStoreSize = rawBlockCount * blockSize;

            var xRawBlockStoreFS = new FileStream(Path.Combine(xBaseDir, "RawBlocks.bin"), FileMode.CreateNew);
            xRawBlockStoreFS.SetLength((long)xRawStoreSize);
            var xRawBlockStore = new SimpleStreamBlockStore(xRawBlockStoreFS, blockSize);

            var xRawBlockManagerFS = new FileStream(Path.Combine(xBaseDir, "RawBlockBitmap.bin"), FileMode.CreateNew);
            xRawBlockManagerFS.SetLength((long)(xRawBlockStore.BlockCount / 8));
            mRawBlockManager = new BitmapBlockManager(xRawBlockManagerFS, (ulong)(xRawBlockManagerFS.Length / blockSize), blockSize);

            var xVirtualBlockManagerFS = new FileStream(Path.Combine(xBaseDir, "VirtualBlocksBitmap.bin"), FileMode.CreateNew);
            xVirtualBlockManagerFS.SetLength((long)(virtualBlockCount /8 ));
            var xVirtualBlockManager = new BitmapBlockManager(xVirtualBlockManagerFS, virtualBlockCount / blockSize / 8, blockSize);

            var xVirtualBlockStoreFS = new FileStream(Path.Combine(xBaseDir, "VirtualBlocks.bin"), FileMode.CreateNew);
            xVirtualBlockStoreFS.SetLength((long)(virtualBlockCount * 8));
            var xVirtualBlockStore = new SimpleStreamBlockStore(xVirtualBlockStoreFS, 8);

            var xRawBlockUsageCounterFS = new FileStream(Path.Combine(xBaseDir, "RawBlockUsageCounts.bin"), FileMode.CreateNew);
            xRawBlockUsageCounterFS.SetLength((long)(rawBlockCount * 8));
            var xRawBlockUsageCounterStore = new SimpleStreamBlockStore(xRawBlockUsageCounterFS, blockSize);
            var xRawBlockUsageCounter = new SimpleUsageCountStore(xRawBlockUsageCounterStore);

            var xHashesDir = Path.Combine(xBaseDir, "Hashes");
            SimpleHashManager.Create(xHashesDir);
            var xHashManager = new SimpleHashManager(xHashesDir);

            return new DeduplicatingBlockStore(xVirtualBlockManager, xVirtualBlockStore, xRawBlockStore, mRawBlockManager, virtualBlockCount,
                xRawBlockUsageCounter, xHashManager);
        }
    }
}