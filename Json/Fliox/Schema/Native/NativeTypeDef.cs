﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Native
{
public sealed class NativeTypeDef : TypeDef
    {
        // --- internal
        internal readonly   Type                native;
        internal readonly   TypeMapper          mapper;
        internal            NativeTypeDef       baseType;
        internal            List<FieldDef>      fields;
        internal            List<CommandDef>    commands;
        internal            UnionType           unionType;
        internal            string              discriminator;
        internal            bool                isAbstract;
        
        // --- TypeDef
        public   override   TypeDef             BaseType        => baseType;
        public   override   bool                IsEnum          { get; }
        public   override   bool                IsClass         { get; }
        public   override   bool                IsStruct        { get; }
        public   override   List<FieldDef>      Fields          => fields;
        public   override   List<CommandDef>    Commands        => commands;
        public   override   string              Discriminant    { get; }
        public   override   string              Discriminator   => discriminator;
        public   override   UnionType           UnionType       => unionType;
        public   override   bool                IsAbstract      => isAbstract;
        public   override   ICollection<string> EnumValues      { get; }
        
        public   override   string              ToString()      => $"{Namespace} {Name}";
        
        public NativeTypeDef (TypeMapper mapper, string name, string @namespace) :
            base(name, @namespace) 
        {
            this.native     = mapper.type;
            this.mapper     = mapper;
            IsEnum          = native.IsEnum;
            IsClass         = mapper.IsComplex;
            IsStruct        = mapper.type.IsValueType && mapper.type != typeof(JsonKey); // JsonKey is "nullable"
            Discriminant    = mapper.Discriminant;
            EnumValues      = mapper.GetEnumValues();
        }
        
        public override bool Equals(object obj) {
            if (obj == null)
                throw new NullReferenceException();
            var other = (NativeTypeDef)obj;
            return native == other.native;
        }

        public override int GetHashCode() {
            return (native != null ? native.GetHashCode() : 0);
        }
    }
}