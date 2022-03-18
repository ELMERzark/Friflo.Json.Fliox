// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { TaskType } from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { Change }   from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";

export type Right_Union =
    | RightAllow
    | RightTask
    | RightSendMessage
    | RightSubscribeMessage
    | RightOperation
    | RightPredicate
;

export abstract class Right {
    abstract type:
        | "allow"
        | "task"
        | "sendMessage"
        | "subscribeMessage"
        | "operation"
        | "predicate"
    ;
    /** optional description explaining the Right */
    description? : string | null;
}

/**
 * Allow full access to the given **database**.  
 * In case **database** ends with a '*' e.g. 'test*' access to all databases with the prefix 'test'
 * is granted.  
 * Using **database**: '*' grant access to all databases.
 */
export class RightAllow extends Right {
    type         : "allow";
    database?    : string | null;
}

/** **RightTask** grant **database** access by a set of task **types**.    */
export class RightTask extends Right {
    type         : "task";
    database?    : string | null;
    /** set fo task types like: create, read, upsert, delete, query, ... */
    types        : TaskType[];
}

/**
 * **RightSendMessage** allows sending messages to a **database** by a set of **names**.  
 * Each allowed message can be listed explicit in **names**. E.g. 'std.Echo'   
 * A group of messages can be allowed by using a prefix. E.g. 'std.*'   
 * To grant sending every message independent of its name use: '*'    
 * Note: commands are messages - so permission of sending commands is same as for messages.
 */
export class RightSendMessage extends Right {
    type         : "sendMessage";
    database?    : string | null;
    names        : string[];
}

/**
 * **RightSubscribeMessage** allows subscribing messages send to a **database**.  
 * Allow subscribing a specific message by using explicit message **names**. E.g. 'std.Echo'   
 * Allow subscribing a group of messages by using a prefix. E.g. 'std.*'   
 * Allow subscribing all messages by using: '*'    
 * Note: commands are messages - so permission of subscribing commands is same as for messages.
 */
export class RightSubscribeMessage extends Right {
    type         : "subscribeMessage";
    database?    : string | null;
    names        : string[];
}

/**
 * **RightOperation** grant **database** access for the given **containers**
 * based on a set of **operations**.   
 * E.g. create, read, upsert, delete, query or aggregate (count)  
 * It also allows subscribing database changes by **subscribeChanges**
 */
export class RightOperation extends Right {
    type         : "operation";
    database?    : string | null;
    containers   : { [key: string]: ContainerAccess };
}

/** Grant execution of specific container operations and subscriptions */
export class ContainerAccess {
    /** Set of granted operation types */
    operations?       : OperationType[] | null;
    /** Set of granted change subscriptions */
    subscribeChanges? : Change[] | null;
}

export type OperationType =
    | "create"         /** allow to create entities in a container */
    | "upsert"         /** allow to upsert entities in a container */
    | "delete"         /** allow to delete entities in a container */
    | "deleteAll"      /** allow to delete all container entities */
    | "patch"          /** allow to patch entities in a container */
    | "read"           /** allow to read entities in a container */
    | "query"          /** allow to query entities in a container */
    | "aggregate"      /** allow to aggregate - count - entities in a container */
    | "mutate"         /** allow to mutate - create, upsert, delete and patch - entities in a container */
    | "full"           /** allow all operation types in a container */
;

export class RightPredicate extends Right {
    type         : "predicate";
    names        : string[];
}

