﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredMemberAttribute;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class EntityIdStore : FlioxClient {
        // --- containers
        public  EntitySet <Guid,    GuidEntity>      guidEntities       { get; private set; }
        public  EntitySet <int,     IntEntity>       intEntities        { get; private set; }
        public  EntitySet <int,     AutoIntEntity>   intEntitiesAuto    { get; private set; }
        public  EntitySet <long,    LongEntity>      longEntities       { get; private set; }
        public  EntitySet <short,   ShortEntity>     shortEntities      { get; private set; }
        public  EntitySet <byte,    ByteEntity>      byteEntities       { get; private set; }
        public  EntitySet <string,  CustomIdEntity>  customIdEntities   { get; private set; }
        public  EntitySet <string,  EntityRefs>      entityRefs         { get; private set; }
        public  EntitySet <string,  CustomIdEntity2> customIdEntities2  { get; private set; }

        public EntityIdStore(FlioxHub hub) : base(hub) { }
    }

    public class GuidEntity {
        public Guid id;
    }

    public class IntEntity {
        public int  id;
    }
    
    public class AutoIntEntity {
        [Fri.AutoIncrement]
        public int  id;
    }
    
    public class LongEntity {
        [Fri.PrimaryKey]
        public long Id { get; set; }
    }
    
    public class ShortEntity {
        public short id;
    }
    
    public class ByteEntity {
        public byte id;
    }
    
    public class CustomIdEntity {
        [Fri.PrimaryKey]
        [Req]  public string customId;
    }
    
    public class EntityRefs {
        [Req]
        public  string                              id;
        public  Ref      <Guid,   GuidEntity>       guidEntity;
        public  Ref      <Guid?,  GuidEntity>       guidNullEntity;
        public  Ref      <int,    IntEntity>        intEntity;
        public  Ref      <int?,   IntEntity>        intNullEntity;
        public  Ref      <int?,   IntEntity>        intNullEntity2;
        public  Ref      <long,   LongEntity>       longEntity;
        public  Ref      <long?,  LongEntity>       longNullEntity;
        public  Ref      <short,  ShortEntity>      shortEntity;
        public  Ref      <short?, ShortEntity>      shortNullEntity;
        public  Ref      <byte,   ByteEntity>       byteEntity;
        public  Ref      <byte?,  ByteEntity>       byteNullEntity;
        public  Ref      <string, CustomIdEntity>   customIdEntity;
        public  List<Ref <int,    IntEntity>>       intEntities;
        // arrays with nullable references are supported, but bot recommended. It forces the application
        // for null checks, which can simply omitted by not using an array with nullable references.
        public  List<Ref <int?,   IntEntity>>       intNullEntities;
    }

    public class CustomIdEntity2 {
#if UNITY_5_3_OR_NEWER
        [Fri.Key] [Req]
#else
        // Apply [Key]      alternatively by System.ComponentModel.DataAnnotations.KeyAttribute
        // Apply [Required] alternatively by System.ComponentModel.DataAnnotations.RequiredAttribute
        [Key] [Required]
#endif
        public string customId2;
    }

}