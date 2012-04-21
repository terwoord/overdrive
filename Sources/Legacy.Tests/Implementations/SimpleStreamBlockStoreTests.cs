using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerWoord.OverDriveStorage.Implementations;
using TerWoord.OverDriveStorage.Tests.TestUtilities;

namespace TerWoord.OverDriveStorage.Legacy.Tests.Implementations
{
    [TestClass]
    public class SimpleStreamBlockStoreTests
    {
        [TestMethod]
        public void DoTest()
        {
            using (var xBackend = new MemoryStream())
            {
                xBackend.SetLength(2);
                var xStore = new SimpleStreamBlockStore(xBackend, 1);
                Assert.AreEqual(1u, xStore.BlockSize);
                Assert.AreEqual(2u, xStore.BlockCount);
                var xBuff = new byte[1];
                xBuff[0] = 1;
                var xArraySegment = new ArraySegment<byte>(xBuff);
                xStore.Retrieve(0, xArraySegment);
                Assert.AreEqual(0, xBuff[0]);
                // now store a byte in block 0, and read back both 0 and 1
                xBuff[0] = 2;
                xStore.Store(1, xArraySegment);

                xStore.Retrieve(0, xArraySegment);
                Assert.AreEqual(0, xBuff[0]);

                xStore.Retrieve(1, xArraySegment);
                Assert.AreEqual(2, xBuff[0]);
            }
        }

        [TestMethod]
        [ExpectedArgumentNullExceptionAttribute(Argument = "backend")]
        public void Test_StoreThrowsErrorWithoutBackend()
        {
            IBlockStore xStore = new SimpleStreamBlockStore(null as Stream, 1);
        }

        [TestMethod]
        [ExpectedExceptionMessage(ExceptionType = typeof(Exception), Message = "File size must be multiple of BlockSize!")]
        public void Test_StoreThrowsErrorWithWrongSize()
        {
            using (var xMS = new MemoryStream(new byte[2]))
            {
                var xStore = new SimpleStreamBlockStore(xMS, 4);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void Test_StoreThrowsErrorWithWrongIndexSizeInRetrieve()
        {
            using (var xMS = new MemoryStream(new byte[2]))
            {
                IBlockStore xStore = new SimpleStreamBlockStore(xMS, 2);
                var xBuff = new byte[2];
                var xSeg = new ArraySegment<byte>(xBuff);
                xStore.Retrieve(1, xSeg);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void Test_StoreThrowsErrorWithWrongIndexSizeInStore()
        {
            using (var xMS = new MemoryStream(new byte[2]))
            {
                IBlockStore xStore = new SimpleStreamBlockStore(xMS, 2);
                var xBuff = new byte[2];
                var xSeg = new ArraySegment<byte>(xBuff);
                xStore.Store(1, xSeg);
            }
        }

        [TestMethod]
        [ExpectedExceptionMessage(ExceptionType = typeof(Exception), Message = "Buffer is not of correct size")]
        public void Test_StoreThrowsErrorWithWrongBufferSizeInStore()
        {
            using (var xMS = new MemoryStream(new byte[2]))
            {
                IBlockStore xStore = new SimpleStreamBlockStore(xMS, 2);
                var xBuff = new byte[1];
                var xSeg = new ArraySegment<byte>(xBuff);
                xStore.Store(0, xSeg);
            }
        }

        [TestMethod]
        [ExpectedExceptionMessage(ExceptionType = typeof(Exception), Message = "Buffer is not of correct size")]
        public void Test_StoreThrowsErrorWithWrongBufferSizeInRetrieve()
        {
            using (var xMS = new MemoryStream(new byte[2]))
            {
                IBlockStore xStore = new SimpleStreamBlockStore(xMS, 2);
                var xBuff = new byte[1];
                var xSeg = new ArraySegment<byte>(xBuff);
                xStore.Retrieve(0, xSeg);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Test_StoreThrowsErrorAfterDisposed()
        {
            var xBuff = new byte[2];
            var xSeg = new ArraySegment<byte>(xBuff);
            var xBlock = new SimpleStreamBlockStore(new MemoryStream(new byte[2]), 2);
            using (xBlock)
            {
                xBlock.Retrieve(0, xSeg);
            }
            xBlock.Retrieve(0, xSeg);
        }

        [TestMethod]
        public void TestWithFileStream()
        {
            var xTempFile = Path.GetTempFileName();
            try
            {
                using (var xFS = new FileStream(xTempFile, FileMode.Open))
                {
                    xFS.SetLength(2);
                }
                using (var xStore = new SimpleStreamBlockStore(xTempFile, 2))
                {
                    Assert.AreEqual(2u, xStore.BlockSize);
                    Assert.AreEqual(1ul, xStore.BlockCount);
                }
            }
            finally
            {
                File.Delete(xTempFile);
            }
        }
    }
}