// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { int64 } from "./Standard";

// schema documentation only - not implemented right now
export interface ClusterStore {
    // --- containers
    containers  : { [key: string]: DbContainers };
    commands    : { [key: string]: DbCommands };
    schemas     : { [key: string]: DbSchema };

    // --- commands
    ["std.Echo"]           (param: any) : any;
    ["std.Containers"]     (param: any) : DbContainers;
    ["std.Commands"]       (param: any) : DbCommands;
    ["std.Schema"]         (param: any) : DbSchema;
    ["std.Stats"]          (param: string | null) : DbStats;
    ["std.Details"]        (param: any) : HostDetails;
    ["std.Cluster"]        (param: any) : HostCluster;
}

export class DbContainers {
    id          : string;
    storage     : string;
    containers  : string[];
}

export class DbCommands {
    id        : string;
    commands  : string[];
}

export class DbSchema {
    id           : string;
    schemaName   : string;
    schemaPath   : string;
    jsonSchemas  : { [key: string]: any };
}

export class DbStats {
    containers? : ContainerStats[] | null;
}

export class ContainerStats {
    name   : string;
    count  : int64;
}

export class HostDetails {
    version         : string;
    hostName?       : string | null;
    projectName?    : string | null;
    projectWebsite? : string | null;
    envName?        : string | null;
    envColor?       : string | null;
}

export class HostCluster {
    databases  : DbContainers[];
}

