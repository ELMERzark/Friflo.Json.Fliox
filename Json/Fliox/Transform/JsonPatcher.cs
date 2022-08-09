﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Transform.Patch;

namespace Friflo.Json.Fliox.Transform
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class JsonPatcher : IDisposable
    {
        private             Utf8JsonWriter  serializer;
        
        private             Bytes           targetJson = new Bytes(128);
        private             Utf8JsonParser  targetParser;
        
        private             Bytes           patchJson = new Bytes(128);
        private             Utf8JsonParser  patchParser;
        
        private             Bytes           keyBytes = new Bytes(32);
        private readonly    List<PatchNode> nodeStack = new List<PatchNode>();
        private readonly    List<JsonKey>   pathTokens = new List<JsonKey>(); // reused buffer
        private readonly    PatchNode       rootNode = new PatchNode();

        public void Dispose() {
            keyBytes.Dispose();
            patchParser.Dispose();
            patchJson.Dispose();
            targetParser.Dispose();
            targetJson.Dispose();
            serializer.Dispose();
        }
        
        public JsonValue ApplyPatches(JsonValue target, IList<JsonPatch> patches, bool pretty = false) {
            if (target.IsNull())
                throw new ArgumentException("ApplyPatches() target mus not be null");
                    
            PatchNode.CreatePatchTree(rootNode, patches, pathTokens);
            nodeStack.Clear();
            nodeStack.Add(rootNode);
            targetJson.Clear();
            targetJson.AppendArray(target);
            targetParser.InitParser(targetJson);
            targetParser.NextEvent();
            serializer.InitSerializer();
            serializer.SetPretty(pretty);

            TraceTree(ref targetParser);
            if (nodeStack.Count != 0)
                throw new InvalidOperationException("Expect nodeStack.Count == 0");
            rootNode.ClearChildren();
            return new JsonValue(serializer.json.AsArray());
        }

        public JsonValue Copy(JsonValue json, bool pretty) {
            serializer.SetPretty(pretty);
            this.targetJson.Clear();
            this.targetJson.AppendArray(json);
            targetParser.InitParser(this.targetJson);
            targetParser.NextEvent();
            serializer.InitSerializer();
            serializer.WriteTree(ref targetParser);
            return new JsonValue(serializer.json.AsArray());
        }

        private bool TraceObject(ref Utf8JsonParser p) {
            while (Utf8JsonWriter.NextObjectMember(ref p)) {
                var key  = new JsonKey(ref p.key, ref p.valueParser);
                var node = nodeStack[nodeStack.Count - 1];
                if (node.children.TryGetValue(key, out PatchNode patch)) {
                    switch (patch.patchType) {
                        case PatchType.Replace:
                        case PatchType.Add:
                            patchJson.Clear();
                            patchJson.AppendArray(patch.json);
                            patchParser.InitParser(patchJson);
                            patchParser.NextEvent();
                            serializer.WriteMember(ref p.key, ref patchParser);
                            targetParser.SkipEvent();
                            node.children.Remove(key);
                            continue;
                        case PatchType.Remove:
                            targetParser.SkipEvent();
                            node.children.Remove(key);
                            continue;
                        case null:
                            nodeStack.Add(patch);
                            break;
                        default:
                            throw new InvalidOperationException($"patchType not supported: {patch.patchType}");
                    }
                }
                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        serializer.MemberArrayStart(in p.key);
                        TraceArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        serializer.MemberObjectStart(in p.key);
                        TraceObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                        serializer.MemberStr(in p.key, in p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        serializer.MemberBytes(in p.key, ref p.value);
                        break;
                    case JsonEvent.ValueBool:
                        serializer.MemberBln(in p.key, p.boolValue);
                        break;
                    case JsonEvent.ValueNull:
                        serializer.MemberNul(in p.key);
                        break;
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("WriteObject() unreachable"); // because of behaviour of ContinueObject()
                }
            }

            switch (p.Event) {
                case JsonEvent.ObjectEnd:
                    var node = nodeStack[nodeStack.Count - 1];
                    foreach (var child in node.children) {
                        var patch = child.Value;
                        switch (patch.patchType) {
                            case PatchType.Replace:
                            case PatchType.Add:
                                keyBytes.Clear();
                                child.Key.AppendTo(ref keyBytes, ref p.format);
                                // var key = child.Key.AsString();
                                // keyBytes.AppendString(key);
                                patchJson.Clear();
                                patchJson.AppendArray(patch.json);
                                patchParser.InitParser(patchJson);
                                patchParser.NextEvent();
                                serializer.WriteMember(ref keyBytes, ref patchParser);
                                continue;
                            case PatchType.Remove:
                            case null:
                                continue;
                            default:
                                throw new InvalidOperationException($"patchType not supported: {patch.patchType}");
                        }
                    }
                    serializer.ObjectEnd();
                    nodeStack.RemoveAt(nodeStack.Count - 1);
                    break;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
            return true;
        }
        
        private bool TraceArray(ref Utf8JsonParser p) {
            int index = -1;
            while (Utf8JsonWriter.NextArrayElement(ref p)) {
                index++;
                var node = nodeStack[nodeStack.Count - 1];
                var key = new JsonKey(index);
                if (node.children.TryGetValue(key, out PatchNode patch)) {
                    switch (patch.patchType) {
                        case PatchType.Replace:
                            patchJson.Clear();
                            patchJson.AppendArray(patch.json);
                            patchParser.InitParser(patchJson);
                            patchParser.NextEvent();
                            serializer.WriteTree(ref patchParser);
                            targetParser.SkipEvent();
                            node.children.Remove(key);
                            continue;
                        case PatchType.Add:
                        case PatchType.Remove:
                            throw new InvalidOperationException($"patchType not supported for JSON array: {patch.patchType}");
                        case null:
                            nodeStack.Add(patch);
                            break;
                        default:
                            throw new InvalidOperationException($"patchType not supported: {patch.patchType}");
                    }
                }
                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        serializer.ArrayStart(true);
                        TraceArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        serializer.ObjectStart();
                        TraceObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                        serializer.ElementStr(in p.value);
                        break;
                    case JsonEvent.ValueNumber:
                        serializer.ElementBytes (ref p.value);
                        break;
                    case JsonEvent.ValueBool:
                        serializer.ElementBln(p.boolValue);
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
            switch (p.Event) {
                case JsonEvent.ArrayEnd:
                    serializer.ArrayEnd();
                    nodeStack.RemoveAt(nodeStack.Count - 1);
                    break;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
            return true;
        }
        
        private bool TraceTree(ref Utf8JsonParser p) {
            switch (rootNode.patchType) {
                case PatchType.Replace:
                    patchJson.Clear();
                    patchJson.AppendArray(rootNode.json);
                    patchParser.InitParser(patchJson);
                    patchParser.NextEvent();
                    serializer.WriteTree(ref patchParser);
                    nodeStack.Clear();
                    return true;
                case null:
                    break;
                default:
                    throw new InvalidOperationException($"PatchType not supported on JSON root. PatchType: {rootNode.patchType}"); 
            }
            switch (p.Event) {
                case JsonEvent.ObjectStart:
                    serializer.ObjectStart();
                    return TraceObject(ref p);
                case JsonEvent.ArrayStart:
                    serializer.ArrayStart(true);
                    return TraceArray(ref p);
                case JsonEvent.ValueString:
                    serializer.ElementStr(in p.value);
                    return true;
                case JsonEvent.ValueNumber:
                    serializer.ElementBytes(ref p.value);
                    return true;
                case JsonEvent.ValueBool:
                    serializer.ElementBln(p.boolValue);
                    return true;
                case JsonEvent.ValueNull:
                    serializer.ElementNul();
                    return true;
            }
            return false;
        }
    }
}