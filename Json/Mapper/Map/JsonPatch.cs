﻿using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Mapper.Map
{
    public class JsonPatch
    {
        private List<Patch>     patches;
        private StringBuilder   sb = new StringBuilder();
        
        public List<Patch> CreatePatches(Diff diff) {
            patches = new List<Patch>();
            TraceDiff(diff);
            return patches;
        }

        private void TraceDiff(Diff diff) {
            if (diff.diffType == DiffType.Modified) {
                sb.Clear();
                diff.AddPath(sb);
                var replace = new PatchReplace {
                    path  = sb.ToString(),
                    value = diff.right
                };
                patches.Add(replace);
            }
            var children = diff.children;
            if (children != null) {
                for (int n = 0; n < children.Count; n++) {
                    var child = children[n];
                    TraceDiff(child);
                }
            }
        }
    }
}