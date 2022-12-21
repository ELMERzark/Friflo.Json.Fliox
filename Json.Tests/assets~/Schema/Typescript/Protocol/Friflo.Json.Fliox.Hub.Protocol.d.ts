// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { int32 }                 from "./Standard";
import { SyncRequestTask }       from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { SyncRequestTask_Union } from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { SyncTaskResult }        from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { SyncTaskResult_Union }  from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { EntityError }           from "./Friflo.Json.Fliox.Hub.Protocol.Models";

/** **ProtocolMessage** is the base type for all messages which are classified into request, response and event. */
export type ProtocolMessage_Union =
    | SyncRequest
    | SyncResponse
    | ErrorResponse
    | EventMessage
;

export abstract class ProtocolMessage {
    /** message type */
    abstract msg:
        | "sync"
        | "resp"
        | "error"
        | "ev"
    ;
}

export type ProtocolRequest_Union =
    | SyncRequest
;

export abstract class ProtocolRequest extends ProtocolMessage {
    /** request type */
    abstract msg:
        | "sync"
    ;
    /**
     * Used only for **RemoteClientHub** to enable:
     * 
     * 1. Out of order response handling for their corresponding requests.
     * 
     * 2. Multiplexing of requests and their responses for multiple clients e.g. **FlioxClient**
     * using the same connection.
     * This is not a common scenario but it enables using a single **WebSocketClientHub**
     * used by multiple clients.
     * 
     * The host itself only echos the **reqId** to **reqId** and
     * does **not** utilize it internally.
     */
    req? : int32 | null;
    /**
     * As a user can access a **FlioxHub** by multiple clients the **clientId**
     * enables identifying each client individually.   
     * The **clientId** is used for **SubscribeMessage** and **SubscribeChanges**
     * to enable sending **SyncEvent**'s to the desired subscriber.
     */
    clt? : string | null;
}

/** A **SyncRequest** is sent to a **FlioxHub** targeting a specific **database**. */
export class SyncRequest extends ProtocolRequest {
    /** message type */
    msg       : "sync";
    /**
     * Identify the user performing a sync request.
     * In case using of using **UserAuthenticator** the **userId** and **token**
     * are use for user authentication.
     */
    user?     : string | null;
    token?    : string | null;
    /**
     * **eventAck** is used to ensure (change) events are delivered reliable.
     * A client set **eventAck** to the last received **seq** in case
     * it has subscribed to database changes by a **SubscribeChanges** task.
     * Otherwise **eventAck** is null.
     */
    ack?      : int32 | null;
    /** list of tasks either container operations or database commands / messages */
    tasks     : SyncRequestTask_Union[];
    /** database name the **tasks** apply to. null to access the default database */
    database? : string | null;
    /** optional JSON value - can be used to describe a request */
    info?     : any | null;
}

/** Base type for response messages send from a host to a client in reply of **SyncRequest** */
export type ProtocolResponse_Union =
    | SyncResponse
    | ErrorResponse
;

export abstract class ProtocolResponse extends ProtocolMessage {
    /** response type */
    abstract msg:
        | "resp"
        | "error"
    ;
    /** Set to the value of the corresponding **reqId** of a **ProtocolRequest** */
    req? : int32 | null;
    /**
     * Set to **clientId** of a **SyncRequest** in case the given
     * **clientId** was valid. Otherwise it is set to null.
     */
    clt? : string | null;
}

/** A **SyncResponse** is the response of **SyncRequest** executed by a **FlioxHub** */
export class SyncResponse extends ProtocolResponse {
    /** message type */
    msg         : "resp";
    /** for debugging - not used by Protocol */
    database?   : string | null;
    /** list of task results corresponding to the **tasks** in a **SyncRequest** */
    tasks?      : SyncTaskResult_Union[] | null;
    /**
     * entities as results from the **tasks** in a **SyncRequest**
     * grouped by container
     */
    containers? : ContainerEntities[] | null;
    info?       : any | null;
}

/**
 * Used by **SyncResponse** to return the **entities** as results
 * from **tasks** of a **SyncRequest**
 */
export class ContainerEntities {
    /** container name the of the returned **entities** */
    container  : string;
    /** number of **entities** - not utilized by Protocol */
    count?     : int32 | null;
    /**
     * all **entities** from the **container** resulting from
     * **ReadEntities** and **QueryEntities** tasks of a **SyncRequest**
     */
    entities   : any[];
    /** list of entities not found by **ReadEntities** tasks */
    notFound?  : string[] | null;
    /** list of entity errors read from **container** */
    errors?    : EntityError[] | null;
}

/** **ErrorResponse** is returned for a **SyncRequest** in case the whole requests failed */
export class ErrorResponse extends ProtocolResponse {
    /** response type */
    msg      : "error";
    /** error message */
    message? : string | null;
    /** error type: invalid request or execution exception */
    type     : ErrorResponseType;
}

export type ErrorResponseType =
    | "BadRequest"       /** Invalid JSON request or invalid request parameters. Maps to HTTP status code 400 (Bad Request) */
    | "Exception"        /** Internal exception. Maps to HTTP status code 500 (Internal Server Error) */
    | "BadResponse"      /** Invalid JSON response. Maps to HTTP status code 500 (Internal Server Error) */
;

export type ProtocolEvent_Union =
    | EventMessage
;

export abstract class ProtocolEvent extends ProtocolMessage {
    /** event type */
    abstract msg:
        | "ev"
    ;
    /**
     * The target client the event is sent to. This enables sharing a single (WebSocket) connection by multiple clients.
     * In many scenarios this property is redundant as every client uses a WebSocket exclusively.
     */
    clt  : string;
}

/**
 * Contains a set of **SyncEvent**'s. It is send as a push message to clients to deliver the events
 * subscribed by these clients.
 */
export class EventMessage extends ProtocolEvent {
    /** message type */
    msg     : "ev";
    /**
     * Increasing event sequence number starting with 1 for a specific target client **dstClientId**.
     * Each target client (subscriber) has its own sequence.
     */
    seq     : int32;
    /**
     * Each **SyncEvent** corresponds to a **SyncRequest** and contains the subscribed
     * messages and container changes in its **tasks** field
     */
    events? : SyncEvent[] | null;
}

/**
 * A **SyncEvent** corresponds to a **SyncRequest** and contains the subscribed
 * messages and container changes in its **tasks** field
 */
export class SyncEvent {
    /**
     * The user which caused the event. Specifically the user which made a database change or sent a message / command.
     * The user client is not preserved by en extra property as a use case for this is not obvious.
     */
    src       : string;
    /** Is true if the receiving client is the origin of the event */
    isOrigin? : boolean | null;
    /** The database the **tasks** refer to */
    db        : string;
    /**
     * Contains the events an application subscribed. These are:  **CreateEntities**, 
     * **UpsertEntities**, 
     * **DeleteEntities**,
     * **SendMessage**, 
     * **SendCommand**
     */
    tasks?    : SyncRequestTask_Union[] | null;
}

