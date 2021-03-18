﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    public class Differ
    {
        public  readonly    TypeCache       typeCache;

        private readonly    List<PathNode>  path        = new List<PathNode>();
        private readonly    List<Parent>    parentStack = new List<Parent>();

        public Differ(TypeCache typeCache) {
            this.typeCache = typeCache;
        }

        public Diff GetDiff<T>(T left, T right) {
            parentStack.Clear();
            path.Clear();

            var mapper = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            var diff = mapper.Diff(this, left, right);
            if (parentStack.Count != 0)
                throw new InvalidOperationException($"Expect objectStack.Count == 0. Was: {parentStack.Count}");
            return diff;
        }

        private Diff GetParent(int parentIndex) {
            var parent = parentStack[parentIndex];
            var parentDiff = parent.diff;
            if (parentDiff != null)
                return parentDiff;

            var parentOfParentIndex = parentIndex - 1;
            if (parentOfParentIndex >= 0) {
                Diff parentOfParent = GetParent(parentOfParentIndex);
                parentDiff = parent.diff = new Diff(parentOfParent, path[parentOfParentIndex], parent.left, parent.right, new List<Diff>());
                parentOfParent.children.Add(parentDiff);
                return parentDiff;
            }
            parentDiff = parent.diff = new Diff(null, new PathNode{ nodeType = NodeType.Root }, parent.left, parent.right, new List<Diff>());
            return parentDiff;
        }

        public Diff AddDiff(object left, object right) {
            if (path.Count != parentStack.Count)
                throw new InvalidOperationException("Expect path.Count != parentStack.Count");

            Diff itemDiff = null; 
            int parentIndex = parentStack.Count - 1;
            if (parentIndex >= 0) {
                var parent = GetParent(parentIndex);
                itemDiff = new Diff(parent, path[parentIndex], left, right, null);
                parent.children.Add(itemDiff);
            } else {
                itemDiff = new Diff(null, new PathNode{ nodeType = NodeType.Root }, left, right, null);
            }
            return itemDiff;
        }

        public void PushField(PropField field) {
            var item = new PathNode {
                nodeType = NodeType.Member,
                field = field
            };
            path.Add(item);
        }
        
        public void PushElement(int index) {
            var item = new PathNode {
                nodeType = NodeType.Element,
                index = index
            };
            path.Add(item);
        }

        public void Pop() {
            int last = path.Count - 1;
            path.RemoveAt(last);
        }


        public void CompareElement<T> (TypeMapper<T> elementType, int index, T leftItem, T rightItem)
        {
            PushElement(index);
            bool leftNull  = elementType.IsNull(ref leftItem);
            bool rightNull = elementType.IsNull(ref rightItem);
            if (!leftNull || !rightNull) {
                if (!leftNull && !rightNull) {
                    elementType.Diff(this, leftItem, rightItem);
                } else {
                    AddDiff(leftItem, rightItem);
                }
            }
            Pop();
        }

        public void PushParent(object left, object right) {
            parentStack.Add(new Parent(left, right));
        }
        
        public Diff PopParent() {
            var lastIndex = parentStack.Count - 1;
            var last = parentStack[lastIndex];
            parentStack.RemoveAt(lastIndex);
            return last.diff;
        } 

    }


    public class Diff
    {
        public Diff(Diff parent, PathNode pathNode, object left, object right,  List<Diff> children) {
            this.parent     = parent;
            this.pathNode   = pathNode;
            this.left       = left;
            this.right      = right;
            this.children   = children;
        }

        public  readonly    Diff            parent; 
        public  readonly    PathNode        pathNode;
        public  readonly    object          left;
        public  readonly    object          right;
        public  readonly    List<Diff>      children;

        public override string ToString() {
            var sb = new StringBuilder();
            CreatePath(sb);
            return sb.ToString();
        }
        
        private void CreatePath(StringBuilder sb) {
            if (parent != null)
                parent.CreatePath(sb);
            switch (pathNode.nodeType) {
                case NodeType.Member:
                    sb.Append('/');
                    sb.Append(pathNode.field.name);
                    break;
                case NodeType.Element:
                    sb.Append('/');
                    sb.Append(pathNode.index);
                    break;
                case NodeType.Root:
                    return;
            }
        }
        
        public string GetChildrenDiff() {
            var sb = new StringBuilder();
            if (children != null) {
                foreach (var child in children) {
                    child.CreatePath(sb);
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }
    }

    class Parent
    {
        public readonly     object      left;
        public readonly     object      right;
        public              Diff        diff;

        public Parent(object left, object right) {
            this.left = left;
            this.right = right;
            diff = null;
        }
    }

    internal enum NodeType
    {
        Root,
        Element,
        Member,
    }

    public struct PathNode
    {
        internal    NodeType    nodeType;
        public      PropField   field;
        public      int         index;
    }
}
