﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Transform
{
    /// <summary>
    /// Implement models of RFC 6902 - "JavaScript Object Notation (JSON) Patch"
    /// See: https://tools.ietf.org/html/rfc6902
    /// </summary>
    [Fri.Discriminator("op")]
    [Fri.Polymorph(typeof(PatchReplace),    Discriminant = "replace")]
    [Fri.Polymorph(typeof(PatchAdd),        Discriminant = "add")]
    [Fri.Polymorph(typeof(PatchRemove),     Discriminant = "remove")]
    [Fri.Polymorph(typeof(PatchCopy),       Discriminant = "copy")]
    [Fri.Polymorph(typeof(PatchMove),       Discriminant = "move")]
    [Fri.Polymorph(typeof(PatchTest),       Discriminant = "test")]
    public abstract class JsonPatch
    {
        public abstract PatchType PatchType { get; }
    }

    public class PatchReplace : JsonPatch
    {
        [Fri.Property(Required = true)]
        public string       path;
        [Fri.Property(Required = true)]
        public JsonValue    value;

        public override PatchType   PatchType   => PatchType.Replace;
        public override string      ToString()  => path;
    }
    
    public class PatchAdd : JsonPatch
    {
        [Fri.Property(Required = true)]
        public string       path;
        [Fri.Property(Required = true)]
        public JsonValue    value;

        public override PatchType   PatchType   => PatchType.Add;
        public override string      ToString()  => path;
    }
    
    public class PatchRemove : JsonPatch
    {
        [Fri.Property(Required = true)]
        public string       path;

        public override PatchType   PatchType   => PatchType.Remove;
        public override string      ToString()  => path;
    }
    
    public class PatchCopy : JsonPatch
    {
        [Fri.Property(Required = true)]
        public string       path;
        public string       from;

        public override PatchType   PatchType   => PatchType.Copy;
        public override string      ToString()  => path;
    }
    
    public class PatchMove : JsonPatch
    {
        [Fri.Property(Required = true)]
        public string       path;
        public string       from;

        public override PatchType   PatchType   => PatchType.Move;
        public override string      ToString()  => path;

    }
    
    public class PatchTest : JsonPatch
    {
        [Fri.Property(Required = true)]
        public string       path;
        public JsonValue    value;

        public override PatchType   PatchType   => PatchType.Test;
        public override string      ToString()  => path;
    }
    
    public enum PatchType
    {
        Replace,
        Remove,
        Add,
        Copy,
        Move,
        Test,
    }
}
