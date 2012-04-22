using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerWoord.OverDriveStorage
{
    public static class BlockStoreExtensions
    {
        public static void Retrieve(this IBlockStore aThis, ulong index, uint count, ArraySegment<byte> buffer)
        {
            var xIndices = new ulong[count];
            for (uint i = 0; i < count; i++)
            {
                xIndices[i] = index + i;
            }
            aThis.Retrieve(xIndices, buffer);
        }

        public static void Store(this IBlockStore aThis, ulong index, uint count, ArraySegment<byte> buffer)
        {
            var xIndices = new ulong[count];
            for (uint i = 0; i < count; i++)
            {
                xIndices[i] = index + i;
            }
            aThis.Store(xIndices, buffer);
        }
    }
}