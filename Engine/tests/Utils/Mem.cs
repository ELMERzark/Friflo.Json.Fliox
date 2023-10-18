using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace Tests.Utils;

[ExcludeFromCodeCoverage]
public static class Mem
{
    public static long GetAllocatedBytes() {
        return GC.GetAllocatedBytesForCurrentThread();
    }
        
    /// <summary>Assert no allocation were performed</summary>
    public static long AssertNoAlloc(long start) {
        long current    = GC.GetAllocatedBytesForCurrentThread();
        var diff        = current - start;
        if (diff == 0) {
            return current;
        }
        var msg = $"expected no allocation.\n but was: {diff}";
        throw new AssertionException(msg);
    }
    
    /// <summary>Assert allocation of expected bytes</summary>
    public static long AssertAlloc(long start, long expected) {
        long current    = GC.GetAllocatedBytesForCurrentThread();
        var diff        = current - start;
        if (diff == expected) {
            return current;
        }
        var msg = $"expected allocation of {expected} bytes.\n but was: {diff}";
        throw new AssertionException(msg);
    }
    
    public static bool IsDebug => IsDebugInternal();
    
    private static bool IsDebugInternal() {
#if DEBUG
        return true;
#else
        return false;
#endif        
    }

}