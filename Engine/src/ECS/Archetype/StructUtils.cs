﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

// class is obsolete
internal static class StructInfo
{
    /// <summary> Is a multiple of 64. See <see cref="ComponentType{T}.PadCount512"/> </summary>
    internal const  int     ChunkSize           = 512; // check 64 - can be removed
    internal const  int     MissingAttribute    = 0;
}

internal static class StructUtils
{
    private static  int     _nextStructIndex    = 1;
    
    internal static int NewStructIndex(Type type, out string structKey)
    {
        foreach (var attr in type.CustomAttributes) {
            if (attr.AttributeType != typeof(ComponentKeyAttribute)) {
                continue;
            }
            var arg     = attr.ConstructorArguments;
            structKey   = (string) arg[0].Value;
            return _nextStructIndex++;
        }
        structKey = type.Name;
        return _nextStructIndex++;
    }
}
