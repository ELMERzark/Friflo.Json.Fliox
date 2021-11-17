﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Schema.Validation
{
    public sealed class TypeValidator : IDisposable
    {
        internal            Utf8JsonParser  parser; // on top enabling instance offset 0
        private             Bytes           jsonBytes = new Bytes(128);
        private             ValidationError validationError;
        private  readonly   List<bool[]>    foundFieldsCache = new List<bool[]>();
        private  readonly   StringBuilder   sb = new StringBuilder();
        private  readonly   Regex           dateTime;
        private  readonly   Regex           bigInt;
        private  readonly   Regex           guid;
        
        public              bool            qualifiedTypeErrors;

        // RFC 3339 + milliseconds
        private  static readonly Regex  DateTime    = new Regex(@"\b^[1-9]\d{3}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}Z$\b",           RegexOptions.Compiled);
        private  static readonly Regex  BigInt      = new Regex(@"\b^-?[0-9]+$\b",                                                  RegexOptions.Compiled);
        private  static readonly Regex  Guid        = new Regex(@"\b^[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?$\b",RegexOptions.Compiled);

        public TypeValidator (bool qualifiedTypeErrors = false) {
            this.qualifiedTypeErrors    = qualifiedTypeErrors;
            dateTime                    = DateTime;
            bigInt                      = BigInt;
            guid                        = Guid;
        }
        
        public void Dispose() {
            parser.Dispose();
            jsonBytes.Dispose();
            foundFieldsCache.Clear();
            sb.Clear();
        }
        
        private void Init(JsonValue json) {
            validationError = new ValidationError();
            jsonBytes.Clear();
            jsonBytes.AppendArray(json);
            parser.InitParser(jsonBytes);
        }
        
        private bool Return(ValidationType type, bool success, out string error) {
            if (!success) {
                error = validationError.AsString(sb, qualifiedTypeErrors);
                return false;
            }
            var ev = parser.NextEvent();
            if (ev == JsonEvent.EOF) {
                error = null;
                return true;
            }
            return RootError(type, "Expected EOF after reading JSON", out error);
        }
        
        public bool ValidateObject (JsonValue json, ValidationType type, out string error) {
            Init(json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ObjectStart) {
                bool success = ValidateObject(type, 0);
                return Return(type, success, out error);    
            }
            return RootError(type, "ValidateObject() expect object. was:", out error);
        }
        
        public bool ValidateObjectMap (JsonValue json, ValidationType type, out string error) {
            Init(json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ObjectStart) {
                bool success = ValidateElement(type, false, null, 0);
                return Return(type, success, out error);    
            }
            return RootError(type, "ValidateObjectMap() expect object. was:", out error);
        }
        
        public bool ValidateArray (JsonValue json, ValidationType type, out string error) {
            Init(json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ArrayStart) {
                bool success = ValidateElement(type, false, null, 0);
                return Return(type, success, out error);    
            }
            return RootError(type, "ValidateArray() expect array. was:", out error);
        }
        
        private bool ValidateObject (ValidationType type, int depth)
        {
            if (type.typeId == TypeId.Union) {
                var ev      = parser.NextEvent();
                var unionType = type.unionType;
                if (ev != JsonEvent.ValueString) {
                    return ErrorType("Expect discriminator as first member.", ev.ToString(), false, unionType.discriminatorStr, null, type);
                }
                if (!parser.key.IsEqual(ref unionType.discriminator)) {
                    return ErrorType("Invalid discriminator.", parser.key.AsString(), true, unionType.discriminatorStr, null, type);
                }
                if (!ValidationUnion.FindUnion(unionType, ref parser.value, out var newType)) {
                    var expect = unionType.TypesAsString;
                    return ErrorType("Invalid discriminant.", parser.value.AsString(), true, expect, null, type);
                }
                type = newType;
            }
            var foundFields = GetFoundFields(type, foundFieldsCache, depth);

            while (true) {
                var             ev = parser.NextEvent();
                ValidationField field;
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (ValidateString (ref parser.value, field.type, type))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueNumber:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (ValidateNumber(field.type, type))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueBool:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (field.typeId == TypeId.Boolean)
                            continue;
                        var value = parser.boolValue ? "true" : "false";
                        return ErrorType("Incorrect type.", value, false, field.typeName, field.type.@namespace, type);
                    
                    case JsonEvent.ValueNull:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (!field.required)
                            continue;
                        return Error("Required property must not be null.", type);
                    
                    case JsonEvent.ArrayStart:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (field.isArray) {
                            if (ValidateElement (field.type, field.isNullableElement, type, depth))
                                continue;
                            return false;
                        }
                        return ErrorType("Incorrect type.", "array", false, field.typeName, field.type.@namespace, type);
                    
                    case JsonEvent.ObjectStart:
                        if (!ValidationType.FindField(type, this, out field, foundFields))
                            return false;
                        if (field.isDictionary) {
                            if (ValidateElement (field.type, field.isNullableElement, type, depth))
                                continue;
                            return false;
                        }
                        if (field.typeId == TypeId.Class) {
                            if (ValidateObject (field.type, depth + 1))
                                continue;
                            return false;
                        }
                        return ErrorType("Incorrect type.", "object", false, field.typeName, field.type.@namespace, type);
                    
                    case JsonEvent.ObjectEnd:
                        if (type.HasMissingFields(foundFields, sb)) {
                            return ErrorValue("Missing required fields:", sb.ToString(), false, type);
                        }
                        return true;
                    
                    case JsonEvent.Error:
                        return Error(parser.error.GetMessageBody(), type);

                    default:
                        return Error($"Unexpected JSON event in object: {ev}", type);
                }
            }
        }
        
        private bool ValidateElement (ValidationType type, bool isNullableElement, ValidationType parent, int depth) {
            while (true) {
                var     ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (ValidateString(ref parser.value, type, parent))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueNumber:
                        if (ValidateNumber(type, parent))
                            continue;
                        return false;
                        
                    case JsonEvent.ValueBool:
                        if (type.typeId == TypeId.Boolean)
                            continue;
                        var value = parser.boolValue ? "true" : "false";
                        return ErrorType("Incorrect type.", value, false, type.name, null, parent);
                    
                    case JsonEvent.ValueNull:
                        if (isNullableElement)
                            continue;
                        return Error("Element must not be null.", parent);
                    
                    case JsonEvent.ArrayStart:
                        var expect = ValidationType.GetName(type, qualifiedTypeErrors);
                        return Error($"Found array as array item. expect: {expect}", parent); // todo
                    
                    case JsonEvent.ObjectStart:
                        if (type.typeId == TypeId.Class || type.typeId == TypeId.Union) {
                            // in case of a dictionary the key is not relevant
                            if (ValidateObject(type, depth + 1))
                                continue;
                            return false;
                        }
                        return ErrorType("Incorrect type.", "object", false, type.name, type.@namespace, parent);
                    
                    case JsonEvent.ObjectEnd:
                        return true;

                    case JsonEvent.ArrayEnd:
                        return true;
                    
                    case JsonEvent.Error:
                        return Error(parser.error.GetMessageBody(), parent);

                    default:
                        return Error($"Unexpected JSON event: {ev}", parent);
                }
            }
        }
        
        private bool RootError (ValidationType type, string msg, out string error) {
            if (parser.Event == JsonEvent.Error) {
                Error(parser.error.GetMessageBody(), type);
            } else {
                ErrorValue(msg, parser.Event.ToString(), false, type);
            }
            error = validationError.AsString(sb, qualifiedTypeErrors);
            return false;
        }
        
        internal bool ErrorType (string msg, string was, bool isString, string expect, string expectNamespace, ValidationType type) {
            if (validationError.msg != null) {
                throw new InvalidOperationException($"error already set. Error: {validationError}");
            }
            validationError = new ValidationError(msg, was, isString, expect, expectNamespace, type, parser.GetPath(), parser.Position);
            return false;         
        }
        
        private bool Error(string msg, ValidationType type) {
            if (validationError.msg != null) {
                throw new InvalidOperationException($"error already set. Error: {validationError}");
            }
            validationError = new ValidationError(msg, null, false, type, parser.GetPath(), parser.Position);
            return false;
        }

        internal bool ErrorValue(string msg, string value, bool isString, ValidationType type) {
            if (validationError.msg != null) {
                throw new InvalidOperationException($"error already set. Error: {validationError}");
            }
            validationError = new ValidationError(msg, value, isString, type, parser.GetPath(), parser.Position);
            return false;
        }
        
        // --- helper methods
        private bool ValidateString (ref Bytes value, ValidationType type, ValidationType parent) {
            switch (type.typeId) {
                case TypeId.String:
                    return true;
                
                case TypeId.BigInteger:
                    var str = value.AsString();
                    if (bigInt.IsMatch(str)) {
                        return true;
                    }
                    return ErrorValue("Invalid BigInteger:", str, true, parent);
                
                case TypeId.DateTime:
                    str = value.AsString();
                    if (dateTime.IsMatch(str)) {
                        return true;
                    }
                    return ErrorValue("Invalid DateTime:", str, true, parent);
                
                case TypeId.Guid:
                    str = value.AsString();
                    if (guid.IsMatch(str)) {
                        return true;
                    }
                    return ErrorValue("Invalid Guid:", str, true, parent);
                
                case TypeId.Enum:
                    return ValidationType.FindEnum(type, ref value, this, parent);
                
                default:
                    return ErrorType("Incorrect type.", Truncate(ref value), true, type.name, type.@namespace, parent);
            }
        }
        
        private static string Truncate (ref Bytes value) {
            var str = value.AsString();
            if (str.Length < 20)
                return str;
            return str.Substring(20) + "...";
        }
        
        private bool ValidateNumber (ValidationType type, ValidationType owner) {
            var typeId = type.typeId; 
            switch (typeId) {
                case TypeId.Uint8:
                case TypeId.Int16:
                case TypeId.Int32:
                case TypeId.Int64:
                    if (parser.isFloat) {
                        return ErrorType("Invalid integer.", parser.value.AsString(), false, type.name, type.@namespace, owner);
                    }
                    var value = parser.ValueAsLong(out bool success);
                    if (!success) {
                        return ErrorType("Invalid integer.", parser.value.AsString(), false, type.name, type.@namespace, owner);
                    }
                    switch (typeId) {
                        case TypeId.Uint8: if (          0 <= value && value <=        255) { return true; } break;   
                        case TypeId.Int16: if (     -32768 <= value && value <=      32767) { return true; } break;
                        case TypeId.Int32: if (-2147483648 <= value && value <= 2147483647) { return true; } break;
                        case TypeId.Int64:                                                  { return true; }
                        default:
                            throw new InvalidOperationException("cant be reached");
                    }
                    return ErrorType("Integer out of range.", parser.value.AsString(), false, type.name, type.@namespace, owner);
                
                case TypeId.Float:
                case TypeId.Double:
                    return true;
                default:
                    return ErrorType("Incorrect type.", parser.value.AsString(), false, type.name, type.@namespace, owner);
            }
        }
        
        private static bool[] GetFoundFields(ValidationType type, List<bool[]> foundFieldsCache, int depth) {
            while (foundFieldsCache.Count <= depth) {
                foundFieldsCache.Add(null);
            }
            int requiredCount = type.requiredFieldsCount;
            bool[] foundFields = foundFieldsCache[depth];
            if (foundFields == null || foundFields.Length < requiredCount) {
                foundFields = foundFieldsCache[depth] = new bool[requiredCount];
            }
            for (int n= 0; n < requiredCount; n++) {
                foundFields[n] = false;
            }
            return foundFields;
        }
    }
}