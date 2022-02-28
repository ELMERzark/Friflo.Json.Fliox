﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Native;

// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Fliox.Schema.Validation
{
    /// <summary>
    /// <see cref="ValidationSet"/> provide the validation rules for <see cref="TypeValidator"/> to validate
    /// arbitrary JSON payloads by <see cref="TypeValidator.ValidateObject"/>.
    /// </summary>
    public sealed class ValidationSet
    {
        private  readonly   List<ValidationType>                types;
        private  readonly   Dictionary<TypeDef, ValidationType> typeMap;
        private  readonly   TypeDef                             rootType;
        
        public ValidationType GetValidationType (TypeDef type) {
            if (typeMap.TryGetValue(type, out var validationType))
                return validationType;
            return null;
        }
        
        /// <summary>
        /// Construct an immutable <see cref="ValidationSet"/> from a given <see cref="JSON.JsonTypeSchema"/> or a
        /// <see cref="Native.NativeTypeSchema"/>. The <see cref="ValidationSet"/> is intended to be used by
        /// <see cref="TypeValidator"/> to validate JSON payloads by <see cref="TypeValidator.ValidateObject"/>. 
        /// </summary>
        public ValidationSet (TypeSchema schema) {
            rootType        = schema.RootType;
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
            AddStandardType(TypeId.Guid,        standardType.Guid);
            AddStandardType(TypeId.JsonValue,   standardType.JsonValue);
            AddStandardType(TypeId.String,      standardType.JsonKey);

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
        
        public ICollection<ValidationType>  GetEntityTypes() {
            if (rootType == null) {
                throw new InvalidOperationException("GetEntityTypes() requires a TypeSchema with a TypeSchema.RootType");
            }
            var entityTypes = new List<ValidationType>();
            foreach (var field in rootType.Fields) {
                var validationType = typeMap[field.type];
                entityTypes.Add(validationType);
            }
            return entityTypes;
        }
        
        public ValidationType               TypeDefAsValidationType(TypeDef type) => typeMap[type];
        
        public ICollection<ValidationType>  TypeDefsAsValidationTypes(ICollection<TypeDef> types) {
            var list = new List<ValidationType>(this.types.Count);
            foreach (var typeDef in types) {
                var validationType = TypeDefAsValidationType(typeDef);
                list.Add(validationType);
            }
            return list;
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
                case TypeId.Guid:       return "Guid";
                case TypeId.JsonValue:  return "JSON";
                default:
                    throw new InvalidOperationException($"no standard typeId: {typeId}");
            }
        }
        
        public ValidationField  GetValidationField(NativeTypeSchema nativeSchema, Type type) {
            bool            required;
            var             underlyingType = Nullable.GetUnderlyingType(type);
            NativeTypeDef   typeDef;
            if (underlyingType != null) {
                required    = false;
                typeDef     = nativeSchema.GetNativeType(underlyingType);
            } else {
                typeDef     = nativeSchema.GetNativeType(type);
                required    = !typeDef.IsClass;
            }
            var isArray             = typeDef.mapper.IsArray;
            var fieldDef            = new FieldDef("param", required, false, false, typeDef, isArray, false, false, null, null, null);
            var validationField     = new ValidationField(fieldDef, -1);
            validationField.type    = TypeDefAsValidationType(typeDef);
            return validationField;
        }
    }
}