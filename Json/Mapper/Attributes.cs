﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class Fri {

        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
        public sealed class TypeMapperAttribute : Attribute {
            public TypeMapperAttribute (Type  typeMapper) {}
        }
        
        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
        public sealed class DiscriminatorAttribute : Attribute {
            public DiscriminatorAttribute (string discriminator) {}
        }

        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
        public sealed class PolymorphAttribute : Attribute {
            public string     Discriminant    { get; set; }
            public PolymorphAttribute (Type instance) {}
        }

        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
        public sealed class InstanceAttribute : Attribute {
            public InstanceAttribute (Type instance) {}
        }
        
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public sealed class PropertyAttribute : Attribute {
            public string     Name    { get; set; }
        }
        
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public sealed class IgnoreAttribute : Attribute {
        }
    }
    
    // Change to attribute
    public class Entity {
        public string   id;
    }
}
