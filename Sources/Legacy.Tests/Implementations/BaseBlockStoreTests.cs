using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerWoord.OverDriveStorage.Legacy.Implementations;
using TerWoord.OverDriveStorage.Tests.TestUtilities;

namespace TerWoord.OverDriveStorage.Legacy.Tests.Implementations
{
    [TestClass]
    public class BaseBlockStoreTests
    {
        private class BlockStoreTester : BaseBlockStore
        {
            public List<ulong> BlocksStored = new List<ulong>();
            public List<ulong> BlocksRead = new List<ulong>();
            public override void Store(ulong index, ArraySegment<byte> buffer)
            {
                BlocksStored.Add(index);
                //ByteConverter.WriteBytes(index, buffer, buffer.Offset);
            }

            public override void Retrieve(ulong index, ArraySegment<byte> buffer)
            {
                BlocksRead.Add(index);
                ByteConverter.WriteBytes(index, buffer.Array, buffer.Offset);                
            }

            public override uint BlockSize
            {
                get
                {
                    return BlockSizeSet;
                }
            }

            public uint BlockSizeSet
            {
                get;
                set;
            }

            public override ulong BlockCount
            {
                get
                {
                    return BlockCountSet;
                }
            }

            public ulong BlockCountSet
            {
                get;
                set;
            }

            public int DumpCacheInfoCalled = 0;

            public override void DumpCacheInfo(System.IO.StreamWriter output, string linePrefix)
            {
                DumpCacheInfoCalled++;                
            }
        }

        [TestMethod]
        public void CreateAndDisposeTest()
        {
            var xTester = new BlockStoreTester();
            xTester.Dispose();
        }

        [TestMethod]
        public void CreateAndDisposeTest_DoubleDisposeShouldntError()
        {
            var xTester = new BlockStoreTester();
            xTester.Dispose();
            xTester.Dispose();
        }


        [TestMethod]
        public void TestMultipleStore()
        {
            using (var xTester = new BlockStoreTester())
            {
                xTester.BlockSizeSet = 1;
                xTester.BlockCountSet = 100;
                var xBlocks = new byte[100];
                var xBlocksToStore = new ulong[100];
                for (uint i = 0; i < 100; i++)
                {
                    xBlocksToStore[i] = i;
                }

                xTester.Store(xBlocksToStore, new ArraySegment<byte>(xBlocks));

                CollectionAssert.AreEqual(xBlocksToStore, xTester.BlocksStored);
            }
        }

        [TestMethod]
        public void TestMultipleRetrieve()
        {
            using (var xTester = new BlockStoreTester())
            {
                xTester.BlockSizeSet = 8;
                Assert.AreEqual<uint>(8, xTester.BlockSize);
                xTester.BlockCountSet = 100;
                Assert.AreEqual<ulong>(100, xTester.BlockCount);
                var xBlocks = new byte[800];
                var xBlocksToStore = new ulong[100];
                for (uint i = 0; i < 100; i++)
                {
                    xBlocksToStore[i] = i;
                }

                xTester.Retrieve(xBlocksToStore, new ArraySegment<byte>(xBlocks));
                for (int i = 0; i < 100; i++)
                {
                    Assert.AreEqual<ulong>((ulong)i, ByteConverter.ReadUInt64(xBlocks, i * 8));
                }
            }
        }

        [TestMethod]
        [ExpectedExceptionMessage(Message="Buffer size not correct!")]
        public void TestError_ReadMultipleWrongBufferSize()
        {
            using (var xTester = new BlockStoreTester())
            {
                xTester.BlockSizeSet = 1;
                xTester.BlockCountSet = 1;
                var xBlock = new byte[1];
                xTester.Retrieve(new ulong[] { 2, 3 }, new ArraySegment<byte>(xBlock));
            }
        }

        [TestMethod]
        [ExpectedExceptionMessage(Message = "Buffer size not correct!")]
        public void TestError_WriteMultipleWrongBufferSize()
        {
            using (var xTester = new BlockStoreTester())
            {
                xTester.BlockSizeSet = 1;
                xTester.BlockCountSet = 1;
                var xBlock = new byte[1];
                xTester.Store(new ulong[]{2, 3}, new ArraySegment<byte>(xBlock));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestError_AccessAfterDispose()
        {
            var xTester = new BlockStoreTester();
            xTester.BlockSizeSet = 1;
                xTester.BlockCountSet = 1;
                var xBlock = new byte[1];
                
            xTester.Dispose();
            xTester.Store(new ulong[] { 2, 3 }, new ArraySegment<byte>(xBlock));
        }
    }
}
