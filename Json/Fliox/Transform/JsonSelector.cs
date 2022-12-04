﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Transform.Select;

namespace Friflo.Json.Fliox.Transform
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class JsonSelector : IDisposable
    {
        private             Utf8JsonWriter                      serializer;
        private             Utf8JsonParser                      targetParser;
        
        private readonly    List<PathNode<JsonSelectResult>>    nodeStack = new List<PathNode<JsonSelectResult>>();
        private readonly    JsonSelect                          reusedSelect = new JsonSelect();

        public              string                              ErrorMessage => targetParser.error.msg.AsString();

        public void Dispose() {
            targetParser.Dispose();
            serializer.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>null in case of an error - error message is available via <see cref="ErrorMessage"/>.</returns>
        public IReadOnlyList<JsonSelectResult> Select(in JsonValue json, JsonSelect scalarSelect, bool pretty = false) {
            scalarSelect.InitSelectorResults();
            nodeStack.Clear();
            nodeStack.Add(scalarSelect.nodeTree.rootNode);
            targetParser.InitParser(json);
            targetParser.NextEvent();
            serializer.SetPretty(pretty);
            
            TraceTree(ref targetParser);
            if (targetParser.error.ErrSet)
                return null;

            if (nodeStack.Count != 0)
                throw new InvalidOperationException("Expect nodeStack.Count == 0");
            
            // refill result list cause application code may mutate between Select() calls
            var results = scalarSelect.results; 
            results.Clear();
            foreach (var selector in scalarSelect.nodeTree.selectors) {
                results.Add(selector.result);
            }
            return results;
        }

        private void AddPathNodeResult(PathNode<JsonSelectResult> node) {
            var selectors = node.selectors;
            switch (targetParser.Event) {
                case JsonEvent.ObjectStart:
                    serializer.InitSerializer();
                    serializer.ObjectStart();
                    serializer.WriteObject(ref targetParser);
                    var json = serializer.json.AsString();
                    JsonSelectResult.Add(json, selectors);
                    return;
                case JsonEvent.ArrayStart:
                    serializer.InitSerializer();
                    serializer.ArrayStart(true);
                    serializer.WriteArray(ref targetParser);
                    json = serializer.json.AsString();
                    JsonSelectResult.Add(json, selectors);
                    return;
                case JsonEvent.ValueString:
                    json = targetParser.value.AsString();
                    JsonSelectResult.Add(json, selectors);
                    return;
                case JsonEvent.ValueNumber:
                    json = targetParser.value.AsString();
                    JsonSelectResult.Add(json, selectors);
                    return;
                case JsonEvent.ValueBool:
                    JsonSelectResult.Add(targetParser.boolValue ? "true" : "false", selectors);
                    return;
                case JsonEvent.ValueNull:
                    JsonSelectResult.Add("null", selectors);
                    return;
            }
        }
        
        private bool TraceObject(ref Utf8JsonParser p) {
            while (Utf8JsonWriter.NextObjectMember(ref p)) {
                var node = nodeStack[nodeStack.Count - 1];
                if (!node.FindByBytes(ref p.key, out PathNode<JsonSelectResult> path)) {
                    targetParser.SkipEvent();
                    continue;
                }
                // found node
                if (path.selectors.Count > 0) {
                    AddPathNodeResult(path);
                    continue;  // <- JsonSelector read JSON objects & arrays
                }
                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        nodeStack.Add(path);
                        TraceArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        nodeStack.Add(path);
                        TraceObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNull:
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
                PathNode<JsonSelectResult> path;
                if (node.wildcardNode != null) {
                    path = node.wildcardNode;
                    path.arrayIndex = index;
                } else {
                    if (!node.FindByIndex(index, out path)) {
                        targetParser.SkipEvent();
                        continue;
                    }
                    // found node
                }
                if (path.selectors.Count > 0) {
                    AddPathNodeResult(path);
                    continue;  // <- JsonSelector read JSON objects & arrays
                }
                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        nodeStack.Add(path);
                        TraceArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        nodeStack.Add(path);
                        TraceObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNull:
                        break;
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("TraceArray() unreachable"); // because of behaviour of ContinueArray()
                }
            }
            switch (p.Event) {
                case JsonEvent.ArrayEnd:
                    nodeStack.RemoveAt(nodeStack.Count - 1);
                    break;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
            return true;
        }
        
        private bool TraceTree(ref Utf8JsonParser p) {
            switch (p.Event) {
                case JsonEvent.ObjectStart:
                    return TraceObject(ref p);
                case JsonEvent.ArrayStart:
                    return TraceArray(ref p);
                case JsonEvent.ValueString:
                    return true;
                case JsonEvent.ValueNumber:
                    return true;
                case JsonEvent.ValueBool:
                    return true;
                case JsonEvent.ValueNull:
                    return true;
            }
            return false;
        }

    }
}
