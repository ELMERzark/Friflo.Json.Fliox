// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using System;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace EntityIdStore2.Graph {

public class GuidEntity {
    Guid  id;
}

public class GuidNullEntity {
    Guid  id;
}

public class IntEntity {
    int  id;
}

public class LongEntity {
    long  Id;
}

public class ShortEntity {
    short  id;
}

public class ByteEntity {
    byte  id;
}

public class CustomIdEntity {
    [Fri.Key]
    [Fri.Required]
    string  customId;
}

public class EntityRefs {
    [Fri.Required]
    string     id;
    Guid       guidEntity;
    Guid?      guidNullEntity;
    int        intEntity;
    long       longEntity;
    short      shortEntity;
    byte       byteEntity;
    string     customIdEntity;
    List<int>  intEntities;
}

public class CustomIdEntity2 {
    [Fri.Key]
    [Fri.Required]
    string  customId2;
}

}

