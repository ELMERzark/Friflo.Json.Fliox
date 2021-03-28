﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;

namespace Friflo.Json.EntityGraph.Database
{
    // ------------------------------------ CommandContext ------------------------------------
    /// <summary>
    /// One <see cref="SyncContext"/> is created per <see cref="EntityContainer"/> to enable multi threaded
    /// request handling for different <see cref="EntityContainer"/> instances.
    ///
    /// The <see cref="EntityContainer.SyncContext"/> for a specific <see cref="EntityContainer"/> must not be used
    /// multi threaded.
    ///
    /// E.g. Reading key/values of a database can be executed multi threaded, but serializing for them
    /// for a <see cref="SyncResponse"/> in <see cref="DatabaseCommand.Execute"/> need to be single threaded. 
    /// </summary>
    public class SyncContext : IDisposable
    {
        public              JsonSerializer  serializer;
        public              JsonParser      parser;

        public void Dispose() {
            parser.Dispose();
            serializer.Dispose();
        }
    }
}