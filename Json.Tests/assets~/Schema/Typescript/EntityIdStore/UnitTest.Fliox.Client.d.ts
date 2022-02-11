// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { DbContainers } from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbCommands }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostDetails }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostCluster }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { Guid }         from "./Standard";
import { int32 }        from "./Standard";
import { int64 }        from "./Standard";
import { int16 }        from "./Standard";
import { uint8 }        from "./Standard";

export interface EntityIdStore {
    // --- containers
    guidEntities       : { [key: string]: GuidEntity };
    intEntities        : { [key: string]: IntEntity };
    intEntitiesAuto    : { [key: string]: AutoIntEntity };
    longEntities       : { [key: string]: LongEntity };
    shortEntities      : { [key: string]: ShortEntity };
    byteEntities       : { [key: string]: ByteEntity };
    customIdEntities   : { [key: string]: CustomIdEntity };
    entityRefs         : { [key: string]: EntityRefs };
    customIdEntities2  : { [key: string]: CustomIdEntity2 };

    // --- commands
    ["std.Echo"]           (param: any) : any;
    ["std.Containers"]     (param: any) : DbContainers;
    ["std.Commands"]       (param: any) : DbCommands;
    ["std.Schema"]         (param: any) : DbSchema;
    ["std.Stats"]          (param: any) : DbStats;
    ["std.Details"]        (param: any) : HostDetails;
    ["std.Cluster"]        (param: any) : HostCluster;
}

export class GuidEntity {
    id  : Guid;
}

export class IntEntity {
    id  : int32;
}

export class AutoIntEntity {
    id  : int32;
}

export class LongEntity {
    Id  : int64;
}

export class ShortEntity {
    id  : int16;
}

export class ByteEntity {
    id  : uint8;
}

export class CustomIdEntity {
    customId  : string;
}

export class EntityRefs {
    id               : string;
    guidEntity       : Guid;
    guidNullEntity?  : Guid | null;
    intEntity        : int32;
    intNullEntity?   : int32 | null;
    intNullEntity2?  : int32 | null;
    longEntity       : int64;
    longNullEntity?  : int64 | null;
    shortEntity      : int16;
    shortNullEntity? : int16 | null;
    byteEntity       : uint8;
    byteNullEntity?  : uint8 | null;
    customIdEntity?  : string | null;
    intEntities?     : int32[] | null;
    intNullEntities? : (int32 | null)[] | null;
}

export class CustomIdEntity2 {
    customId2  : string;
}

