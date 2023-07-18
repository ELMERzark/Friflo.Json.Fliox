// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { int32 }   from "./Standard";
import { int64 }   from "./Standard";
import { JsonKey } from "./Standard";

/**
 * **ClusterStore** provide information about databases hosted by the Hub:   
 * - available containers aka tables per database   
 * - available commands per database   
 * - the schema assigned to each database
 */
// schema documentation only - not implemented right now
export interface ClusterStore {
    // --- containers
    containers  : { [key: string]: DbContainers };
    messages    : { [key: string]: DbMessages };
    schemas     : { [key: string]: DbSchema };

    // --- commands
    ["ModelFiles"]                  (param: ModelFilesQuery | null) : ModelFiles[];
    /** Echos the given parameter to assure the database is working appropriately. */
    ["std.Echo"]                    (param: any) : any;
    /** A command that completes after a specified number of milliseconds. */
    ["std.Delay"]                   (param: int32) : int32;
    /** List all database containers */
    ["std.Containers"]              () : DbContainers;
    /** List all database commands and messages */
    ["std.Messages"]                () : DbMessages;
    /** Return the Schema assigned to the database */
    ["std.Schema"]                  () : DbSchema;
    /** Return the number of entities of all containers (or the given container) of the database */
    ["std.Stats"]                   (param: string | null) : DbStats;
    /**
     * Begin a transaction containing all subsequent **SyncTask**'s.  
     * The transaction ends by either calling **SyncTasks** or explicit by
     * **TransactionCommit** / **TransactionRollback**
     */
    ["std.TransactionBegin"]        () : TransactionResult;
    /** Commit a transaction started previously with **TransactionBegin** */
    ["std.TransactionCommit"]       () : TransactionResult;
    /** Rollback a transaction started previously with **TransactionBegin** */
    ["std.TransactionRollback"]     () : TransactionResult;
    /** Execute a raw SQL query / statement */
    ["std.ExecuteRawSQL"]           (param: string | null) : RawSqlResult;
    /** Returns general information about the Hub like version, host, project and environment name */
    ["std.Host"]                    (param: HostParam | null) : HostInfo;
    /** List all databases and their containers hosted by the Hub */
    ["std.Cluster"]                 () : HostCluster;
    /** Return the groups of the current user. Optionally change the groups of the current user */
    ["std.User"]                    (param: UserParam | null) : UserResult;
    /** Return client specific infos and adjust general client behavior like **queueEvents** */
    ["std.Client"]                  (param: ClientParam | null) : ClientResult;
}

/** **containers** and **storage** type of a database */
export class DbContainers {
    /** database name */
    id          : string;
    /** **storage** type. e.g. memory, file-system, ... */
    storage     : string;
    /** list of database **containers** */
    containers  : string[];
    /** true if the database is the default database of a Hub */
    defaultDB?  : boolean | null;
}

/** **commands** and **messages** of a database */
export class DbMessages {
    /** database name */
    id        : string;
    /** list of database **commands** */
    commands  : string[];
    /** list of database **messages** */
    messages  : string[];
}

/**
 * A **DbSchema** can be assigned to a database to specify its **containers**, **commands** and **messages**.  
 * The types used by the Schema are declared within **jsonSchemas**.  
 * The type referenced by the tuple **schemaName** / **schemaPath** specify the
 * database containers, commands and messages.
 */
export class DbSchema {
    /** database name */
    id           : string;
    /** refer a type definition of the JSON Schema referenced with **schemaPath** */
    schemaName   : string;
    /** refer a JSON Schema in **jsonSchemas** */
    schemaPath   : string;
    /**
     * map of **JSON Schemas** each containing a set of type definitions.  
     * Each JSON Schema is identified by its unique path
     */
    jsonSchemas  : { [key: string]: any };
}

export class ModelFilesQuery {
    /** specific database or null to retrieve all available models */
    db?   : string | null;
    /** specific model type - e.g. 'typescript' or null to retrieve all available model types */
    type? : string | null;
}

export class ModelFiles {
    db     : string;
    type   : string;
    label  : string;
    files  : ModelFile[];
}

export class ModelFile {
    path     : string;
    content  : string;
}

/** list of container statistics. E.g. the number of entities per container */
export class DbStats {
    /** list of container statistics - number of entities per container */
    containers? : ContainerStats[] | null;
}

/** statistics of a single container. E.g. the number of entities in a container */
export class ContainerStats {
    /** container name */
    name   : string;
    /** number of entities / records within a container */
    count  : int64;
}

/** return the execution result performed by a transaction. */
export class TransactionResult {
    /**
     * The execution performed by the transaction.  
     * In case any task in the transaction failed the transaction performs a **Rollback**
     */
    executed  : TransactionCommand;
}

export type TransactionCommand =
    | "Commit"
    | "Rollback"
;

export class RawSqlResult {
    rows     : int32;
    columns  : int32;
    values?  : JsonKey[] | null;
}

export class HostParam {
    memory?    : boolean | null;
    gcCollect? : boolean | null;
}

/** general information about a Hub */
export class HostInfo {
    /** host name used to identify a specific host in a network. */
    hostName        : string;
    /** host version */
    hostVersion     : string;
    /** Fliox library version */
    flioxVersion    : string;
    /** project name */
    projectName?    : string | null;
    /** link to a website describing project and Hub */
    projectWebsite? : string | null;
    /** environment name. e.g. 'dev', 'test', 'staging', 'prod' */
    envName?        : string | null;
    /**
     * the color used to display the environment name in GUI's using CSS color format.  
     * E.g. using red for a production environment: "#ff0000" or "rgb(255 0 0)"
     */
    envColor?       : string | null;
    /** is true if host support Pub-Sub. */
    pubSub          : boolean;
    /** routes configures by **HttpHost** - commonly below /fliox */
    routes          : string[];
    memory?         : HostMemory | null;
}

export class HostMemory {
    totalAllocatedBytes  : int64;
    totalMemory          : int64;
    gc?                  : HostGCMemory | null;
}

/** See [GCMemoryInfo](https://learn.microsoft.com/en-us/dotnet/api/system.gcmemoryinfo) */
export class HostGCMemory {
    highMemoryLoadThresholdBytes  : int64;
    totalAvailableMemoryBytes     : int64;
    memoryLoadBytes               : int64;
    heapSizeBytes                 : int64;
    fragmentedBytes               : int64;
}

/** All **databases** hosted by Hub */
export class HostCluster {
    /** list of **databases** hosted by Hub */
    databases  : DbContainers[];
}

export class UserParam {
    addGroups?    : string[] | null;
    removeGroups? : string[] | null;
}

export class UserResult {
    roles    : string[];
    groups   : string[];
    clients  : string[];
    /** number executed requests and tasks per database */
    counts   : RequestCount[];
}

/** number of requests and tasks executed per database */
export class RequestCount {
    /** database name */
    db?       : string | null;
    /** number of executed requests */
    requests  : int32;
    /** number of executed tasks */
    tasks     : int32;
}

export class ClientParam {
    /** Return the client id set in **SyncRequest** or creates a new one in case is was not set. */
    ensureClientId? : boolean | null;
    /**
     * If **false** the hub try to send events to a client when the events are emitted.
     * Sending events to a disconnected client will never arrive.   
     * If **true** the hub will store all unacknowledged events for a client in a FIFO queue and send them on reconnects.
     */
    queueEvents?    : boolean | null;
}

export class ClientResult {
    /** returns true if the host queue events for the client in case of disconnects */
    queueEvents         : boolean;
    /**
     * return number of queued events not acknowledged by the client.
     * Events are queued only if the client instruct the Hub to queue events by setting **queueEvents** = true
     */
    queuedEvents        : int32;
    /**
     * return the client id set in the **SyncRequest**. Can be null.  
     * A new client id is created in case any task requires a client id and the **SyncRequest** did not set a client id.  
     * E.g. **ensureClientId** = true or **queueEvents** = true
     */
    clientId?           : string | null;
    /** number of sent or queued client events and its message and change subscriptions */
    subscriptionEvents? : SubscriptionEvents | null;
}

/** number of sent or queued client events and its message and change subscriptions */
export class SubscriptionEvents {
    /** number of events sent to a client */
    seq          : int32;
    /** number of queued events not acknowledged by a client */
    queued       : int32;
    /** true if client is instructed to queue events for reliable event delivery in case of reconnects */
    queueEvents  : boolean;
    /** true if client is connected. Non remote client are always connected */
    connected    : boolean;
    /**
     * The endpoint of the client events are sent to.  
     * E.g. ws:[::1]:52089 for WebSockets, udp:127.0.0.1:60005 for UDP or in-process
     */
    endpoint?    : string | null;
    /** message / command subscriptions of a client */
    messageSubs? : string[] | null;
    /** change subscriptions of a client */
    changeSubs?  : ChangeSubscription[] | null;
}

/** change subscription for a specific container */
export class ChangeSubscription {
    /** name of subscribed container */
    container  : string;
    /** type of subscribed changes like create, upsert, delete and patch */
    changes    : ChangeType[];
    /** filter to narrow the amount of change events */
    filter?    : string | null;
}

/** Filter type used to specify the type of an entity change */
export type ChangeType =
    | "create"      /** filter change events of created entities. */
    | "upsert"      /** filter change events of upserted entities. */
    | "merge"       /** filter change events of entity patches. */
    | "delete"      /** filter change events of deleted entities. */
;

