﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Diff
{
    public class Patcher : IDisposable
    {
        public  readonly    TypeCache       typeCache;
        private readonly    JsonReader      jsonReader;
        private             PatchType       patchType;
        private             string          json;
        private             int             pathPos;
        private readonly    List<string>    pathNodes = new List<string>();
        private             string          path;
        
        public Patcher(JsonReader jsonReader) {
            this.jsonReader = jsonReader;
            this.typeCache = jsonReader.TypeCache;
        }

        public void Dispose() { }

        public void Patch<T>(TypeMapper<T> mapper, T root, Patch patch) {
            pathPos = 0;
            patchType = patch.PatchType;
            switch (patchType) {
                case PatchType.Replace:
                    var replace = (PatchReplace) patch;
                    json = replace.value.json;
                    PathToPathNodes(replace.path, pathNodes);
                    path = replace.path;
                    if (pathNodes.Count == 0) {
                        jsonReader.ReadTo(json, root);
                        return;
                    }
                    mapper.PatchObject(this, root);
                    break;
                case PatchType.Add:
                    var add = (PatchAdd) patch;
                    json = add.value.json;
                    PathToPathNodes(add.path, pathNodes);
                    path = add.path;
                    mapper.PatchObject(this, root);
                    break;
                case PatchType.Remove:
                    var remove = (PatchRemove) patch;
                    PathToPathNodes(remove.path, pathNodes);
                    path = remove.path;
                    mapper.PatchObject(this, root);
                    break;
                default:
                    throw new NotImplementedException($"Patch type not supported. Type: {patch.GetType()}");
            }
        }

        public bool IsMember(string key) {
            return key.Equals(pathNodes[pathPos]);
        }

        public NodeAction Member(TypeMapper typeMapper, object member, out object value) {
            if (++pathPos >= pathNodes.Count) {
                value = jsonReader.ReadObject(json, typeMapper.type);
                return NodeAction.Assign;
            }
            typeMapper.PatchObject(this, member);
            value = member;
            return NodeAction.Assign;
        }
        
        public string GetMemberKey() {
            var key = pathNodes[pathPos];
            return key;
        }

        public void WalkElement(TypeMapper elementType, object element, out object value) {
            if (++pathPos >= pathNodes.Count) {
                value = jsonReader.ReadObject(json, elementType.type);
                return;
            }
            elementType.PatchObject(this, element);
            value = element;
        }

        public int GetElementIndex(int count) {
            var node = pathNodes[pathPos];
            if (!int.TryParse(node, out int index))
                throw new InvalidOperationException($"Incompatible element index type. index: {node} path: {path}");
            if (index >= count)
                throw new InvalidOperationException($"Element index out of range. Count: {count} index: {index} path: {path}");
            return index;
        }

        private static void PathToPathNodes(string path, List<string> pathNodes) {
            pathNodes.Clear();
            int last = 1;
            int len = path.Length;
            if (len == 0)
                return;
            for (int n = 1; n < len; n++) {
                if (path[n] == '/') {
                    var pathNode = path.Substring(last, n - last);
                    pathNodes.Add(pathNode);
                    last = n + 1;
                }
            }
            var lastNode = path.Substring(last, len - last);
            pathNodes.Add(lastNode);
        }
    }

    public enum NodeAction
    {
        Assign,
        Remove,
    }
}
