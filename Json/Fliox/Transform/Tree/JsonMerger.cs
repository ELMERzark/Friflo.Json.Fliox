// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using static Friflo.Json.Burst.JsonEvent;

namespace Friflo.Json.Fliox.Transform.Tree
{
    public class JsonMerger : IDisposable
    {
        public              bool                WriteNullMembers {
            get => writeNullMembers;
            set => writeNullMembers = astWriter.WriteNullMembers = value;
        }

        private             Utf8JsonParser      parser;
        private             Utf8JsonWriter      writer;
        private             Bytes               json;
        private readonly    JsonAstReader       astReader;
        private readonly    JsonAstWriter       astWriter;
        private             JsonAst             ast;
        private readonly    List<AstMembers>    membersStack;
        private             int                 membersStackIndex;
        private             bool                writeNullMembers;
        
        public JsonMerger() {
            json            = new Bytes(128);
            astReader       = new JsonAstReader();
            astWriter       = new JsonAstWriter();
            membersStack    = new List<AstMembers>();
        }
        
        public void Dispose() {
            astReader.Dispose();
            astWriter.Dispose();
            json.Dispose();
            parser.Dispose();
            writer.Dispose();
        }
        
        public JsonValue    Merge (JsonValue value, JsonValue patch) {
            MergeInternal(value, patch);
            return new JsonValue(writer.json.AsArray());
        }
        
        public Bytes        MergeBytes (JsonValue value, JsonValue patch) {
            MergeInternal(value, patch);
            return writer.json;
        }

        private void MergeInternal (JsonValue value, JsonValue patch) {
            membersStackIndex   = 0;
            ast                 = astReader.CreateAst(patch);
            astWriter.Init(ast);
            writer.InitSerializer();
            writer.SetPretty(false);
            json.Clear();
            json.AppendArray(value);
            parser.InitParser(json);
            parser.NextEvent();

            Start(0);
            
            astWriter.AssertBuffers();
        }
        
        private void Start(int index)
        {
            var ev  = parser.Event;
            switch (ev) {
                case ValueNull:     astWriter.WriteRootValue(ref writer);   break;
                case ValueBool:     astWriter.WriteRootValue(ref writer);   break;
                case ValueNumber:   astWriter.WriteRootValue(ref writer);   break;
                case ValueString:   astWriter.WriteRootValue(ref writer);   break;
                case ArrayStart:    astWriter.WriteRootValue(ref writer);   break;
                case ObjectStart:
                    writer.ObjectStart  ();
                    parser.NextEvent();
                    TraverseObject(index);  // descend
                    writer.ObjectEnd    ();
                    return;
                case ObjectEnd:
                case ArrayEnd:
                case EOF:
                default:
                    throw new InvalidOperationException($"unexpected state: {ev}");
            }
            parser.NextEvent();
        }

        private void TraverseArray() {
            while (true) {
                var ev = parser.Event;
                switch (ev) {
                    case ValueNull:     writer.ElementNul   ();                 break;
                    case ValueBool:     writer.ElementBln   (parser.boolValue); break;
                    case ValueNumber:   writer.ElementBytes (ref parser.value); break;
                    case ValueString:   writer.ElementStr   (parser.value);     break;
                    case ObjectStart:
                        writer.ObjectStart  ();
                        parser.NextEvent();
                        TraverseObject(-1); // descend
                        writer.ObjectEnd    ();
                        break;
                    case ArrayStart:
                        // no test coverage - at some point its enough :D
                        // addendum: well, I did it anyway :)
                        writer.ArrayStart   (false);
                        parser.NextEvent();
                        TraverseArray();    // descend
                        writer.ArrayEnd     ();
                        break;
                    case ArrayEnd:
                        return;
                    case ObjectEnd:
                    case EOF:
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
                parser.NextEvent();
            }
        }
        
        private void TraverseObject(int index)
        {
            var members = CreateMembers();
            GetPatchMembers(index, members.items);

            while (true) {
                var ev  = parser.Event;
                switch (ev) {
                    case ValueNull:
                        if (!ReplaceNode (index, members, out _)) {
                            if (writeNullMembers)                   writer.MemberNul  (parser.key);
                        }
                        break;
                    case ValueBool:
                        if (!ReplaceNode (index, members, out _)) { writer.MemberBln  (parser.key, parser.boolValue); }
                        break;
                    case ValueNumber:
                        if (!ReplaceNode (index, members, out _)) { writer.MemberBytes(parser.key, ref parser.value); }
                        break;
                    case ValueString:
                        if (!ReplaceNode (index, members, out _)) { writer.MemberStr  (parser.key, parser.value); }
                        break;
                    case ObjectStart:
                        if (ReplaceNode  (index, members, out int member)) {
                            parser.SkipTree();
                        } else {
                            writer.MemberObjectStart(parser.key);
                            parser.NextEvent();
                            TraverseObject (member);// descend
                            writer.ObjectEnd        ();
                        }
                        break;
                    case ArrayStart:
                        if (ReplaceNode (index, members, out _)) {
                            parser.SkipTree();
                        } else {
                            writer.MemberArrayStart (parser.key);
                            parser.NextEvent();
                            TraverseArray();        // descend
                            writer.ArrayEnd         ();
                        }
                        break;
                    case ObjectEnd:
                        WriteNewMembers(members);
                        ReleasePatchMembers();
                        return;
                    case ArrayEnd:
                    case EOF:
                    default:
                        throw new InvalidOperationException($"unexpected state: {ev}");
                }
                parser.NextEvent();
            }
        }
        
        private void WriteNewMembers(AstMembers members) {
            foreach (var member in members.items) {
                if (member.found)
                    continue;
                astWriter.WriteObjectMember(member.index, ref writer);
            }
        }
        
        private AstMembers CreateMembers() {
            if (membersStackIndex < membersStack.Count) {
                return membersStack[membersStackIndex++];
            }
            membersStackIndex++;
            var members = new AstMembers(new List<AstMember>());
            membersStack.Add(members);
            return members;
        }
        
        private void ReleasePatchMembers() {
            var members = membersStack[--membersStackIndex];
            members.items.Clear();
        }
        
        private void GetPatchMembers (int index, List<AstMember> members) {
            members.Clear();
            if (index == -1) {
                return; // case: only left (original) object available - no counterpart in patch (right) object 
            }
            var child = ast.intern.nodes[index].child;
            while (child != -1) {
                members.Add(new AstMember(child, false));
                child = ast.intern.nodes[child].next;
            }
        }
        
        private bool ReplaceNode (int index, AstMembers members, out int member)
        {
            if (index == -1) {
                member = -1;
                return false;
            }
            ref var searchKey   = ref parser.key;
            var searchKeyLen    = searchKey.Len;
            var searchKeySpan   = new Span<byte> (searchKey.buffer.array, searchKey.start, searchKeyLen);
            var items           = members.items;
            var memberCount     = items.Count;
            for (int n = 0; n < memberCount; n++)
            {
                var astMember       = items[n];
                if (astMember.found)
                    continue;
                var node            = ast.intern.nodes[astMember.index];
                if (searchKeyLen != node.key.len)
                    continue;
                var nodeKey = new Span<byte> (ast.intern.Buf, node.key.start, node.key.len);
                if (!searchKeySpan.SequenceEqual(nodeKey))
                    continue;
                // --- found node member
                member              = astMember.index;
                members.items[n]    = new AstMember (member, true);
                astWriter.WriteObjectMember(member, ref writer);
                return true;
            }
            member = -1;
            return false;
        }
    }
    
    internal readonly struct AstMembers
    {
        internal readonly   List<AstMember> items;
        
        public   override   string          ToString() => $"{items.Count}";
        
        internal AstMembers(List<AstMember> items) {
            this.items = items;
        }
    }
    
    internal readonly struct AstMember
    {
        internal readonly   int     index;
        internal readonly   bool    found;
        
        public   override   string  ToString() => $"index: {index} found: {found}";
        
        internal AstMember(int index, bool found) {
            this.index  = index;
            this.found  = found;
        }
    }
}