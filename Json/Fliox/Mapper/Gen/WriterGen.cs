// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using Friflo.Json.Fliox.Mapper.Map.Val;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Mapper.Map
{
    delegate void WriteDelegate<in T>(T obj, PropField[] fields, ref Writer writer, ref bool firstMember);

    partial struct Writer
    {
        private void WriteKeyNull (PropField field, ref bool firstMember) {
            if (!writeNullMembers)
                return;
            WriteFieldKey(field, ref firstMember);
            AppendNull();
        }
        
        // ---------------------------------- object - class / struct  ----------------------------------
        public void WriteClass<T> (PropField field, T value, ref bool firstMember) where T : class {
            if (value == null) {
                WriteKeyNull(field, ref firstMember);
                return;
            }
            WriteFieldKey(field, ref firstMember);
            var mapper = (TypeMapper<T>)field.fieldType;
            mapper.Write(ref this, value);
        }
        
        public void WriteStruct<T> (PropField field, T value, ref bool firstMember) where T : struct {
            WriteFieldKey(field, ref firstMember);
            var mapper = (TypeMapper<T>)field.fieldType;
            mapper.Write(ref this, value);
        }
        
        public void WriteStructNull<T> (PropField field, T? value, ref bool firstMember) where T : struct {
            if (value == null) {
                WriteKeyNull(field, ref firstMember);
                return;
            }
            WriteFieldKey(field, ref firstMember);
            var mapper = (TypeMapper<T>)field.fieldType;
            mapper.Write(ref this, value.Value);
        }
        
        // ------------------------------------------- bool ---------------------------------------------
        public void WriteBoolean (PropField field, bool value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendBool(ref bytes, value);
        }
        
        // --- nullable
        public void WriteBooleanNull (PropField field, bool? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendBool(ref bytes, value.Value);
        }
        
        // ------------------------------------------- number ---------------------------------------------
        // --- integer
        public void WriteByte (PropField field, byte value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void WriteInt16 (PropField field, short value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void WriteInt32 (PropField field, int value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void WriteInt64 (PropField field, long value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendLong(ref bytes, value);
        }
        
        // --- floating point
        public void WriteSingle (PropField field, float value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendFlt(ref bytes, value);
        }
        
        public void WriteDouble (PropField field, double value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendDbl(ref bytes, value);
        }
        
        // -------------------------------- nullable number ------------------------------------------
        // --- integer
        public void WriteByteNull (PropField field, byte? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value.Value);
        }
        
        public void WriteInt16Null (PropField field, short? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value.Value);
        }
        
        public void WriteInt32Null (PropField field, int? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value.Value);
        }
        
        public void WriteInt64Null (PropField field, long? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendLong(ref bytes, value.Value);
        }
        
        // --- floating point
        public void WriteSingleNull (PropField field, float? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendFlt(ref bytes, value.Value);
        }
        
        public void WriteDoubleNull (PropField field, double? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendDbl(ref bytes, value.Value);
        }
        
        // ------------------------------------------- string ---------------------------------------------
        public void WriteString (PropField field, string value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            WriteString(value);
        }

        public void WriteJsonKey (PropField field, in JsonKey value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            switch (value.type) {
                case JsonKeyType.Long:
                    bytes.AppendChar('\"');
                    format.AppendLong(ref bytes, value.lng);
                    bytes.AppendChar('\"');
                    break;
                case JsonKeyType.String:
                    WriteString(value.str);
                    break;
                case JsonKeyType.Guid:
                    WriteGuid(value.guid);
                    break;
                case JsonKeyType.Null:
                    AppendNull();
                    break;
            }
        }
        
        /*
        public void WriteGuid (PropField field, in Guid value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            WriteGuid(value);
        }
        
        public void WriteDateTime (PropField field, in DateTime value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            DateTimeMapper.ToRFC_3339(value);
        } */
        
        // ------------------------------------------- JSON ---------------------------------------------
        public void WriteJsonValue (PropField field, in JsonValue value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            if (!value.IsNull())
                bytes.AppendArray(value);
            else
                AppendNull();
        }
        
        // ------------------------------------------- enum ---------------------------------------------
        public void WriteEnum<T> (PropField field, T value, ref bool firstMember) where T : struct {
            WriteFieldKey(field, ref firstMember);
            var mapper = (EnumMapper<T>)field.fieldType;
            mapper.Write(ref this, value);
        }
        
        public void WriteEnumNull<T> (PropField field, T? value, ref bool firstMember) where T : struct {
            if (!value.HasValue) {
                WriteKeyNull(field, ref firstMember);
                return;
            }
            WriteFieldKey(field, ref firstMember);
            var mapper = (EnumMapper<T>)field.fieldType;
            mapper.Write(ref this, value.Value);
        }
    }
}
