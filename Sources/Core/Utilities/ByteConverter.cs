using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerWoord.OverDriveStorage.Utilities
{
    public static class ByteConverter
    {
        //public static void WriteBytes(uint value, byte[] buffer, int offset)
        //{
        //    unsafe
        //    {
        //        fixed (byte* fixedBuffer = &buffer[offset])
        //        {
        //            uint* destination = (uint*)(fixedBuffer);
        //            *destination = value;

        //        } // fixed
        //    }
        //} // function

        public static void WriteBytes(ulong value, byte[] buffer, int offset)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (buffer.Length < (offset + 8))
            {
                throw new ArgumentException("Not enough data", "buffer");
            }
            unchecked
            {
                buffer[offset+0] = (byte)value;
                buffer[offset+1] = (byte)(value >> 8);
                buffer[offset+2] = (byte)(value >> 16);
                buffer[offset+3] = (byte)(value >> 24);
                buffer[offset+4] = (byte)(value >> 32);
                buffer[offset+5] = (byte)(value >> 40);
                buffer[offset+6] = (byte)(value >> 48);
                buffer[offset+7] = (byte)(value >> 56);
                //    return buffer[offset + 7]
                //        | (buffer[offset + 6] << 8)
                //        | (buffer[offset + 5] << 16)
                //        | (buffer[offset + 4] << 24)
                //        | (buffer[offset + 3] << 32)
                //        | (buffer[offset + 2] << 40)
                //        | (buffer[offset + 1] << 48)
                //        | (buffer[offset] << 54);
            }
        }

        //public unsafe static uint ReadUInt32(byte[] buffer, int offset)
        //{
        //    fixed (byte* fixedBuffer = &buffer[offset])
        //    {
        //        uint* destination = (uint*)(fixedBuffer);
        //        return *destination;

        //    } // fixed

        //} // function

        //public unsafe static ushort ReadUInt16(byte[] buffer, int offset)
        //{
        //    fixed (byte* fixedBuffer = &buffer[offset])
        //    {
        //        var destination = (ushort*)(fixedBuffer);
        //        return *destination;

        //    } // fixed

        //} // function

        public static ulong ReadUInt64(byte[] buffer, int offset)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (buffer.Length < (offset + 8))
            {
                throw new ArgumentException("Not enough data", "buffer");
            }
            unchecked
            {
                uint xLow = (uint)((buffer[offset + 3] << 24)
                    | (buffer[offset + 2] << 16)
                    | (buffer[offset + 1] << 8)
                    | (buffer[offset]));
                uint xHigh = (uint)((buffer[offset + 7] << 24)
                | (buffer[offset + 6] << 16)
                | (buffer[offset + 5] << 8)
                | (buffer[offset + 4]));
                //return (ulong)xLow | (((ulong)(uint)xHigh) << 32);
                return ((ulong)xLow) | (((ulong)xHigh) << 32);
            }
        } // function

        //public static Guid ReadGuid(byte[] buffer, int offset)
        //{
        //    var xBuff = new byte[16];
        //    Buffer.BlockCopy(buffer, offset, xBuff, 0, 16);
        //    return new Guid(xBuff);
        //}

        //public static void WriteBytes(Guid value, byte[] buffer, int offset)
        //{
        //    Buffer.BlockCopy(value.ToByteArray(), 0, buffer, offset, 16);
        //}
    } // class
} // namespace