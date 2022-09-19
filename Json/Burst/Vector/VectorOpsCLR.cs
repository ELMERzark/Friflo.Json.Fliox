// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if NETCOREAPP3_0 || NETCOREAPP3_0_OR_GREATER

using System.Runtime.Intrinsics.X86;

namespace Friflo.Json.Burst.Vector
{
    /// <summary>
    /// Vector (SIMD) operations for Microsoft CLR
    /// </summary>
    public class VectorOpsCLR : VectorOps
    {
        public override unsafe void Xor(
            byte[] dest,    int destPos,
            byte[] src,     int srcPos,
            byte[] mask,    int maskPos,
            int length)
        {
            int     n = 0;
            const   int vectorSize = 16; // 128 bit
            fixed (byte* destPointer  = dest)
            fixed (byte* srcPointer   = src)
            fixed (byte* maskPointer  = mask)
            {
                var end = length - vectorSize;
                for (; n <= end; n += vectorSize) {
                    var bufferVector        = Sse2.LoadVector128(srcPointer   + srcPos + n);
                    var maskingKeyVector    = Sse2.LoadVector128(maskPointer  + (maskPos + n) % 4);
                    var xor                 = Sse2.Xor(bufferVector, maskingKeyVector);
                    Sse2.Store(destPointer + destPos + n, xor);
                }
            }
            // remaining bytes
            base.Xor(dest, destPos + n, src, srcPos + n, mask, maskPos + n, length - n);
        }

        public override void Populate(byte[] arr) {
            PopulateVector(arr);
        }
    }
}

#endif