﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Utils
{
    public class TypeContext
    {
        public readonly     Generator       generator;
        public readonly     HashSet<Type>   imports;
        public readonly     TypeMapper      owner;

        public override     string          ToString() => owner.type.Name;

        public TypeContext (Generator generator, HashSet<Type> imports, TypeMapper owner) {
            this.generator      = generator;
            this.imports        = imports;
            this.owner          = owner;
        }
    }
}