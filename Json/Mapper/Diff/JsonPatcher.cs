﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Diff
{
    public class JsonPatcher : IDisposable
    {
        public  readonly    JsonMapper      mapper;
        public  readonly    Differ          differ;
        
        private readonly    StringBuilder   sb = new StringBuilder();
        private readonly    TypeCache       typeCache;
        private readonly    Patcher         patcher;

        public JsonPatcher(TypeStore typeStore) 
            : this (new JsonMapper(typeStore))
        { }
        
        public JsonPatcher(JsonMapper mapper) {
            this.mapper = mapper;
            typeCache   = mapper.reader.TypeCache;
            patcher     = new Patcher(mapper.reader);
            differ      = new Differ(mapper.writer);
        }

        public void Dispose() {
            differ.Dispose();
            patcher.Dispose();
            mapper.Dispose();
        }

        public List<Patch> CreatePatches(DiffNode diff) {
            var patches = new List<Patch>();
            TraceDiff(diff, patches);
            return patches;
        }
        
        public List<Patch> GetPatches<T>(T left, T right) {
            var diff = differ.GetDiff(left, right);
            var patches = CreatePatches(diff);
            return patches;
        }

        public void ApplyPatches<T>(T root, IEnumerable<Patch> patches) {
            var rootMapper = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            foreach (var patch in patches) { 
                patcher.Patch(rootMapper, root, patch);
            }
        }
        
        public void ApplyDiff<T>(T root, DiffNode diff) {
            List<Patch> patches = CreatePatches(diff);
            ApplyPatches(root, patches);
        }

        private void TraceDiff(DiffNode diff, List<Patch> patches) {
            switch (diff.diffType) {
                case DiffType.NotEqual:
                    sb.Clear();
                    diff.AddPath(sb);
                    var json = mapper.WriteObject(diff.right);
                    Patch patch = new PatchReplace {
                        path = sb.ToString(),
                        value = { json = json }
                    };
                    patches.Add(patch);
                    break;
                case DiffType.OnlyLeft:
                    sb.Clear();
                    diff.AddPath(sb);
                    patch = new PatchRemove {
                        path = sb.ToString()
                    };
                    patches.Add(patch);
                    break;
                case DiffType.OnlyRight:
                    sb.Clear();
                    diff.AddPath(sb);
                    json = mapper.WriteObject(diff.right);
                    patch = new PatchAdd {
                        path = sb.ToString(),
                        value = { json = json }
                    };
                    patches.Add(patch);
                    break;
            }
            var children = diff.children;
            if (children != null) {
                for (int n = 0; n < children.Count; n++) {
                    var child = children[n];
                    TraceDiff(child, patches);
                }
            }
        }
    }
}