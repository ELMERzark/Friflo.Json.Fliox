// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { int32 }                 from "./Standard"
import { Guid }                  from "./Standard"
import { FilterOperation }       from "./Friflo.Json.Fliox.Transform"
import { FilterOperation_Union } from "./Friflo.Json.Fliox.Transform"
import { JsonPatch }             from "./Friflo.Json.Fliox.Transform"
import { JsonPatch_Union }       from "./Friflo.Json.Fliox.Transform"
import { int64 }                 from "./Standard"

export class DatabaseMessage {
    req?  : DatabaseRequest_Union | null;
    resp? : DatabaseResponse_Union | null;
    ev?   : DatabaseEvent_Union | null;
}

export type DatabaseRequest_Union =
    | SyncRequest
;

export abstract class DatabaseRequest {
    abstract type:
        | "sync"
    ;
    reqId? : int32 | null;
}

export class SyncRequest extends DatabaseRequest {
    type    : "sync";
    client? : string | null;
    ack?    : int32 | null;
    token?  : string | null;
    tasks   : DatabaseTask_Union[];
}

export type DatabaseTask_Union =
    | CreateEntities
    | UpsertEntities
    | ReadEntitiesList
    | QueryEntities
    | PatchEntities
    | DeleteEntities
    | SendMessage
    | SubscribeChanges
    | SubscribeMessage
    | ReserveKeys
;

export abstract class DatabaseTask {
    abstract task:
        | "create"
        | "upsert"
        | "read"
        | "query"
        | "patch"
        | "delete"
        | "message"
        | "subscribeChanges"
        | "subscribeMessage"
        | "reserveKeys"
    ;
}

export class CreateEntities extends DatabaseTask {
    task           : "create";
    container      : string;
    reservedToken? : Guid | null;
    keyName?       : string | null;
    entities       : any[];
}

export class UpsertEntities extends DatabaseTask {
    task       : "upsert";
    container  : string;
    keyName?   : string | null;
    entities   : any[];
}

export class ReadEntitiesList extends DatabaseTask {
    task       : "read";
    container  : string;
    keyName?   : string | null;
    isIntKey?  : boolean | null;
    reads      : ReadEntities[];
}

export class ReadEntities {
    ids         : string[];
    references? : References[] | null;
}

export class References {
    selector    : string;
    container   : string;
    keyName?    : string | null;
    isIntKey?   : boolean | null;
    references? : References[] | null;
}

export class QueryEntities extends DatabaseTask {
    task        : "query";
    container   : string;
    keyName?    : string | null;
    isIntKey?   : boolean | null;
    filterLinq? : string | null;
    filter?     : FilterOperation_Union | null;
    references? : References[] | null;
}

export class PatchEntities extends DatabaseTask {
    task       : "patch";
    container  : string;
    keyName?   : string | null;
    patches    : EntityPatch[];
}

export class EntityPatch {
    key         : string;
    operations  : JsonPatch_Union[];
}

export class DeleteEntities extends DatabaseTask {
    task       : "delete";
    container  : string;
    ids?       : string[] | null;
    all?       : boolean | null;
}

export class SendMessage extends DatabaseTask {
    task   : "message";
    name   : string;
    value  : any;
}

export class SubscribeChanges extends DatabaseTask {
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

export class SubscribeMessage extends DatabaseTask {
    task    : "subscribeMessage";
    name    : string;
    remove? : boolean | null;
}

export class ReserveKeys extends DatabaseTask {
    task       : "reserveKeys";
    container  : string;
    count      : int32;
}

export type DatabaseResponse_Union =
    | SyncResponse
    | ErrorResponse
;

export abstract class DatabaseResponse {
    abstract type:
        | "sync"
        | "error"
    ;
    reqId? : int32 | null;
}

export class SyncResponse extends DatabaseResponse {
    type          : "sync";
    error?        : ErrorResponse | null;
    tasks?        : TaskResult_Union[] | null;
    results?      : ContainerEntities[] | null;
    createErrors? : EntityErrors[] | null;
    upsertErrors? : EntityErrors[] | null;
    patchErrors?  : EntityErrors[] | null;
    deleteErrors? : EntityErrors[] | null;
}

export class ErrorResponse extends DatabaseResponse {
    type     : "error";
    message? : string | null;
}

export type TaskResult_Union =
    | CreateEntitiesResult
    | UpsertEntitiesResult
    | ReadEntitiesListResult
    | QueryEntitiesResult
    | PatchEntitiesResult
    | DeleteEntitiesResult
    | SendMessageResult
    | SubscribeChangesResult
    | SubscribeMessageResult
    | ReserveKeysResult
    | TaskErrorResult
;

export abstract class TaskResult {
    abstract task:
        | "create"
        | "upsert"
        | "read"
        | "query"
        | "patch"
        | "delete"
        | "message"
        | "subscribeChanges"
        | "subscribeMessage"
        | "reserveKeys"
        | "error"
    ;
}

export class CreateEntitiesResult extends TaskResult {
    task   : "create";
    Error? : CommandError | null;
}

export class CommandError {
    message? : string | null;
}

export class UpsertEntitiesResult extends TaskResult {
    task   : "upsert";
    Error? : CommandError | null;
}

export class ReadEntitiesListResult extends TaskResult {
    task   : "read";
    reads  : ReadEntitiesResult[];
}

export class ReadEntitiesResult {
    Error?      : CommandError | null;
    references  : ReferencesResult[];
}

export class ReferencesResult {
    error?      : string | null;
    container?  : string | null;
    ids         : string[];
    references? : ReferencesResult[] | null;
}

export class QueryEntitiesResult extends TaskResult {
    task        : "query";
    Error?      : CommandError | null;
    container?  : string | null;
    filterLinq? : string | null;
    ids         : string[];
    references? : ReferencesResult[] | null;
}

export class PatchEntitiesResult extends TaskResult {
    task   : "patch";
    Error? : CommandError | null;
}

export class DeleteEntitiesResult extends TaskResult {
    task   : "delete";
    Error? : CommandError | null;
}

export class SendMessageResult extends TaskResult {
    task    : "message";
    Error?  : CommandError | null;
    result? : any | null;
}

export class SubscribeChangesResult extends TaskResult {
    task  : "subscribeChanges";
}

export class SubscribeMessageResult extends TaskResult {
    task  : "subscribeMessage";
}

export class ReserveKeysResult extends TaskResult {
    task   : "reserveKeys";
    Error? : CommandError | null;
    keys?  : ReservedKeys | null;
}

export class ReservedKeys {
    start  : int64;
    count  : int32;
    token  : Guid;
}

export class TaskErrorResult extends TaskResult {
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

export class ContainerEntities {
    container  : string;
    entities   : any[];
    notFound?  : string[] | null;
    errors?    : EntityError[] | null;
}

export class EntityError {
    key      : string;
    type     : EntityErrorType;
    message? : string | null;
}

export type EntityErrorType =
    | "Undefined"
    | "ParseError"
    | "ReadError"
    | "WriteError"
    | "DeleteError"
    | "PatchError"
;

export class EntityErrors {
    container  : string;
    errors?    : EntityError[] | null;
}

export type DatabaseEvent_Union =
    | SubscriptionEvent
;

export abstract class DatabaseEvent {
    abstract type:
        | "subscription"
    ;
    seq     : int32;
    target? : string | null;
    client? : string | null;
}

export class SubscriptionEvent extends DatabaseEvent {
    type    : "subscription";
    tasks?  : DatabaseTask_Union[] | null;
}

