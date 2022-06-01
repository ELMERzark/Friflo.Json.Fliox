// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;
using System;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace EntityIdStore2.Client {

public abstract class EntityIdStore {
    [Required]
    Dictionary<string, GuidEntity>       guidEntities;
    [Required]
    Dictionary<string, IntEntity>        intEntities;
    [Required]
    Dictionary<string, AutoIntEntity>    intEntitiesAuto;
    [Required]
    Dictionary<string, LongEntity>       longEntities;
    [Required]
    Dictionary<string, ShortEntity>      shortEntities;
    [Required]
    Dictionary<string, ByteEntity>       byteEntities;
    [Required]
    Dictionary<string, CustomIdEntity>   customIdEntities;
    [Required]
    Dictionary<string, EntityRefs>       entityRefs;
    [Required]
    Dictionary<string, CustomIdEntity2>  customIdEntities2;
}

public class GuidEntity {
    Guid  id;
}

public class IntEntity {
    int  id;
}

public class AutoIntEntity {
    int  id;
}

public class LongEntity {
    [PrimaryKey]
    long  Id;
}

public class ShortEntity {
    short  id;
}

public class ByteEntity {
    byte  id;
}

public class CustomIdEntity {
    [PrimaryKey]
    [Required]
    string  customId;
}

public class EntityRefs {
    [Required]
    string      id;
    Guid        guidEntity;
    Guid?       guidNullEntity;
    int         intEntity;
    int?        intNullEntity;
    int?        intNullEntity2;
    long        longEntity;
    long?       longNullEntity;
    short       shortEntity;
    short?      shortNullEntity;
    byte        byteEntity;
    byte?       byteNullEntity;
    string      customIdEntity;
    List<int>   intEntities;
    List<int?>  intNullEntities;
}

public class CustomIdEntity2 {
    [PrimaryKey]
    [Required]
    string  customId2;
}

}

