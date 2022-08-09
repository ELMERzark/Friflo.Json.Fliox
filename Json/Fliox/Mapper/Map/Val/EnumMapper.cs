﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.MapIL.Val;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    internal sealed class EnumMatcher : ITypeMatcher {
        public static readonly EnumMatcher Instance = new EnumMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (!IsEnum(type, out bool _))
                return null;
            object[] constructorParams = {config, type};
#if !UNITY_5_3_OR_NEWER
            if (config.useIL) {
                // new EnumILMapper<T>(config, type);
                return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(EnumILMapper<>), new[] {type}, constructorParams);
            }
#endif
            // new EnumMapper<T> (config, type)
            var enumMapper = TypeMapperUtils.CreateGenericInstance(typeof(EnumMapper<>), new[] {type}, constructorParams);
            return (TypeMapper)enumMapper;
        }
        
        public static bool IsEnum(Type type, out bool isNullable) {
            isNullable = false;
            if (!type.IsEnum) {
                Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof( Nullable<>) );
                if (args == null)
                    return false;
                Type nullableType = args[0];
                if (!nullableType.IsEnum)
                    return false;
                isNullable = true;
            }
            return true;
        }

        
    }

    /// <summary>
    /// The mapping <see cref="enumToString"/> and <see cref="stringToEnum"/> is not bidirectional as this is the behaviour of C# enum types
    /// <code>
    /// public enum TestEnum {
    ///     Value1 = 11,
    ///     Value2 = 11, // duplicate constant value - C#/.NET maps these enum values to the first value using same constant
    /// }
    /// </code>
    /// </summary>    
    internal sealed class EnumMapper<T> : TypeMapper<T>
    {
        private     readonly Dictionary<BytesString, object> stringToEnum   = new Dictionary<BytesString, object>();
        private     readonly Dictionary<object, BytesString> enumToString   = new Dictionary<object, BytesString>();
        //
        private     readonly Dictionary<long, object>        integralToEnum = new Dictionary<long, object>();
        private     readonly Dictionary<string, string>      stringToDoc;
        
        public override string DataTypeName() { return $"enum {typeof(T).Name}"; }
        
        public EnumMapper(StoreConfig config, Type type) :
            base (config, typeof(T), Nullable.GetUnderlyingType(typeof(T)) != null, false)
        {
            Type enumType       = isNullable ? nullableUnderlyingType : type;
            var  enumContext    = new EnumContext(enumType, config.assemblyDocs);
            // ReSharper disable once PossibleNullReferenceException
            FieldInfo[] fields = enumType.GetFields();
            for (int n = 0; n < fields.Length; n++) {
                FieldInfo enumField = fields[n];
                if (enumField.FieldType.IsEnum) {
                    Enum    enumValue       = (Enum)enumField.GetValue(type);
                    string  enumName        = enumField.Name;
                    object  enumConst       = enumField.GetRawConstantValue();
                    long    enumIntegral    = TypeUtils.GetIntegralValue(enumConst, type);
                    var     name            = new BytesString(enumName);
                    stringToEnum.Add(name, enumValue);
                    enumToString.  TryAdd(enumValue, name);
                    integralToEnum.TryAdd(enumIntegral, enumValue);
                    enumContext.AddEnumValueDoc(ref stringToDoc, enumName);
                }
            }
            /*
            Type underlyingType = Enum.GetUnderlyingType(type);
            Array enumValues = Enum.GetValues(type);
            string[] enumNames = Enum.GetNames(type);

            for (int n = 0; n < enumValues.Length; n++) {
                Enum enumValue = (Enum)enumValues.GetValue(n);
                string enumName = enumNames[n];
                var name            = new BytesString(enumName);
                stringToEnum.TryAdd(name, enumValue);
                enumToString.Add(enumValue, name);
                // long underlyingValue = GetIntegralValue(enumValue, underlyingType);
                // integralToEnum.TryAdd(underlyingValue, enumValue);
            } */
        }
        
        public override void Dispose() {
            foreach (var key in stringToEnum.Keys)
                key.value.Dispose(Untracked.Bytes);
        }
        
        public override  List<string>    GetEnumValues() {
            var enumValues = new List<string>();
            foreach (var pair in stringToEnum) {
                BytesString enumValueBytes  = pair.Key;
                string      enumValue       = enumValueBytes.ToString();
                enumValues.Add(enumValue);
            }
            return enumValues;
        }
        
        public override  IReadOnlyDictionary<string, string> GetEnumValueDocs() => stringToDoc;

        public override void InitTypeMapper(TypeStore typeStore) {
        }

        public override void Write(ref Writer writer, T slot) {
            if (enumToString.TryGetValue(slot, out BytesString enumName)) {
                writer.bytes.AppendChar('\"');
                writer.bytes.AppendBytes(ref enumName.value);
                writer.bytes.AppendChar('\"');
            }
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            ref var parser = ref reader.parser;
            if (parser.Event == JsonEvent.ValueString) {
                reader.keyRef.value = parser.value;
                if (stringToEnum.TryGetValue(reader.keyRef, out object enumValue)) {
                    success = true;
                    return (T)enumValue;
                }
                return reader.ErrorIncompatible<T>("enum ", typeof(T).Name, this, out success);
            }
            if (parser.Event == JsonEvent.ValueNumber) {
                long integralValue = parser.ValueAsLong(out success);
                if (!success)
                    return default;
                if (integralToEnum.TryGetValue(integralValue, out object enumValue)) {
                    success = true;
                    return (T)enumValue;
                }
                return reader.ErrorIncompatible<T>("enum ", typeof(T).Name, this, out success);
            }
            return reader.HandleEvent(this, out success);
        }        
    }
    
    internal readonly struct EnumContext
    {
        private     readonly    Assembly        assembly;
        private     readonly    AssemblyDocs    assemblyDocs;
        private     readonly    string          @namespace;
        
        internal EnumContext(Type enumType, AssemblyDocs assemblyDocs) {
            assembly            = enumType.Assembly;
            this.assemblyDocs   = assemblyDocs;
            @namespace          = enumType.FullName;
        }
        
        public void AddEnumValueDoc(ref Dictionary<string, string> stringToDoc, string enumName) {
            if (assembly == null || assemblyDocs == null)
                return;
            var signature           = $"F:{@namespace}.{enumName}";
            var valueDoc            = assemblyDocs.GetDocs(assembly, signature);
            if (valueDoc == null)
                return;
            if (stringToDoc == null) {
                stringToDoc = new Dictionary<string, string>();
            }
            stringToDoc.Add(enumName, valueDoc);
        }
    }
}
