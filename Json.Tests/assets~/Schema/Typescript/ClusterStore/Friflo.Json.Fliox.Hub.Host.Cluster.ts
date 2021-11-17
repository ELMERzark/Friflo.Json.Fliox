// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema

export abstract class ClusterStore {
    catalogs        : { [key: string]: Catalog };
    catalogSchemas  : { [key: string]: CatalogSchema };
}

export interface ClusterStoreService {
    Echo (command: any) : any;
}

export class Catalog {
    name        : string;
    containers  : string[];
}

export class CatalogSchema {
    id        : string;
    rootType  : string;
    rootPath  : string;
    schemas   : { [key: string]: string };
}

