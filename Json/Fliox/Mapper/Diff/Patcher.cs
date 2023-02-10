﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Select;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Diff
{
    public sealed class Patcher : IDisposable
    {
        private  readonly   TypeCache       typeCache;
        private  readonly   ObjectReader    jsonReader;
        private             PatchType       patchType;
        private             JsonValue       json;
        private             int             pathPos;
        private  readonly   List<JsonKey>   pathNodes = new List<JsonKey>();
        private             string          path;
        
        public              TypeCache       TypeCache => typeCache;
        
        public Patcher(ObjectReader jsonReader) {
            this.jsonReader = jsonReader;
            typeCache       = jsonReader.TypeCache;
        }

        public void Dispose() { }

        internal void Patch<T>(TypeMapper<T> mapper, T root, JsonPatch patch) {
            pathPos = 0;
            patchType = patch.PatchType;
            switch (patchType) {
                case PatchType.Replace:
                    var replace = (PatchReplace) patch;
                    json = replace.value;
                    path = PathTools.PathToPathTokens(replace.path, pathNodes);
                    if (pathNodes.Count == 0) {
                        jsonReader.ReadToMapper(mapper, json, root, false);
                        return;
                    }
                    break;
                case PatchType.Add:
                    var add = (PatchAdd) patch;
                    json = add.value;
                    path = PathTools.PathToPathTokens(add.path, pathNodes);
                    break;
                case PatchType.Remove:
                    var remove = (PatchRemove) patch;
                    path = PathTools.PathToPathTokens(remove.path, pathNodes);
                    break;
                default:
                    throw new NotImplementedException($"Patch type not supported. Type: {patch.GetType()}");
            }
            mapper.PatchObject(this, root);
        }

        public bool IsMember(in JsonKey key) {
            return key.IsEqual(pathNodes[pathPos]);
        }

        public NodeAction DescendMember(TypeMapper typeMapper, in Var member, out Var value) {
            if (++pathPos >= pathNodes.Count) {
                value = jsonReader.ReadObjectVar(json, typeMapper.type);
                switch (patchType) {
                    case PatchType.Replace:
                    case PatchType.Add:
                        return NodeAction.Assign;
                    case PatchType.Remove:
                        return NodeAction.Remove;
                }
                throw new NotImplementedException($"patchType not implemented: {patchType}");
            }
            typeMapper.PatchObject(this, member.Object);
            value = member;
            return NodeAction.Assign;
        }
        
        public JsonKey GetMemberKey() {
            var key = pathNodes[pathPos];
            return key;
        }
        
        public NodeAction DescendElement<T>(TypeMapper elementType, T element, out T value) {
            if (++pathPos >= pathNodes.Count) {
                value = jsonReader.Read<T>(json);
                return NodeAction.Assign;
            }
            elementType.PatchObject(this, element);
            value = element;
            return NodeAction.Assign;
        }

        public int GetElementIndex(int count) {
            var nodeKey = pathNodes[pathPos];
            var index   = nodeKey.AsLong();
            if (index >= count)
                throw new InvalidOperationException($"Element index out of range. Count: {count} index: {index} path: {path}");
            return (int)index;
        }
    }

    public enum NodeAction
    {
        Assign  = 1,
        Remove  = 2,
    }
}
