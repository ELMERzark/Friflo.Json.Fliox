﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;

#if !UNITY_5_3_OR_NEWER
[assembly: CLSCompliant(true)]
#endif

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Flow.Graph
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class Entity
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _id;
        
        public string id {
            get => _id;
            set {
                if (_id == value)
                    return;
                if (_id == null) {
                    _id = value;
                    return;
                }
                throw new ArgumentException($"Entity id must not be changed. Type: {GetType().Name}, was: {_id}, set: {value}");
            }
        }

        public override     string  ToString() => id ?? "null";
    }
}