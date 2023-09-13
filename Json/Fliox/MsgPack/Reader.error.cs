﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;

namespace Friflo.Json.Fliox.MsgPack
{
    public ref partial struct MsgReader
    {
        private void SetError(MsgReaderState error, MsgFormat type, int cur) {
            if (state != MsgReaderState.Ok) {
                return;
            }
            StopReader(error, type, cur);
            this.error = CreateErrorMessage();
        }
        
        private void SetRangeError(MsgFormat type, int cur) {
            if (state != MsgReaderState.Ok) {
                return;
            }
            StopReader(MsgReaderState.RangeError, type, cur);
            error = CreateErrorMessage();
        }
        
        private void SetEofError(int cur) {
            if (state != MsgReaderState.Ok) {
                return;
            }
            StopReader(MsgReaderState.UnexpectedEof, MsgFormat.root, cur);
            error = CreateErrorMessage();
        }
        
        private void SetEofErrorType(MsgFormat type, int cur) {
            if (state != MsgReaderState.Ok) {
                return;
            }
            StopReader(MsgReaderState.UnexpectedEof, type, cur);
            error = CreateErrorMessage();
        }
        
        // ----------------------------------------- utils -----------------------------------------
        private string CreateErrorMessage()
        {
            var sb = new StringBuilder();
            sb.Append("MessagePack error - ");
            sb.Append(MsgPackUtils.Error(state));
            sb.Append('.');
            if (errorType != MsgFormat.root) {
                sb.Append(" was: ");
                if (state == MsgReaderState.RangeError) {
                    MsgPackUtils.AppendValue(sb, data, errorType, errorPos);
                    sb.Append(' ');
                }
                sb.Append($"{MsgPackUtils.Name(errorType)}(0x{(int)errorType:X})");
            }
            sb.Append(" pos: ");
            sb.Append(errorPos);
            if (keyName == null) {
                sb.Append(" (root)");
            } else {
                var key = MsgPackUtils.SpanToString(keyName);
                sb.Append(" - last key: '");
                sb.Append(key);
                sb.Append('\'');
            }
            return sb.ToString();
        }
        
        private void StopReader(MsgReaderState state, MsgFormat type, int cur) {
            this.state  = state;
            pos         = MsgError;
            errorType   = type;
            errorPos    = cur;
        }
    }
}