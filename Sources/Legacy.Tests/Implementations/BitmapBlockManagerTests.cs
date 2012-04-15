using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerWoord.OverDriveStorage.Legacy.Implementations;
using TerWoord.OverDriveStorage.Tests.TestUtilities;

namespace TerWoord.OverDriveStorage.Tests.Implementations
{
    [TestClass]
    public class BitmapBlockManagerTests
    {
        [TestMethod]
        public void DoTestMarkReserved()
        {
            using (var xMSStore = new MemoryStream(new byte[32768]))
            {
                using(var xMSBitmap = new MemoryStream(new byte[8]))
                {
                    using(var xBlockStore = new SimpleStreamBlockStore(xMSStore, 512))
                    {
                        using (var xBitmapBlockManager = new BitmapBlockManager(xMSBitmap, 1, 8))
                        {
                            Assert.IsFalse(xBitmapBlockManager.IsReserved(0));
                            //var xBit = xBitmapBlockManager.Reserve();
                            xBitmapBlockManager.MarkReserved(0);
                            Assert.IsTrue(xBitmapBlockManager.IsReserved(0));
                            xBitmapBlockManager.Free(0);
                            Assert.IsFalse(xBitmapBlockManager.IsReserved(0));

                        }
                    }
                }
            }
        }

        [TestMethod]
        public void DoTestMarkReserve()
        {
            using (var xMSStore = new MemoryStream(new byte[32768]))
            {
                using (var xMSBitmap = new MemoryStream(new byte[8]))
                {
                    using (var xBlockStore = new SimpleStreamBlockStore(xMSStore, 512))
                    {
                        using (var xBitmapBlockManager = new BitmapBlockManager(xMSBitmap, 1,8))
                        {
                            Assert.IsFalse(xBitmapBlockManager.IsReserved(0));
                            var xBit = xBitmapBlockManager.Reserve();
                            Assert.IsTrue(xBitmapBlockManager.IsReserved(xBit));
                            xBitmapBlockManager.Free(xBit);
                            Assert.IsFalse(xBitmapBlockManager.IsReserved(xBit));

                        }
                    }
                }
            }
        }

        [TestMethod]
        [ExpectedArgumentNullException(Argument="baseStore")]
        public void Test_ThrowErrorWithEmptyBaseStore()
        {
            using (var xMSBitmap = new MemoryStream(new byte[8]))
            {
                using (var xBitmapBlockManager = new BitmapBlockManager(null, xMSBitmap))
                {
                }
            }
        }

        [TestMethod]
        [ExpectedArgumentNullException(Argument = "bitmap")]
        public void Test_ThrowErrorWithEmptyBitmapStream()
        {
            using (var xMSStore = new MemoryStream(new byte[32768]))
            {
                using (var xBlockStore = new SimpleStreamBlockStore(xMSStore, 512))
                {
                    using (var xBitmapBlockManager = new BitmapBlockManager(xBlockStore, null))
                    {
                    }
                }
            }
        }

        [TestMethod]
        [ExpectedExceptionMessage(ExceptionType = typeof(Exception), Message = "Wrong Bitmap stream size! (Expected = 8, Actual = 7)")]
        public void Test_ChecksForBitmapSize()
        {
            using (var xMSStore = new MemoryStream(new byte[32768]))
            {
                using (var xMSBitmap = new MemoryStream(new byte[7]))
                {
                    using (var xBlockStore = new SimpleStreamBlockStore(xMSStore, 512))
                    {
                        using (var xBitmapBlockManager = new BitmapBlockManager(xMSBitmap, 1, 8))
                        {
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void Test_ChecksDisposeWhenDisposed()
        {
            using (var xMSStore = new MemoryStream(new byte[32768]))
            {
                using (var xMSBitmap = new MemoryStream(new byte[8]))
                {
                    using (var xBlockStore = new SimpleStreamBlockStore(xMSStore, 512))
                    {
                        var xBitmapBlockManager = new BitmapBlockManager(xMSBitmap, 1, 8);
                        xBitmapBlockManager.Dispose();
                        xBitmapBlockManager.Dispose();
                    }
                }
            }
        }

        [TestMethod]
        public void Test_MultipleReserve()
        {
            using (var xMSStore = new MemoryStream(new byte[32768]))
            {
                using (var xMSBitmap = new MemoryStream(new byte[8]))
                {
                    using (var xBlockStore = new SimpleStreamBlockStore(xMSStore, 512))
                    {
                        using (var xBitmapBlockManager = new BitmapBlockManager(xMSBitmap, 1, 8))
                        {
                            var xResult = xBitmapBlockManager.Reserve(3);
                            Assert.AreEqual(3, xResult.Length);
                            // todo: how to test better?
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void Test_MarkReservedAfterFullFillAndSomeFree()
        {
            using (var xMSBitmap = new MemoryStream(new byte[8]))
            {
                using (var xBitmap = new BitmapBlockManager(xMSBitmap, 1, 8))
                {
                    var xReservations = xBitmap.Reserve(64);
                    Assert.AreEqual(64, xReservations.Length);
                    ulong xReservation;
                    Assert.IsFalse(xBitmap.TryReserve(out xReservation));
                    xBitmap.Free(xReservations[0]);
                    Assert.IsTrue(xBitmap.TryReserve(out xReservation));
                    Assert.IsFalse(xBitmap.TryReserve(out xReservation));
                }
            }
        }
    }
}