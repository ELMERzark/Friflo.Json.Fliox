﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Mapper.MapIL.Val;
using static Friflo.Json.Fliox.Mapper.Diff.DiffType;
// ReSharper disable CompareOfFloatsByEqualityOperator

// ReSharper disable PossibleInvalidOperationException
namespace Friflo.Json.Fliox.Mapper.Map.Val
{
    internal sealed class StringMatcher : ITypeMatcher {
        public static readonly StringMatcher Instance = new StringMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(string))
                return null;
            return new StringMapper(config, type);
        }
    }
    
    internal sealed class StringMapper : TypeMapper<string>
    {
        public override string DataTypeName() { return "string"; }
        
        public StringMapper(StoreConfig config, Type type) : base (config, type, true, false) { }

        public override void        Write   (ref Writer writer, string slot)            => writer.WriteString(slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.String);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.String, right.String);
        public override DiffType    Diff    (Differ differ, string left, string right)  => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (string value)                              => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.String, out success));
        
        public override string Read(ref Reader reader, string slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return reader.HandleEvent(this, out success);
            success = true;
            return reader.parser.value.GetString(ref reader.charBuf);
            // return reader.parser.value.ToString();
            // return null;
        }
    }
    
    // ---------------------------------------------------------------------------- double
    internal class DoubleMatcher : ITypeMatcher {
        public static readonly DoubleMatcher Instance = new DoubleMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(double))
                return config.useIL ? new DoubleFieldMapper(config, type) : new DoubleMapper (config, type);
            if (type == typeof(double?))
                return config.useIL ? new NullableDoubleFieldMapper(config, type) : new NullableDoubleMapper (config, type);
            return null;
        }
    }
    internal class DoubleMapper : TypeMapper<double> {
        public override string DataTypeName() { return "double"; }
        
        public DoubleMapper(StoreConfig config, Type type) : base (config, type, false, true) { }

        public override void        Write   (ref Writer writer, double slot)            => writer.format.AppendDbl(ref writer.bytes, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Flt64);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Flt64, right.Flt64);
        public override DiffType    Diff    (Differ differ, double left, double right)  => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (double value)                              => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Flt64, out success));
        
        public override double Read(ref Reader reader, double slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsDoubleStd(out success);
        }
    }
    internal class NullableDoubleMapper : TypeMapper<double?> {
        public override string DataTypeName() { return "double?"; }
        
        public NullableDoubleMapper(StoreConfig config, Type type) : base (config, type, true, true) { }

        public override void        Write   (ref Writer writer, double? slot)           => writer.format.AppendDbl(ref writer.bytes, (double)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Flt64Null);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Flt64Null, right.Flt64Null);
        public override DiffType    Diff    (Differ differ, double? left, double? right)=> left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (double? value)                             => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Flt64Null, out success));

        public override double? Read(ref Reader reader, double? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsDoubleStd(out success);
        }
    }

    // ---------------------------------------------------------------------------- float
    internal class FloatMatcher : ITypeMatcher {
        public static readonly FloatMatcher Instance = new FloatMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(float))
                return config.useIL ? new FloatFieldMapper(config, type) : new FloatMapper (config, type); 
            if (type == typeof(float?))
                return config.useIL ? new NullableFloatFieldMapper(config, type) : new NullableFloatMapper (config, type); 
            return null;
        }
    }
    internal class FloatMapper : TypeMapper<float> {
        public override string DataTypeName() { return "float"; }

        public FloatMapper(StoreConfig config, Type type) : base (config, type, false, true) { }
        
        public override void        Write   (ref Writer writer, float slot)             => writer.format.AppendFlt(ref writer.bytes, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Flt32);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Flt32, right.Flt32);
        public override DiffType    Diff    (Differ differ, float left, float right)    => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (float value)                               => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Flt32, out success));
        
        public override float Read(ref Reader reader, float slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsFloatStd(out success);
        }
    }
    internal class NullableFloatMapper : TypeMapper<float?> {
        public override string DataTypeName() { return "float?"; }

        public NullableFloatMapper(StoreConfig config, Type type) : base (config, type, true, true) { }
        
        public override void        Write   (ref Writer writer, float? slot)            => writer.format.AppendFlt(ref writer.bytes, (float)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Flt32Null);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Flt32Null, right.Flt32Null);
        public override DiffType    Diff    (Differ differ, float? left, float? right)  => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (float? value)                              => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Flt32Null, out success));
        
        public override float? Read(ref Reader reader, float? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsFloatStd(out success);
        }
    }

    // ---------------------------------------------------------------------------- long
    internal class LongMatcher : ITypeMatcher {
        public static readonly LongMatcher Instance = new LongMatcher();
                
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(long))
                return config.useIL ? new LongFieldMapper(config, type) : new LongMapper (config, type);
            if (type == typeof(long?))
                return config.useIL ? new NullableLongFieldMapper(config, type) : new NullableLongMapper (config, type);
            return null;
        }
    }
    internal class LongMapper : TypeMapper<long> {
        public override string DataTypeName() { return "long"; }

        public LongMapper(StoreConfig config, Type type) : base (config, type, false, true) { }
        
        public override void        Write   (ref Writer writer, long slot)              => writer.format.AppendLong(ref writer.bytes, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Int64);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Int64, right.Int64);
        public override DiffType    Diff    (Differ differ, long left, long right)      => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (long value)                                => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Int64, out success));
        
        public override long Read(ref Reader reader, long slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsLong(out success);
        }
    }
    internal class NullableLongMapper : TypeMapper<long?> {
        public override string DataTypeName() { return "long?"; }

        public NullableLongMapper(StoreConfig config, Type type) : base (config, type, true, true) { }
        
        public override void        Write   (ref Writer writer, long? slot)             => writer.format.AppendLong(ref writer.bytes, (long)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Int64Null);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Int64Null, right.Int64Null);
        public override DiffType    Diff    (Differ differ, long? left, long? right)    => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (long? value)                               => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Int64Null, out success));
        
        public override long? Read(ref Reader reader, long? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsLong(out success);
        }
    }
    
    // ---------------------------------------------------------------------------- int
    internal class IntMatcher : ITypeMatcher {
        public static readonly IntMatcher Instance = new IntMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(int))
                return config.useIL ? new IntFieldMapper(config, type) : new IntMapper (config, type); 
            if (type == typeof(int?))
                return config.useIL ? new NullableIntFieldMapper(config, type) : new NullableIntMapper(config, type);
            return null;
        }
    }
    internal class IntMapper : TypeMapper<int> {
        public override string DataTypeName() { return "int"; }

        public IntMapper(StoreConfig config, Type type) : base (config, type, false, true) { }
        
        public override void        Write   (ref Writer writer, int slot)               => writer.format.AppendInt(ref writer.bytes, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Int32);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Int32, right.Int32);
        public override DiffType    Diff    (Differ differ, int left, int right)        => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (int value)                                 => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Int32, out success));
        
        public override int Read(ref Reader reader, int slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsInt(out success);
        }
    }
    internal class NullableIntMapper : TypeMapper<int?> {
        public override string DataTypeName() { return "int?"; }

        public NullableIntMapper(StoreConfig config, Type type) : base (config, type, true, true) { }
        
        public override void        Write   (ref Writer writer, int? slot)              => writer.format.AppendInt(ref writer.bytes, (int)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Int32Null);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Int32Null, right.Int32Null);
        public override DiffType    Diff    (Differ differ, int? left, int? right)      => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (int? value)                                => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Int32Null, out success));
        
        public override int? Read(ref Reader reader, int? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsInt(out success);
        }
    }
    
    // ---------------------------------------------------------------------------- short
    internal class ShortMatcher : ITypeMatcher {
        public static readonly ShortMatcher Instance = new ShortMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(short))
                return config.useIL ? new ShortFieldMapper(config, type) : new ShortMapper (config, type);
            if (type == typeof(short?))
                return config.useIL ? new NullableShortFieldMapper(config, type) : new NullableShortMapper (config, type);
            return null;
        }
    }
    internal class ShortMapper : TypeMapper<short> {
        public override string DataTypeName() { return "short"; }
        
        public ShortMapper(StoreConfig config, Type type) : base (config, type, false, true) { }

        public override void        Write   (ref Writer writer, short slot)             => writer.format.AppendInt(ref writer.bytes, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Int16);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Int16, right.Int16);
        public override DiffType    Diff    (Differ differ, short left, short right)    => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (short value)                               => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Int16, out success));
        
        public override short Read(ref Reader reader, short slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsShort(out success);
        }
    }
    internal class NullableShortMapper : TypeMapper<short?> {
        public override string DataTypeName() { return "short?"; }
        
        public NullableShortMapper(StoreConfig config, Type type) : base (config, type, true, true) { }

        public override void        Write   (ref Writer writer, short? slot)            => writer.format.AppendInt(ref writer.bytes, (short)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Int16Null);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Int16Null, right.Int16Null);
        public override DiffType    Diff    (Differ differ, short? left, short? right)  => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (short? value)                              => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Int16Null, out success));
        
        public override short? Read(ref Reader reader, short? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsShort(out success);
        }
    }
    
    
    // ---------------------------------------------------------------------------- byte
    internal class ByteMatcher : ITypeMatcher {
        public static readonly ByteMatcher Instance = new ByteMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(byte))
                return config.useIL ? new ByteFieldMapper(config, type) : new ByteMapper (config, type); 
            if (type == typeof(byte?))
                return config.useIL ? new NullableByteFieldMapper(config, type) : new NullableByteMapper(config, type);
            return null;
        }
    }
    internal class ByteMapper : TypeMapper<byte> {
        public override string DataTypeName() { return "byte"; }

        public ByteMapper(StoreConfig config, Type type) : base (config, type, false, true) { }
        
        public override void        Write   (ref Writer writer, byte slot)              => writer.format.AppendInt(ref writer.bytes, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Int8);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Int8, right.Int8);
        public override DiffType    Diff    (Differ differ, byte left, byte right)      => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (byte value)                                => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Int8, out success));
        
        public override byte Read(ref Reader reader, byte slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsByte(out success);
        }
    }
    internal class NullableByteMapper : TypeMapper<byte?> {
        public override string DataTypeName() { return "byte?"; }

        public NullableByteMapper(StoreConfig config, Type type) : base (config, type, true, true) { }
        
        public override void        Write   (ref Writer writer, byte? slot)             => writer.format.AppendInt(ref writer.bytes, (byte)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Int8Null);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Int8Null, right.Int8Null);
        public override DiffType    Diff    (Differ differ, byte? left, byte? right)    => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (byte? value)                               => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Int8Null, out success));
        
        public override byte? Read(ref Reader reader, byte? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsByte(out success);
        }
    }
    
    // ---------------------------------------------------------------------------- bool
    internal class BoolMatcher : ITypeMatcher {
        public static readonly BoolMatcher Instance = new BoolMatcher();

        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type == typeof(bool))
                return config.useIL ? new BoolFieldMapper(config, type) : new BoolMapper (config, type);
            if (type == typeof(bool?))
                return config.useIL ? new NullableBoolFieldMapper(config, type) : new NullableBoolMapper (config, type);
            return null;
        }
    }
    
    internal class BoolMapper : TypeMapper<bool> {
        public override string DataTypeName() { return "bool"; }
        
        public BoolMapper(StoreConfig config, Type type) : base (config, type, false, true) { }

        public override void        Write   (ref Writer writer, bool slot)              => writer.format.AppendBool(ref writer.bytes, slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.Bool);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.Bool, right.Bool);
        public override DiffType    Diff    (Differ differ, bool left, bool right)      => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (bool value)                                => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.Bool, out success));
        
        public override bool Read(ref Reader reader, bool slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueBool)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsBool(out success);
        }
    }
    internal class NullableBoolMapper : TypeMapper<bool?> {
        public override string DataTypeName() { return "bool?"; }
        
        public NullableBoolMapper(StoreConfig config, Type type) : base (config, type, true, true) { }

        public override void        Write   (ref Writer writer, bool? slot)             => writer.format.AppendBool(ref writer.bytes, (bool)slot);
        public override void        WriteVar(ref Writer writer, in Var value)           => Write(ref writer, value.BoolNull);
        public override DiffType    DiffVar (Differ differ, in Var left, in Var right)  => Diff(differ, left.BoolNull, right.BoolNull);
        public override DiffType    Diff    (Differ differ, bool? left, bool? right)    => left == right ? Equal : differ.AddNotEqual(new Var(left), new Var(right));
        public override Var         ToVar   (bool? value)                               => new Var(value);
        public override Var         ReadVar (ref Reader reader, in Var value, out bool success) => new Var(Read(ref reader, value.BoolNull, out success));
        
        public override bool? Read(ref Reader reader, bool? slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueBool)
                return reader.HandleEvent(this, out success);
            return reader.parser.ValueAsBool(out success);
        }
    }
}