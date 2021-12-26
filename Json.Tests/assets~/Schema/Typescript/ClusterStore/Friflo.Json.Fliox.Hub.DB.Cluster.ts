// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema

export abstract class ClusterStore {
    containers  : { [key: string]: DbContainers };
    schemas     : { [key: string]: DbSchema };
    commands    : { [key: string]: DbCommands };
}

export interface ClusterStoreService {
    DbInfo       (param: any) : DbInfo;
    DbContainers (param: any) : DbContainers;
    DbCommands   (param: any) : DbCommands;
    DbSchema     (param: any) : DbSchema;
    DbList       (param: any) : DbList;
    Echo         (param: any) : any;
}

export class DbContainers {
    id            : string;
    databaseType  : string;
    containers    : string[];
}

export class DbSchema {
    id           : string;
    schemaName   : string;
    schemaPath   : string;
    jsonSchemas  : { [key: string]: any };
}

export class DbCommands {
    id        : string;
    commands  : string[];
}

export class DbInfo {
    hubVersion  : string;
}

export class DbList {
    databases  : DbContainers[];
}

