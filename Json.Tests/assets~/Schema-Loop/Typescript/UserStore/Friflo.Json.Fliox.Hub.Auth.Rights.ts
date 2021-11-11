// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { TaskType } from "./Friflo.Json.Fliox.Hub.Protocol.Tasks"
import { Change }   from "./Friflo.Json.Fliox.Hub.Protocol.Tasks"

export type Right_Union =
    | RightAllow
    | RightTask
    | RightMessage
    | RightSubscribeMessage
    | RightAccess
    | RightPredicate
;

export abstract class Right {
    abstract type:
        | "allow"
        | "task"
        | "message"
        | "subscribeMessage"
        | "access"
        | "predicate"
    ;
    description? : string | null;
}

export class RightAllow extends Right {
    type         : "allow";
    database?    : string | null;
}

export class RightTask extends Right {
    type         : "task";
    database?    : string | null;
    types        : TaskType[];
}

export class RightMessage extends Right {
    type         : "message";
    database?    : string | null;
    names        : string[];
}

export class RightSubscribeMessage extends Right {
    type         : "subscribeMessage";
    database?    : string | null;
    names        : string[];
}

export class RightAccess extends Right {
    type         : "access";
    database?    : string | null;
    containers   : { [key: string]: ContainerAccess };
}

export class ContainerAccess {
    operations?       : OperationType[] | null;
    subscribeChanges? : Change[] | null;
}

export type OperationType =
    | "create"
    | "upsert"
    | "delete"
    | "deleteAll"
    | "patch"
    | "read"
    | "query"
    | "mutate"
    | "full"
;

export class RightPredicate extends Right {
    type         : "predicate";
    names        : string[];
}

