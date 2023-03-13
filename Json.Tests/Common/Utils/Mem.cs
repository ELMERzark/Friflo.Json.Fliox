// Copyright (c) Ullrich Praetz. All rights reserved.  
// See LICENSE file in the project root for full license information.


using System;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Tests.Common
{
    /// <summary>
    /// Introduced since TargetFrameworks <b>netstandard2.0</b> does not provide<br/>
    /// <c>GC.GetAllocatedBytesForCurrentThread()</c>
    /// </summary>
    public static class Mem
    {
        public static long GetAllocatedBytes() {
#if NET6_0_OR_GREATER
            return GC.GetAllocatedBytesForCurrentThread();
#else
            return 0;
#endif
        }
        
        public static void AreEqual(long expected, long actual) {
            if (expected == actual) {
                return;
            }
#if NET6_0_OR_GREATER
            var msg = $"allocation expected: {expected}\n but was: {actual}";
            throw new AssertionException(msg);
#endif
        }
        
        public static void InRange(in LongRange expected, long actual) {
            if (expected.min <= actual && actual <= expected.max) {
                return;
            }
#if NET6_0_OR_GREATER
            var msg = $"allocation expected in range: [{expected.min},{expected.max}]\n but was: {actual}";
            throw new AssertionException(msg);
#endif
        }
    }
    
    public readonly struct LongRange {
        public readonly long min;
        public readonly long max;
        
        public LongRange(long min, long max) {
            this.min    = min;
            this.max    = max;
        }
    }
}