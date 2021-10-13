﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.DB.Protocol.Models;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host
{
    internal enum ProcessingType {
        Validate,
        GetKey,
        SetKey
    }
    
    /// <summary>
    /// Is used to ensure that <see cref="ReadEntitiesSetResult"/> returned by <see cref="EntityContainer.ReadEntitiesSet"/>
    /// contains valid <see cref="ReadEntitiesSetResult.entities"/>.
    /// Validation is required for <see cref="EntityDatabase"/> implementations which cannot ensure that the value of
    /// its key/values are JSON. See <see cref="ReadEntitiesSetResult.ValidateEntities"/>.
    /// </summary>
    public sealed class EntityProcessor : IDisposable
    {
        private             Bytes               jsonBytes   = new Bytes(128);
        private             JsonParser          parser;
        private             Bytes               idKey       = new Bytes(16);
        private             bool                foundKey;
        //                  --- ReplaceKey
        private             JsonParser.State    keyState;
        private             int                 keyStart;
        private             int                 keyEnd;
        private             bool                foundIntKey;
        private             bool                asIntKey;
        private             Bytes               sb          = new Bytes(0);
        
        public bool GetEntityKey(JsonUtf8 json, string keyName, out JsonKey keyValue, out string error) {
            keyName  = keyName ?? "id";
            return Traverse(json, keyName, out keyValue, ProcessingType.GetKey,   out error);
        }
        
        public bool Validate(JsonUtf8 json, string keyName, out JsonKey keyValue, out string error) {
            return Traverse(json, keyName, out keyValue, ProcessingType.Validate, out error);
        }
        
        public JsonUtf8 ReplaceKey(JsonUtf8 json, string keyName, bool asIntKey, string newKeyName, out JsonKey keyValue, out string error) {
            this.asIntKey   = asIntKey;
            keyName         = keyName       ?? "id";
            newKeyName      = newKeyName    ?? "id";
            bool equalKeys  = keyName == newKeyName;
            if (!Traverse  (json, keyName, out keyValue, ProcessingType.SetKey,   out error))
                return new JsonUtf8();
            if (equalKeys && foundIntKey == asIntKey)
                return json;
            sb.Clear();
            sb.AppendArray(json, 0, keyStart);
            if (keyState == JsonParser.State.ExpectMember) {
                sb.AppendChar(',');
            }
            sb.AppendChar('\"');
            idKey.Clear();
            idKey.FromString(newKeyName);
            sb.AppendBytes(ref idKey);
            sb.AppendString("\":");
            if (asIntKey) {
                keyValue.AppendTo(ref sb, ref parser.format);
            } else {
                sb.AppendChar('\"');
                keyValue.AppendTo(ref sb, ref parser.format);
                sb.AppendChar('\"');
            }
            var remaining = json.Length - keyEnd; 
            sb.AppendArray(json, keyEnd, remaining);
            var result = sb.AsArray();
            sb.Clear();
            return new JsonUtf8(result);
        }

        private bool Traverse (JsonUtf8 json, string keyName, out JsonKey keyValue, ProcessingType processingType, out string error) {
            foundKey = false;
            idKey.Clear();
            idKey.FromString(keyName);
            keyValue = new JsonKey();
            jsonBytes.Clear();
            jsonBytes.AppendArray(json);
            parser.InitParser(jsonBytes);
            var ev = parser.NextEvent();
            if (ev != JsonEvent.ObjectStart) {
                error   = $"entity value must be an object.";
                return false;
            }
            while (true) {
                var pos     = parser.Position;
                var state   = parser.CurrentState;
                ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                        if (!parser.key.IsEqualBytes(ref idKey))
                            break;
                        foundKey = true;
                        keyValue = new JsonKey(ref parser.value, ref parser.valueParser);
                        switch (processingType) {
                            case ProcessingType.Validate:
                                continue;
                            case ProcessingType.GetKey:
                                error       = null;
                                return true;
                            case ProcessingType.SetKey:
                                keyState    = state;
                                keyStart    = pos;
                                keyEnd      = parser.Position;
                                foundIntKey = ev == JsonEvent.ValueNumber;
                                if (asIntKey  && ev == JsonEvent.ValueString)
                                    continue;
                                if (!asIntKey && ev == JsonEvent.ValueNumber) {
                                    continue;
                                }
                                error       = null;
                                return true;
                        }
                        break;
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNull:
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        parser.SkipTree();
                        if (!parser.error.ErrSet)
                            break;
                        error = parser.error.msg.AsString();
                        return false;
                    case JsonEvent.Error:
                        error = parser.error.msg.AsString();
                        return false;
                    case JsonEvent.ObjectEnd:
                        ev = parser.NextEvent();
                        if (ev != JsonEvent.EOF) {
                            error = "Expected EOF in JSON value";
                            return false;
                        }
                        if (foundKey) {
                            error = null;
                            return true;
                        }
                        error = $"missing key in JSON value. keyName: '{keyName}'";
                        return false;
                    case JsonEvent.ArrayEnd:
                        throw new InvalidOperationException($"unexpected event: {ev}");
                }
            }
        }
        
        public void Dispose() {
            idKey.Dispose();
            jsonBytes.Dispose();
            parser.Dispose();
            sb.Dispose();
        }
    }
}