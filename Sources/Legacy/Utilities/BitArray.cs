using System;
using System.Linq;

namespace TerWoord.OverDriveStorage.Legacy.Utilities
{
    public class BitArray
    {
        public readonly ulong[] Array;
        private readonly int mCount;

        public BitArray(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            var xCapacity = (bytes.Length / 8) + 1;
            Array = new ulong[xCapacity];
            mCount = bytes.Length * 8;

            unsafe
            {
                fixed(ulong* xArrayPtr = Array)
                {
                    fixed(byte* xBytesPtr = bytes)
                    {
                        var xArrayBytePtr = (byte*)xArrayPtr;
                        for(int i = 0; i < bytes.Length;i++)
                        {
                            xArrayBytePtr[i] = xBytesPtr[i];
                        }
                    }
                }
            }
        }

        public bool this[int index]
        {
            get
            {
                return (Array[index / 64] & 1UL << index % 64) != 0;
            }
            set
            {
                var xArrayIdx = index / 64;
                var xBitIdx = index % 64;
                var xValue = Array[xArrayIdx];
                if(value )
                {
                    xValue |= 1UL << xBitIdx;
                }
                else
                {
                    xValue &= ~(1UL << xBitIdx);
                }
                Array[xArrayIdx] = xValue;
            }
        }

        public void CopyTo(byte[] array, int index)
        {
            Buffer.BlockCopy(Array, 0, array, index, array.Length);
        }
    }
}