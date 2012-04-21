using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerWoord.OverDriveStorage.Utilities;

namespace TerWoord.OverDriveStorage.Tests.Utilities
{
    [TestClass]
    public class ByteConverterTests
    {
        [TestMethod]
        public void TestWriteBytesUInt64()
        {
            var xValue = 0x0102030405060708UL;
            var xExpected = BitConverter.GetBytes(xValue);
            var xActual = new byte[8];
            ByteConverter.WriteBytes(xValue, xActual, 0);
            CollectionAssert.AreEqual(xExpected, xActual);
        }

        [TestMethod]
        public void TestReadUInt64()
        {
            var xValue = 0x0102030405060708UL;
            var xArray = BitConverter.GetBytes(xValue);
            var xActual = ByteConverter.ReadUInt64(xArray, 0);
            Assert.AreEqual<ulong>(xValue, xActual);
        }
    }
}