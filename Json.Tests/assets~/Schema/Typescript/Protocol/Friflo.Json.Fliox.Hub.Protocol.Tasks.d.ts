// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { Guid }             from "./Standard";
import { References }       from "./Friflo.Json.Fliox.Hub.Protocol.Models";
import { int32 }            from "./Standard";
import { JsonPatch }        from "./Friflo.Json.Fliox.Transform";
import { JsonPatch_Union }  from "./Friflo.Json.Fliox.Transform";
import { EntityError }      from "./Friflo.Json.Fliox.Hub.Protocol.Models";
import { ReferencesResult } from "./Friflo.Json.Fliox.Hub.Protocol.Models";
import { double }           from "./Standard";
import { int64 }            from "./Standard";

/**
 * Polymorphic base type for all tasks.  
 * All tasks fall into two categories:  **container operations** like: create, read, upsert, delete, query, ...  **database operation** like sending commands or messages
 */
export type SyncRequestTask_Union =
    | CreateEntities
    | UpsertEntities
    | ReadEntities
    | QueryEntities
    | AggregateEntities
    | PatchEntities
    | MergeEntities
    | DeleteEntities
    | SendMessage
    | SendCommand
    | CloseCursors
    | SubscribeChanges
    | SubscribeMessage
    | ReserveKeys
;

export abstract class SyncRequestTask {
    /** task type */
    abstract task:
        | "create"
        | "upsert"
        | "read"
        | "query"
        | "aggregate"
        | "patch"
        | "merge"
        | "delete"
        | "message"
        | "command"
        | "closeCursors"
        | "subscribeChanges"
        | "subscribeMessage"
        | "reserveKeys"
    ;
    info? : any | null;
}

/** Create the given **entities** in the specified **container** */
export class CreateEntities extends SyncRequestTask {
    /** task type */
    task           : "create";
    /** container name the **entities** are created */
    container      : string;
    reservedToken? : Guid | null;
    /** name of the primary key property in **entities** */
    keyName?       : string | null;
    /** the **entities** which are created in the specified **container** */
    entities       : any[];
}

/** Upsert the given **entities** in the specified **container** */
export class UpsertEntities extends SyncRequestTask {
    /** task type */
    task       : "upsert";
    /** container name the **entities** are upserted - created or updated */
    container  : string;
    /** name of the primary key property in **entities** */
    keyName?   : string | null;
    /** the **entities** which are upserted in the specified **container** */
    entities   : any[];
}

/**
 * Read entities by id from the specified **container** using given list of **ids**  
 * To return also entities referenced by entities listed in **ids** use **references**.   
 * This mimic the functionality of a **LEFT JOIN** in **SQL**
 */
export class ReadEntities extends SyncRequestTask {
    /** task type */
    task        : "read";
    /** container name */
    container   : string;
    /** name of the primary key property of the returned entities */
    keyName?    : string | null;
    isIntKey?   : boolean | null;
    /** list of requested entity **ids** */
    ids         : string[];
    /** used to request the entities referenced by properties of a read task result */
    references? : References[] | null;
}

/**
 * Query entities from the given **container** using a **filter**  
 * To return entities referenced by fields of the query result use **references**
 */
export class QueryEntities extends SyncRequestTask {
    /** task type */
    task        : "query";
    /** container name */
    container   : string;
    /** name of the primary key property of the returned entities */
    keyName?    : string | null;
    isIntKey?   : boolean | null;
    /**
     * query filter as JSON tree.   
     * Is used in favour of **filter** as its serialization is more performant
     */
    filterTree? : any | null;
    /**
     * query filter as a [Lambda expression](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions)
     * returning a boolean value. E.g.  `o => o.name == 'Smartphone'`
     * if **filterTree** is assigned it has priority
     */
    filter?     : string | null;
    /** used to request the entities referenced by properties of the query task result */
    references? : References[] | null;
    /** limit the result set to the given number */
    limit?      : int32 | null;
    /** execute a cursor request with the specified **maxCount** number of entities in the result. */
    maxCount?   : int32 | null;
    /** specify the **cursor** of a previous cursor request */
    cursor?     : string | null;
}

/** Aggregate - count - entities from the given **container** using a **filter**   */
export class AggregateEntities extends SyncRequestTask {
    /** task type */
    task        : "aggregate";
    /** container name */
    container   : string;
    /** aggregation type - e.g. count */
    type        : AggregateType;
    /**
     * aggregation filter as JSON tree.   
     * Is used in favour of **filter** as its serialization is more performant
     */
    filterTree? : any | null;
    /**
     * aggregation filter as a [Lambda expression](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions)
     * returning a boolean value. E.g.  `o => o.name == 'Smartphone'`
     */
    filter?     : string | null;
}

/** Aggregation type used in **AggregateEntities** */
export type AggregateType =
    | "count"      /** count entities */
;

/**
 * Patch entities by id in the given **container**  
 * Each **EntityPatch** in **patches** contains a set of **patches** for each entity.
 */
export class PatchEntities extends SyncRequestTask {
    /** task type */
    task       : "patch";
    /** container name */
    container  : string;
    /** name of the primary key property of the entity **patches** */
    keyName?   : string | null;
    /** list of patches for each entity */
    patches    : EntityPatch[];
}

/** Contains the **patches** applied to an entity. Used by **PatchEntities** */
export class EntityPatch {
    id       : string;
    /** list of patches applied to an entity */
    patches  : JsonPatch_Union[];
}

/** Merge entities by id in the given **container**   */
export class MergeEntities extends SyncRequestTask {
    /** task type */
    task       : "merge";
    /** container name */
    container  : string;
    /** name of the primary key property of the entity **patches** */
    keyName?   : string | null;
    /** list of merge patches for each entity */
    patches    : any[];
}

/**
 * Delete entities by id in the given **container**  
 * The entities which will be deleted are listed in **ids**
 */
export class DeleteEntities extends SyncRequestTask {
    /** task type */
    task       : "delete";
    /** container name */
    container  : string;
    /** list of **ids** requested for deletion */
    ids?       : string[] | null;
    /** if true all entities in the specified **container** are deleted */
    all?       : boolean | null;
}

/**
 * Used as base type for **SendMessage** or **SendCommand** to specify the command / message
 * **name** and **param**.   
 * In case **users** or **clients** is set the Hub forward the message as an event only to the
 * given **users** or **clients**.
 */
export abstract class SyncMessageTask extends SyncRequestTask {
    /** command / message name */
    name     : string;
    /** command / message parameter. Can be null or absent */
    param?   : any | null;
    /** if set the Hub forward the message as an event only to given **users** */
    users?   : string[] | null;
    /** if set the Hub forward the message as an event only to given **clients** */
    clients? : string[] | null;
    /** if set the Hub forward the message as an event only to given **groups** */
    groups?  : string[] | null;
}

/**
 * Send a database message with the given **param**.   
 * In case **users** or **clients** is set the Hub forward
 * the message as an event only to the given **users** or **clients**.
 */
export class SendMessage extends SyncMessageTask {
    /** task type */
    task     : "message";
}

/**
 * Send a database command with the given **param**.   
 * In case **users** or **clients** is set the Hub forward
 * the message as an event only to the given **users** or **clients**.
 */
export class SendCommand extends SyncMessageTask {
    /** task type */
    task     : "command";
}

/** Close the **cursors** of the given **container** */
export class CloseCursors extends SyncRequestTask {
    /** task type */
    task       : "closeCursors";
    /** container name */
    container  : string;
    /** list of **cursors** */
    cursors?   : string[] | null;
}

/** Subscribe to specific **changes** of the specified **container** using the given **filter** */
export class SubscribeChanges extends SyncRequestTask {
    /** task type */
    task       : "subscribeChanges";
    /** container name */
    container  : string;
    /** subscribe to entity **changes** of the given **container** */
    changes    : EntityChange[];
    /**
     * subscription filter as a [Lambda expression](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions) (infix notation)
     * returning a boolean value. E.g. o.name == 'Smartphone'
     */
    filter?    : string | null;
}

/** Filter type used to specify the type of an entity change */
export type EntityChange =
    | "create"      /** filter change events of created entities. */
    | "upsert"      /** filter change events of upserted entities. */
    | "patch"       /** filter change events of entity patches. */
    | "delete"      /** filter change events of deleted entities. */
;

/**
 * Subscribe to commands and messages sent to a database by their **name**  
 * Unsubscribe by setting **remove** to true
 */
export class SubscribeMessage extends SyncRequestTask {
    /** task type */
    task    : "subscribeMessage";
    /** subscribe a specific message: 'std.Echo', multiple messages by prefix: 'std.*', all messages: '*' */
    name    : string;
    /** if true a previous added subscription is removed. Otherwise added */
    remove? : boolean | null;
}

/** WIP */
export class ReserveKeys extends SyncRequestTask {
    /** task type */
    task       : "reserveKeys";
    container  : string;
    count      : int32;
}

export type SyncTaskResult_Union =
    | CreateEntitiesResult
    | UpsertEntitiesResult
    | ReadEntitiesResult
    | QueryEntitiesResult
    | AggregateEntitiesResult
    | PatchEntitiesResult
    | MergeEntitiesResult
    | DeleteEntitiesResult
    | SendMessageResult
    | SendCommandResult
    | CloseCursorsResult
    | SubscribeChangesResult
    | SubscribeMessageResult
    | ReserveKeysResult
    | TaskErrorResult
;

export abstract class SyncTaskResult {
    /** task result type */
    abstract task:
        | "create"
        | "upsert"
        | "read"
        | "query"
        | "aggregate"
        | "patch"
        | "merge"
        | "delete"
        | "message"
        | "command"
        | "closeCursors"
        | "subscribeChanges"
        | "subscribeMessage"
        | "reserveKeys"
        | "error"
    ;
}

/** Result of a **CreateEntities** task */
export class CreateEntitiesResult extends SyncTaskResult {
    /** task result type */
    task    : "create";
    /** list of entity errors failed to create */
    errors? : EntityError[] | null;
}

/** Result of a **UpsertEntities** task */
export class UpsertEntitiesResult extends SyncTaskResult {
    /** task result type */
    task    : "upsert";
    /** list of entity errors failed to upsert */
    errors? : EntityError[] | null;
}

/** Result of a **ReadEntities** task */
export class ReadEntitiesResult extends SyncTaskResult {
    /** task result type */
    task        : "read";
    references? : ReferencesResult[] | null;
}

/** Result of a **QueryEntities** task */
export class QueryEntitiesResult extends SyncTaskResult {
    /** task result type */
    task        : "query";
    /** container name - not utilized by Protocol */
    container?  : string | null;
    cursor?     : string | null;
    /** number of **ids** - not utilized by Protocol */
    count?      : int32 | null;
    ids         : string[];
    references? : ReferencesResult[] | null;
}

/** Result of a **AggregateEntities** task */
export class AggregateEntitiesResult extends SyncTaskResult {
    /** task result type */
    task       : "aggregate";
    /** container name - not utilized by Protocol */
    container? : string | null;
    value?     : double | null;
}

/** Result of a **PatchEntities** task */
export class PatchEntitiesResult extends SyncTaskResult {
    /** task result type */
    task    : "patch";
    /** list of entity errors failed to patch */
    errors? : EntityError[] | null;
}

/** Result of a **MergeEntities** task */
export class MergeEntitiesResult extends SyncTaskResult {
    /** task result type */
    task    : "merge";
    /** list of entity errors failed to patch */
    errors? : EntityError[] | null;
}

/** Result of a **DeleteEntities** task */
export class DeleteEntitiesResult extends SyncTaskResult {
    /** task result type */
    task    : "delete";
    /** list of entity errors failed to delete */
    errors? : EntityError[] | null;
}

export abstract class SyncMessageResult extends SyncTaskResult {
}

/** Result of a **SendMessage** task */
export class SendMessageResult extends SyncMessageResult {
    /** task result type */
    task  : "message";
}

/** Result of a **SendCommand** task */
export class SendCommandResult extends SyncMessageResult {
    /** task result type */
    task    : "command";
    result? : any | null;
}

/** Result of a **CloseCursors** task */
export class CloseCursorsResult extends SyncTaskResult {
    /** task result type */
    task   : "closeCursors";
    count  : int32;
}

/** Result of a **SubscribeChanges** task */
export class SubscribeChangesResult extends SyncTaskResult {
    /** task result type */
    task  : "subscribeChanges";
}

/** Result of a **SubscribeMessage** task */
export class SubscribeMessageResult extends SyncTaskResult {
    /** task result type */
    task  : "subscribeMessage";
}

/** WIP */
export class ReserveKeysResult extends SyncTaskResult {
    /** task result type */
    task  : "reserveKeys";
    keys? : ReservedKeys | null;
}

/** WIP */
export class ReservedKeys {
    start  : int64;
    count  : int32;
    token  : Guid;
}

/** A **TaskErrorResult** is returned in case execution of a **SyncRequestTask** failed */
export class TaskErrorResult extends SyncTaskResult {
    /** task result type */
    task        : "error";
    /** task error type */
    type        : TaskErrorResultType;
    /** task error details */
    message?    : string | null;
    /** stacktrace in case the error **type** is a **UnhandledException** */
    stacktrace? : string | null;
}

/** Type of a task error used in **TaskErrorResult** */
export type TaskErrorResultType =
    | "None"
    | "UnhandledException"      /**
       * Unhandled exception while executing a task.  
       * maps to HTTP status: 500
       */
    | "DatabaseError"           /**
       * General database error while task execution.  
       * E.g. the access is currently not available or accessing a missing table.  
       * maps to HTTP status: 500
       */
    | "FilterError"             /** Invalid query filter      maps to HTTP status: 400 */
    | "ValidationError"         /** Schema validation of an entity failed     maps to HTTP status: 400 */
    | "CommandError"            /** Execution of message / command failed caused by invalid input    maps to HTTP status: 400 */
    | "InvalidTask"             /** Invalid task. E.g. by using an invalid task parameter    maps to HTTP status: 400 */
    | "NotImplemented"          /** database message / command not implemented       maps to HTTP status: 501 */
    | "PermissionDenied"        /** task execution not authorized    maps to HTTP status: 403 */
    | "SyncError"               /** The entire **SyncRequest** containing a task failed    maps to HTTP status: 500 */
;

