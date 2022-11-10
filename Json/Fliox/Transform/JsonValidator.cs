﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform
{
    public sealed class JsonValidator : IDisposable
    {
        private             Bytes           jsonBytes = new Bytes(128);
        private             Utf8JsonParser  parser;
        
        public bool IsValidJson(in JsonValue json, out string error) {
            jsonBytes.Clear();
            jsonBytes.AppendArray(json);
            parser.InitParser(jsonBytes);
            while (true) {
                var ev = parser.NextEvent();
                if (ev == JsonEvent.EOF) {
                    error = null;
                    return true;
                }
                if (ev == JsonEvent.Error) {
                    error = parser.error.msg.ToString();
                    return false;
                }
            }
        }
        
        public void Dispose() {
            jsonBytes.Dispose();
            parser.Dispose();
        }
    }
}