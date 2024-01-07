﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Tests.Utils;

public static class TestUtils
{
    private static bool IsInUnitTest { get; }
    
    public static string GetBasePath(string folder = "")
    {
        // remove folder like ".bin/Debug/net6.0" which is added when running unit tests
        var projectFolder   = IsInUnitTest ?  "/../../../" : "/";
        string baseDir      = Directory.GetCurrentDirectory() + projectFolder;
        baseDir = Path.GetFullPath(baseDir + folder);
        return baseDir;
    }
        
    static TestUtils()
    {
        var testAssemblyName    = "nunit.framework";
        var assemblies          = AppDomain.CurrentDomain.GetAssemblies();
        IsInUnitTest            = assemblies.Any(a => a.FullName!.StartsWith(testAssemblyName));
    }
    
    public static double StopwatchMillis(Stopwatch stopwatch) {
        return stopwatch.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
    }
}