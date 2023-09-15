﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;
using System.Text;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;
using static Friflo.Json.Fliox.MsgPack.MsgReaderState;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.MsgPack
{
    [Flags]
    public enum MsgReaderState
    {
        Ok              = 0,
        /// <summary>
        /// bit set in case of range errors for <see cref="ExpectUint8"/>, ..., <see cref="ExpectFloat64"/>
        /// </summary>
        RangeError      = 0b_00001_0000, 
        //
        /// used to mask subsequent states / errors
        Mask            = 0x0f, 
        // --- expected state / error
        ExpectArray     = 0x01,
        ExpectByteArray = 0x02,
        ExpectBool      = 0x03,
        ExpectString    = 0x04,
        ExpectObject    = 0x05,
        ExpectKeyString = 0x06,
        //
        ExpectUint8     = 0x07,
        ExpectInt16     = 0x08,
        ExpectInt32     = 0x09,
        ExpectInt64     = 0x0a,
        ExpectFloat32   = 0x0b,
        ExpectFloat64   = 0x0c,
        // --- general errors
        UnexpectedEof   = 0x0d,
        UnsupportedType = 0x0e,
    }

    public ref partial struct MsgReader
    {
        // --- private fields
        [Browse(Never)] private     ReadOnlySpan<byte>  data;
        [Browse(Never)] private     int                 pos;
        [Browse(Never)] private     ReadOnlySpan<byte>  keyName;
        [Browse(Never)] private     MsgReaderState      state;
        [Browse(Never)] private     MsgFormat           errorType;
        [Browse(Never)] private     int                 errorPos;
        
        // --- public properties
                        public      ReadOnlySpan<byte>  Data            => data;
                        public      int                 Pos             => pos;
        /// <summary><see cref="keyName"/> is set in <see cref="ReadKey"/></summary>
                        public      ReadOnlySpan<byte>  KeyName         => keyName;
                        public      string              KeyNameString   => keyName == null ? null : MsgPackUtils.SpanToString(keyName);
        
                        public      string              Error           => CreateErrorMessage(null);
                        public      MsgReaderState      State           => state;
                        public      override string     ToString()      => GetString();
        // --- const
                        private     const int           MsgError = int.MaxValue;
        
        public MsgReader(ReadOnlySpan<byte> data) {
            this.data   = data;
            pos         = 0;
            keyName     = default;
            state       = Ok;
            errorType   = root;
            errorPos    = 0;
        }
        
        public void Init(ReadOnlySpan<byte> data) {
            this.data   = data;
            pos         = 0;
            keyName     = default;
            state       = Ok;
            errorType   = root;
            errorPos    = 0;
        }
        
        /// <summary>
        /// Return true if the given <paramref name="key"/> is equal to <see cref="KeyName"/>
        /// </summary>
        public bool IsKeyEquals(byte[] key) {
            return keyName.SequenceEqual(key);
        }
        
        private string GetString() {
            if (state != Ok) {
                return CreateErrorMessage(null);
            }
            var sb = new StringBuilder();
            sb.Append($"pos: {pos} ");
            if (keyName == null) {
                sb.Append("(root)");
            } else {
                sb.Append($", last key: '{KeyNameString}'");
            }
            return sb.ToString();
        }
        
        public bool ReadBool ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return false;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case True:      pos = cur + 1; return true;
                case False:     pos = cur + 1; return false;
            }
            SetError(ExpectBool, type, cur);
            return false;
        }
        
        public string ReadString () {
            if (ReadStringSpan(out var span)) {
                return MsgPackUtils.SpanToString(span);
            }
            return null;
        }
        
        public bool ReadStringSpan (out ReadOnlySpan<byte> result)
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                result = default;
                return false;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case nil:
                    pos = cur + 1;
                    result = default;
                    return false;
                case >= fixstr and <= fixstrMax: {
                    return read_str(out result, cur + 1, (int)type & 0x1f, type);
                }
                case str8: {
                    if (cur + 1 >= data.Length) {
                        SetEofErrorType(type, cur);
                        result = default;
                        return false;
                    }
                    int len = data[cur + 1];
                    return read_str(out result, cur + 2, len, type);
                }
                case str16: {
                    if (cur + 2 >= data.Length) {
                        SetEofErrorType(type, cur);
                        result = default;
                        return false;
                    }
                    int len = data[cur + 1] << 8 | data [cur + 2];
                    return read_str(out result, cur + 3, len, type);
                }
                case str32: {
                    if (cur + 4 >= data.Length) {
                        SetEofErrorType(type, cur);
                        result = default;
                        return false;
                    }
                    var len = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(cur + 1, 4));
                    return read_str(out result, cur + 5, (int)len, type);
                }
            }
            SetError(ExpectString, type, cur);
            result = default;
            return false;
        }
    }
}