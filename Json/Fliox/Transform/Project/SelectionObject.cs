// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Project
{
    /// <summary>
    /// Contain the <see cref="name"/> for a GraphQL type.
    /// The <see cref="name"/> is returned for selection sets containing the field: __typename  
    /// </summary>
    public readonly struct SelectionObject
    {
        public   readonly   Utf8String          name;
        public   readonly   SelectionField[]    fields;

        public   override   string              ToString() {
            if (name.IsNull)
                return "<no object type>";
            if (fields == null)
                return $"name: {name.AsString()}, fields: null";
            return $"name: {name.AsString()}, fields: {fields.Length}";
        }

        public  SelectionObject (in Utf8String typeName, SelectionField[] fields) {
            this.name   = typeName;
            this.fields = fields;
        }
        
        public SelectionField FindField(ReadOnlySpan<char> name) {
            if (fields == null) {
                return default;
            }
            for (int n = 0; n < fields.Length; n++) {
                var node  = fields[n];
                if (node.name.AsSpan().SequenceEqual(name)) {
                    return node;
                }
            }
            return default;
        }
    }
    
    public readonly struct SelectionField
    {
        public   readonly   string          name;
        public   readonly   SelectionObject objectType;
        
        public   override   string          ToString() {
            if (name == null)
                return "<no object field>";
            return $"{name} : {objectType.name.AsString()}";
        }

        public SelectionField (string fieldName, in SelectionObject objectType) {
            this.name       = fieldName;
            this.objectType = objectType;
        }
    }
}