using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerWoord.OverDriveStorage.Implementations;

namespace TerWoord.OverDriveStorage.Tests.Implementations.ODBFS
{
    [TestClass]
    public class SimpleSinglePartTests
    {
        [TestMethod]
        public void TestSingleBlock()
        {
            using (var xMemStream = new MemoryStream())
            {
                xMemStream.SetLength(2048);
                // 4 blocks of 512 bytes
                using (var xRawBlockStore = new SimpleStreamBlockStore(xMemStream, 512))
                {
                    ODBFSImpl.Format(xRawBlockStore, 512);
                    using (var xOdbfs = new ODBFSImpl(xRawBlockStore))
                    {
                        Assert.AreEqual(0, xOdbfs.GetVirtualBlocks().Count());
                        var xBlockId = Guid.NewGuid();
                        xOdbfs.CreateNewBlock(xBlockId, 2);
                        var xBuff1 = new byte[512];
                        var xBuff1Seg = new ArraySegment<byte>(xBuff1);
                        var xBuff2 = new byte[512];
                        var xBuff2Seg = new ArraySegment<byte>(xBuff2);
                        using (var xBlock = xOdbfs.OpenBlock(xBlockId))
                        {
                            for (int i = 0; i < xBuff1.Length; i++)
                            {
                                xBuff1[i] = 0xFF;
                            }
                            xBlock.Store(0, xBuff1Seg);
                            xBlock.Store(1, xBuff1Seg);
                            xRawBlockStore.Retrieve(1, xBuff2Seg);
                            Assert.IsTrue(DeduplicatingBlockStore.CompareBlocks(xBuff1, xBuff2,0, 0, 512));
                            xRawBlockStore.Retrieve(2, xBuff2Seg);
                            Assert.IsTrue(DeduplicatingBlockStore.CompareBlocks(xBuff1, xBuff2, 0, 0, 512));
                            xBuff1 = new byte[512];
                            xRawBlockStore.Retrieve(3, xBuff2Seg);
                            Assert.IsTrue(DeduplicatingBlockStore.CompareBlocks(xBuff1, xBuff2, 0, 0, 512));
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TestTwoBlocks()
        {
            using (var xMemStream = new MemoryStream())
            {
                xMemStream.SetLength(2048);
                // 4 blocks of 512 bytes
                using (var xRawBlockStore = new SimpleStreamBlockStore(xMemStream, 512))
                {
                    ODBFSImpl.Format(xRawBlockStore, 512);
                    using (var xOdbfs = new ODBFSImpl(xRawBlockStore))
                    {
                        Assert.AreEqual(0, xOdbfs.GetVirtualBlocks().Count());
                        var xBlock1Id = Guid.NewGuid();
                        var xBlock2Id = Guid.NewGuid();
                        xOdbfs.CreateNewBlock(xBlock1Id, 1);
                        xOdbfs.CreateNewBlock(xBlock2Id, 1);
                        var xBuff1 = new byte[512];
                        var xBuff1Seg = new ArraySegment<byte>(xBuff1);
                        var xBuff2 = new byte[512];
                        var xBuff2Seg = new ArraySegment<byte>(xBuff2);
                        using (var xBlock1 = xOdbfs.OpenBlock(xBlock1Id))
                        {
                            for (int i = 0; i < xBuff1.Length; i++)
                            {
                                xBuff1[i] = 0xAA;
                            }
                            xBlock1.Store(0, xBuff1Seg);
                        }
                        using (var xBlock2 = xOdbfs.OpenBlock(xBlock2Id))
                        {
                            for (int i = 0; i < xBuff1.Length; i++)
                            {
                                xBuff1[i] = 0xBB;
                            }
                            xBlock2.Store(0, xBuff1Seg);
                        }

                        for (int i = 0; i < xBuff1.Length; i++)
                        {
                            xBuff1[i] = 0xAA;
                        }
                        xRawBlockStore.Retrieve(1, xBuff2Seg);
                        Assert.IsTrue(DeduplicatingBlockStore.CompareBlocks(xBuff1, xBuff2, 0, 0, 512));
                        for (int i = 0; i < xBuff1.Length; i++)
                        {
                            xBuff1[i] = 0xBB;
                        }
                        xRawBlockStore.Retrieve(2, xBuff2Seg);
                        Assert.IsTrue(DeduplicatingBlockStore.CompareBlocks(xBuff1, xBuff2, 0, 0, 512));
                        xBuff1 = new byte[512];
                        xRawBlockStore.Retrieve(3, xBuff2Seg);
                        Assert.IsTrue(DeduplicatingBlockStore.CompareBlocks(xBuff1, xBuff2, 0, 0, 512));
                    }
                }
            }
        }
    }
}