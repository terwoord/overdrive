using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerWoord.OverDriveStorage.Tests.TestUtilities;
using TerWoord.OverDriveStorage.Utilities;

namespace TerWoord.OverDriveStorage.Tests.Utilities
{
    [TestClass]
    public class BitArrayTests
    {
        [TestMethod]
        public void Test31thBit()
        {
            var xBitArray = new BitArray(new byte[4]);
            Assert.IsFalse(xBitArray[30]);
            Assert.IsFalse(xBitArray[31]);
            Assert.IsFalse(xBitArray[32]);

            xBitArray[31] = true;
            Assert.IsFalse(xBitArray[30]);
            Assert.IsTrue(xBitArray[31]);
            Assert.IsFalse(xBitArray[32]);
        }

        [TestMethod]
        public void Test32thBit()
        {
            var xBitArray = new BitArray(new byte[4]);
            Assert.IsFalse(xBitArray[31]);
            Assert.IsFalse(xBitArray[32]);
            Assert.IsFalse(xBitArray[33]);

            xBitArray[32] = true;
            Assert.IsFalse(xBitArray[31]);
            Assert.IsTrue(xBitArray[32]);
            Assert.IsFalse(xBitArray[33]);
        }

        [TestMethod]
        [ExpectedArgumentNullException(Argument = "bytes")]
        public void TestError_NoByteArray()
        {
            var xArray = new BitArray(null);
        }
    }
}