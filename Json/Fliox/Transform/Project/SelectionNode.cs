// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Project
{
    public readonly struct SelectionNode
    {
        private   readonly      SelectionNode[] nodes;
        private   readonly      Utf8String      name;
        private   readonly      bool            emitTypeName;
        private   readonly      Utf8String      typeName;

        public override         string          ToString() => FormatToString();

        public SelectionNode  (in Utf8String name, in Utf8String typeName, bool emitTypeName, SelectionNode[] nodes) {
            this.name           = name;
            this.typeName       = typeName;
            this.emitTypeName   = emitTypeName;
            this.nodes          = nodes;
        }
        
        private string FormatToString() {
            var selectionName = name.IsNull ? "(root)" : name.ToString();
            if (nodes == null)
                return selectionName;
            return $"{selectionName} - nodes: {nodes.Length}";
        }
        
        public bool FindNode(ref Bytes key, out SelectionNode result) {
            for (int n = 0; n < nodes.Length; n++) {
                var node  = nodes[n];
                if (node.name.IsEqual(ref key)) {
                    result = node;
                    return true;
                }
            }
            result = default;
            return false;
        }
    }
}
