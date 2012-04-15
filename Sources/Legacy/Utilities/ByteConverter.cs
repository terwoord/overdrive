using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerWoord.OverDriveStorage.Legacy
{

    public static class ByteConverter
    {

        public static void WriteBytes(uint value, byte[] buffer, int offset)
        {
            unsafe
            {

                fixed (byte* fixedBuffer = &buffer[offset])
                {
                    uint* destination = (uint*)(fixedBuffer);
                    *destination = value;

                } // fixed
            }
        } // function


        public unsafe static void WriteBytes(ulong value, byte[] buffer, int offset)
        {
            unsafe
            {
                fixed (byte* fixedBuffer = &buffer[offset])
                {
                    ulong* destination = (ulong*)(fixedBuffer);
                    *destination = value;

                } // fixed
            }
        }// function


        public unsafe static uint ReadUInt32(byte[] buffer, int offset)
        {
            fixed (byte* fixedBuffer = &buffer[offset])
            {
                uint* destination = (uint*)(fixedBuffer);
                return *destination;

            } // fixed

        } // function

        public unsafe static ushort ReadUInt16(byte[] buffer, int offset)
        {
            fixed (byte* fixedBuffer = &buffer[offset])
            {
                var destination = (ushort*)(fixedBuffer);
                return *destination;

            } // fixed

        } // function


        public unsafe static ulong ReadUInt64(byte[] buffer, int offset)
        {
            fixed (byte* fixedBuffer = &buffer[offset])
            {
                ulong* destination = (ulong*)(fixedBuffer);
                return *destination;

            } // fixed

        } // function

        public static Guid ReadGuid(byte[] buffer, int offset)
        {
            var xBuff = new byte[16];
            Buffer.BlockCopy(buffer, offset, xBuff, 0, 16);
            return new Guid(xBuff);
        }

        public static void WriteBytes(Guid value, byte[] buffer, int offset)
        {
            Buffer.BlockCopy(value.ToByteArray(), 0, buffer, offset, 16);
        }
    } // class


} // namespace
