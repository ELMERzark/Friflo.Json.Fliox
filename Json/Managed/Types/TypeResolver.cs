﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Codecs;

namespace Friflo.Json.Managed.Types
{
    public interface ITypeResolver
    {
        StubType CreateStubType(Type type);
    }
    
    public class TypeResolver : ITypeResolver
    {
        public readonly IJsonCodec[] resolvers;

        public TypeResolver(IJsonCodec[] resolvers) {
            this.resolvers = resolvers;
        }

        public StubType CreateStubType(Type type) {
            for (int n = 0; n < resolvers.Length; n++) {
                StubType stubType = resolvers[n].CreateStubType(type);
                if (stubType != null)
                    return stubType;
            }
            return null;
        }
    }

}