// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Transform.Project
{
    public class JsonProjector: IDisposable
    {
        private             Utf8JsonWriter          serializer;
            
        private             Bytes                   targetJson  = new Bytes(128);
        private             Utf8JsonParser          parser;
        private             Bytes                   valueBuf    = new Bytes(1);
        private             Bytes                   __typename;
        
        public              string                  ErrorMessage => parser.error.msg.AsString();
        
        public JsonProjector() {
            __typename   = new Bytes("__typename");
        }

        public void Dispose() {
            __typename.Dispose();
            valueBuf.Dispose();
            parser.Dispose();
            targetJson.Dispose();
            serializer.Dispose();
        }

        public JsonValue Project(in SelectionNode node, in JsonValue value) {
            targetJson.Clear();
            targetJson.AppendArray(value);
            parser.InitParser(targetJson);
            parser.NextEvent();
            serializer.InitSerializer();
            serializer.SetPretty(true);

            TraceTree(node);
            if (parser.error.ErrSet)
                return default;

            return new JsonValue(serializer.json.AsArray());
        }
        
        private bool TraceObject(in SelectionNode node) {
            bool        setUnionType    = node.emitTypeName && node.unions != null;
            Utf8String  unionType       = default;
            while (Utf8JsonWriter.NextObjectMember(ref parser)) {
                // Expect discriminator as first property
                if (setUnionType && parser.Event == JsonEvent.ValueString) {
                    unionType = node.FindUnionType(ref parser.value);
                }
                setUnionType = false;
                if (!node.FindField(ref parser.key, out var subNode)) {
                    parser.SkipEvent();
                    continue;
                }
                switch (parser.Event) {
                    case JsonEvent.ArrayStart:
                        serializer.MemberArrayStart(in parser.key);
                        TraceArray(subNode);
                        break;
                    case JsonEvent.ObjectStart:
                        serializer.MemberObjectStart(in parser.key);
                        TraceObject(subNode);
                        break;
                    case JsonEvent.ValueString:
                        serializer.MemberStr(in parser.key, in parser.value);
                        break;
                    case JsonEvent.ValueNumber:
                        serializer.MemberBytes(in parser.key, ref parser.value);
                        break;
                    case JsonEvent.ValueBool:
                        serializer.MemberBln(in parser.key, parser.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        serializer.MemberNul(in parser.key);
                        break;
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("WriteObject() unreachable"); // because of behaviour of ContinueObject()
                }
            }
            if (node.emitTypeName) {
                if (node.unions == null) { 
                    node.typeName.CopyTo(ref valueBuf);
                } else {
                    unionType.CopyTo    (ref valueBuf);
                }
                serializer.MemberStr(__typename, valueBuf);
            }
            serializer.ObjectEnd();
            return true;
        }
        
        private bool TraceArray(in SelectionNode node) {
            while (Utf8JsonWriter.NextArrayElement(ref parser)) {
                switch (parser.Event) {
                    case JsonEvent.ArrayStart:
                        serializer.ArrayStart(true);
                        TraceArray(node);
                        break;
                    case JsonEvent.ObjectStart:
                        serializer.ObjectStart();
                        TraceObject(node);
                        break;
                    case JsonEvent.ValueString:
                        serializer.ElementStr(in parser.value);
                        break;
                    case JsonEvent.ValueNumber:
                        serializer.ElementBytes (ref parser.value);
                        break;
                    case JsonEvent.ValueBool:
                        serializer.ElementBln(parser.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        serializer.ElementNul();
                        break;
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("TraceArray() unreachable");  // because of behaviour of ContinueArray()
                }
            }
            serializer.ArrayEnd();
            return true;
        }
        
        private bool TraceTree(in SelectionNode node) {
            switch (parser.Event) {
                case JsonEvent.ObjectStart:
                    serializer.ObjectStart();
                    return TraceObject(node);
                case JsonEvent.ArrayStart:
                    serializer.ArrayStart(true);
                    return TraceArray(node);
                case JsonEvent.ValueString:
                    serializer.ElementStr(in parser.value);
                    return true;
                case JsonEvent.ValueNumber:
                    serializer.ElementBytes(ref parser.value);
                    return true;
                case JsonEvent.ValueBool:
                    serializer.ElementBln(parser.boolValue);
                    return true;
                case JsonEvent.ValueNull:
                    serializer.ElementNul();
                    return true;
            }
            return false;
        }
    }
}
