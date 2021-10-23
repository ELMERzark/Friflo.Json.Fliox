// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { Guid }                  from "./Standard"
import { ReadEntitiesSet }       from "./Friflo.Json.Fliox.DB.Protocol.Models"
import { FilterOperation }       from "./Friflo.Json.Fliox.Transform"
import { FilterOperation_Union } from "./Friflo.Json.Fliox.Transform"
import { References }            from "./Friflo.Json.Fliox.DB.Protocol.Models"
import { JsonPatch }             from "./Friflo.Json.Fliox.Transform"
import { JsonPatch_Union }       from "./Friflo.Json.Fliox.Transform"
import { int32 }                 from "./Standard"
import { CommandError }          from "./Friflo.Json.Fliox.DB.Protocol.Models"
import { ReadEntitiesSetResult } from "./Friflo.Json.Fliox.DB.Protocol.Models"
import { ReferencesResult }      from "./Friflo.Json.Fliox.DB.Protocol.Models"
import { int64 }                 from "./Standard"

export type SyncRequestTask_Union =
    | CreateEntities
    | UpsertEntities
    | ReadEntities
    | QueryEntities
    | PatchEntities
    | DeleteEntities
    | SendMessage
    | SendCommand
    | SubscribeChanges
    | SubscribeMessage
    | ReserveKeys
;

export abstract class SyncRequestTask {
    abstract task:
        | "create"
        | "upsert"
        | "read"
        | "query"
        | "patch"
        | "delete"
        | "message"
        | "command"
        | "subscribeChanges"
        | "subscribeMessage"
        | "reserveKeys"
    ;
    info? : any | null;
}

export class CreateEntities extends SyncRequestTask {
    task           : "create";
    container      : string;
    reservedToken? : Guid | null;
    keyName?       : string | null;
    entities       : any[];
}

export class UpsertEntities extends SyncRequestTask {
    task       : "upsert";
    container  : string;
    keyName?   : string | null;
    entities   : any[];
}

export class ReadEntities extends SyncRequestTask {
    task       : "read";
    container  : string;
    keyName?   : string | null;
    isIntKey?  : boolean | null;
    reads      : ReadEntitiesSet[];
}

export class QueryEntities extends SyncRequestTask {
    task        : "query";
    container   : string;
    keyName?    : string | null;
    isIntKey?   : boolean | null;
    filter      : FilterOperation_Union;
    references? : References[] | null;
}

export class PatchEntities extends SyncRequestTask {
    task       : "patch";
    container  : string;
    keyName?   : string | null;
    patches    : { [key: string]: EntityPatch };
}

export class EntityPatch {
    patches  : JsonPatch_Union[];
}

export class DeleteEntities extends SyncRequestTask {
    task       : "delete";
    container  : string;
    ids?       : string[] | null;
    all?       : boolean | null;
}

export class SendMessage extends SyncRequestTask {
    task   : "message";
    name   : string;
    value? : any | null;
}

export class SendCommand extends SendMessage {
    task   : "command";
}

export class SubscribeChanges extends SyncRequestTask {
    task       : "subscribeChanges";
    container  : string;
    changes    : Change[];
    filter?    : FilterOperation_Union | null;
}

export type Change =
    | "create"
    | "upsert"
    | "patch"
    | "delete"
;

export class SubscribeMessage extends SyncRequestTask {
    task    : "subscribeMessage";
    name    : string;
    remove? : boolean | null;
}

export class ReserveKeys extends SyncRequestTask {
    task       : "reserveKeys";
    container  : string;
    count      : int32;
}

export type SyncTaskResult_Union =
    | CreateEntitiesResult
    | UpsertEntitiesResult
    | ReadEntitiesResult
    | QueryEntitiesResult
    | PatchEntitiesResult
    | DeleteEntitiesResult
    | SendMessageResult
    | SendCommandResult
    | SubscribeChangesResult
    | SubscribeMessageResult
    | ReserveKeysResult
    | TaskErrorResult
;

export abstract class SyncTaskResult {
    abstract task:
        | "create"
        | "upsert"
        | "read"
        | "query"
        | "patch"
        | "delete"
        | "message"
        | "command"
        | "subscribeChanges"
        | "subscribeMessage"
        | "reserveKeys"
        | "error"
    ;
}

export class CreateEntitiesResult extends SyncTaskResult {
    task   : "create";
    Error? : CommandError | null;
}

export class UpsertEntitiesResult extends SyncTaskResult {
    task   : "upsert";
    Error? : CommandError | null;
}

export class ReadEntitiesResult extends SyncTaskResult {
    task   : "read";
    reads  : ReadEntitiesSetResult[];
}

export class QueryEntitiesResult extends SyncTaskResult {
    task        : "query";
    Error?      : CommandError | null;
    container?  : string | null;
    ids         : string[];
    references? : ReferencesResult[] | null;
}

export class PatchEntitiesResult extends SyncTaskResult {
    task   : "patch";
    Error? : CommandError | null;
}

export class DeleteEntitiesResult extends SyncTaskResult {
    task   : "delete";
    Error? : CommandError | null;
}

export class SendMessageResult extends SyncTaskResult {
    task   : "message";
    Error? : CommandError | null;
}

export class SendCommandResult extends SyncTaskResult {
    task    : "command";
    Error?  : CommandError | null;
    result? : any | null;
}

export class SubscribeChangesResult extends SyncTaskResult {
    task  : "subscribeChanges";
}

export class SubscribeMessageResult extends SyncTaskResult {
    task  : "subscribeMessage";
}

export class ReserveKeysResult extends SyncTaskResult {
    task   : "reserveKeys";
    Error? : CommandError | null;
    keys?  : ReservedKeys | null;
}

export class ReservedKeys {
    start  : int64;
    count  : int32;
    token  : Guid;
}

export class TaskErrorResult extends SyncTaskResult {
    task        : "error";
    type        : TaskErrorResultType;
    message?    : string | null;
    stacktrace? : string | null;
}

export type TaskErrorResultType =
    | "None"
    | "UnhandledException"
    | "DatabaseError"
    | "InvalidTask"
    | "PermissionDenied"
    | "SyncError"
;

