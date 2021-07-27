﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Native
{
public class NativeTypeDef : TypeDef
    {
        internal readonly   Type                native;
        internal readonly   TypeMapper          mapper;
        internal            TypeDef             baseType;
        internal            List<FieldDef>      fields;
        internal            UnionType           unionType;
        
        public   override   TypeDef             BaseType        => baseType;
        public   override   bool                IsEnum          => native.IsEnum;
        public   override   bool                IsComplex       => mapper.IsComplex;
        public   override   List<FieldDef>      Fields          => fields;
        public   override   string              Discriminant    => mapper.Discriminant;
        public   override   TypeSemantic        TypeSemantic    => mapper.GetTypeSemantic();
        public   override   bool                IsArray         => mapper.IsArray;
        public   override   UnionType           UnionType       => unionType;
        public   override   bool                IsDictionary    => mapper.type.GetInterfaces().Contains(typeof(IDictionary));
        public   override   string              ToString()      => mapper.type.ToString();
        
        public   override   ICollection<string> EnumValues      => mapper.GetEnumValues();
        
        public   override   bool                IsDerivedField(FieldDef fieldDef) {
            var parent = BaseType;
            while (parent != null) {
                if (parent.Fields.Find(f => f.name == fieldDef.name) != null)
                    return true;
                parent = parent.BaseType;
            }
            return false;    
        }
           
        public NativeTypeDef (TypeMapper mapper) {
            this.native     = mapper.type;
            this.mapper     = mapper;
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