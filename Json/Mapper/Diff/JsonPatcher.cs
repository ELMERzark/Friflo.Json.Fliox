﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Diff
{
    public class JsonPatcher : IDisposable
    {
        private             JsonSerializer  serializer;
        
        private             Bytes           targetJson = new Bytes(128);
        private             JsonParser      targetParser;
        
        private             Bytes           patchJson = new Bytes(128);
        private             JsonParser      patchParser;
        
        private             Bytes           keyBytes = new Bytes(32);
        private readonly    List<PatchNode> nodeStack = new List<PatchNode>();
        private readonly    List<string>    pathNodes = new List<string>(); // reused buffer
        private readonly    PatchNode       rootNode = new PatchNode();

        public void Dispose() {
            keyBytes.Dispose();
            patchParser.Dispose();
            patchJson.Dispose();
            targetParser.Dispose();
            targetJson.Dispose();
            serializer.Dispose();
        }
        
        public string ApplyPatches(string root, IList<Patch> patches, bool pretty = false) {
            PatchNode.CreatePatchTree(rootNode, patches, pathNodes);
            nodeStack.Clear();
            nodeStack.Add(rootNode);
            targetJson.Clear();
            targetJson.AppendString(root);
            targetParser.InitParser(targetJson);
            targetParser.NextEvent();
            serializer.InitSerializer();
            serializer.SetPretty(pretty);
            
            TraceTree(ref targetParser);
            if (nodeStack.Count != 0)
                throw new InvalidOperationException("Expect nodeStack.Count == 0");
            rootNode.ClearChildren();
            return serializer.json.ToString();
        }

        private bool TraceObject(ref JsonParser p) {
            while (JsonSerializer.NextObjectMember(ref p)) {
                string key = p.key.ToString();
                var node = nodeStack[nodeStack.Count - 1];
                if (node.children.TryGetValue(key, out PatchNode patch)) {
                    switch (patch.patchType) {
                        case PatchType.Replace:
                        case PatchType.Add:
                            patchJson.Clear();
                            patchJson.AppendString(patch.json);
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
                                keyBytes.AppendString(child.Key);
                                patchJson.Clear();
                                patchJson.AppendString(patch.json);
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
        
        private bool TraceArray(ref JsonParser p) {
            while (JsonSerializer.NextArrayElement(ref p)) {
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
                    break;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
            return true;
        }
        
        private bool TraceTree(ref JsonParser p) {
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