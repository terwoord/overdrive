using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// This is namespace System, so the extensions are always available
namespace System
{
    public static class Extensions
    {
        public static ulong Sum(this IEnumerable<ulong> aThis)
        {
            return aThis.Aggregate((a, b) => a + b);
        }
    }
}