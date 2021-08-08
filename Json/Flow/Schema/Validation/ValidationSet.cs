﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Validation
{
    /// <summary>
    /// <see cref="ValidationSet"/> provide the validation rules for <see cref="TypeValidator"/> to validate
    /// arbitrary JSON payloads by <see cref="TypeValidator.ValidateObject"/>.
    /// </summary>
    public class ValidationSet : IDisposable
    {
        private  readonly   List<ValidationType>                types;
        private  readonly   Dictionary<TypeDef, ValidationType> typeMap;
        
        public              ValidationType                      TypeDefAsValidationType(TypeDef type) => typeMap[type];

        /// <summary>
        /// Construct a <see cref="ValidationSet"/> from a given <see cref="JSON.JsonTypeSchema"/> or a
        /// <see cref="Native.NativeTypeSchema"/>. The <see cref="ValidationSet"/> is intended to be used by
        /// <see cref="TypeValidator"/> to validate JSON payloads by <see cref="TypeValidator.ValidateObject"/>. 
        /// </summary>
        public ValidationSet (TypeSchema schema) {
            var schemaTypes = schema.Types;
            var typeCount   = schemaTypes.Count + 20; // 20 - roughly the number of StandardTypes
            types           = new List<ValidationType>                  (typeCount);
            typeMap         = new Dictionary<TypeDef, ValidationType>   (typeCount);
            
            var standardType = schema.StandardTypes;
            AddStandardType(TypeId.Boolean,     standardType.Boolean);
            AddStandardType(TypeId.String,      standardType.String);
            AddStandardType(TypeId.Uint8,       standardType.Uint8);
            AddStandardType(TypeId.Int16,       standardType.Int16);
            AddStandardType(TypeId.Int32,       standardType.Int32);
            AddStandardType(TypeId.Int64,       standardType.Int64);
            AddStandardType(TypeId.Float,       standardType.Float);
            AddStandardType(TypeId.Double,      standardType.Double);
            AddStandardType(TypeId.BigInteger,  standardType.BigInteger);
            AddStandardType(TypeId.DateTime,    standardType.DateTime);
            AddStandardType(TypeId.JsonValue,   standardType.JsonValue);

            foreach (var type in schemaTypes) {
                if (typeMap.ContainsKey(type))
                    continue;
                var validationType = ValidationType.Create(type);
                if (validationType == null)
                    continue;
                types.Add(validationType);
                typeMap.Add(type, validationType);
            }
            // set ValidationType references
            foreach (var type in types) {
                type.SetFields(typeMap);
                var union = type.unionType;
                union?.SetUnionTypes(typeMap);
            }
        }
        
        private void AddStandardType (TypeId typeId, TypeDef typeDef) {
            if (typeDef == null)
                return;
            var typeName = GetTypeName(typeId);
            var type = new ValidationType(typeId, typeName, typeDef);
            types.Add(type);
            typeMap.Add(typeDef, type);
        }
        
        private static string GetTypeName (TypeId typeId) {
            switch (typeId) {
                case TypeId.Uint8:      return "uint8";
                case TypeId.Int16:      return "int16";
                case TypeId.Int32:      return "int32";
                case TypeId.Int64:      return "int64";
                case TypeId.Float:      return "float";
                case TypeId.Double:     return "double";
                // --- boolean type
                case TypeId.Boolean:    return "boolean";
                // --- string types        
                case TypeId.String:     return "string";
                case TypeId.BigInteger: return "BigInteger";
                case TypeId.DateTime:   return "DateTime";
                case TypeId.JsonValue:  return "JSON";
                default:
                    throw new InvalidOperationException($"no standard typeId: {typeId}");
            }
        }

        public void Dispose() {
            foreach (var type in types) {
                type.Dispose();
            }
        }
    }
}