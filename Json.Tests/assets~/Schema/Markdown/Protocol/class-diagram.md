[generated-by]: https://github.com/friflo/Friflo.Json.Fliox#schema

```mermaid
classDiagram
direction LR

class ProtocolMessage {
    <<abstract>>
}

ProtocolMessage <|-- ProtocolRequest
class ProtocolRequest {
    <<abstract>>
    req? : int32
    clt? : string
}

ProtocolRequest <|-- SyncRequest
class SyncRequest {
    msg       : "sync"
    user?     : string
    token?    : string
    ack?      : int32
    tasks     : SyncRequestTask[]
    database? : string
    info?     : any
}
SyncRequest *-- "0..*" SyncRequestTask : tasks

ProtocolMessage <|-- ProtocolResponse
class ProtocolResponse {
    <<abstract>>
    req? : int32
    clt? : string
}

ProtocolResponse <|-- SyncResponse
class SyncResponse {
    msg         : "resp"
    database?   : string
    tasks?      : SyncTaskResult[]
    containers? : ContainerEntities[]
    info?       : any
}
SyncResponse *-- "0..*" SyncTaskResult : tasks
SyncResponse *-- "0..*" ContainerEntities : containers

class ContainerEntities {
    container  : string
    count?     : int32
    entities   : any[]
    notFound?  : string[]
    errors?    : EntityError[]
}
ContainerEntities *-- "0..*" EntityError : errors

ProtocolResponse <|-- ErrorResponse
class ErrorResponse {
    msg      : "error"
    message? : string
    type     : ErrorResponseType
}
ErrorResponse *-- "1" ErrorResponseType : type

class ErrorResponseType:::cssEnum {
    <<enumeration>>
    BadRequest
    Exception
    BadResponse
}


ProtocolMessage <|-- ProtocolEvent
class ProtocolEvent {
    <<abstract>>
    seq  : int32
    src  : string
    clt  : string
}

ProtocolEvent <|-- EventMessage
class EventMessage {
    msg    : "ev"
    tasks? : SyncRequestTask[]
}
EventMessage *-- "0..*" SyncRequestTask : tasks

class References {
    selector    : string
    container   : string
    keyName?    : string
    isIntKey?   : boolean
    references? : References[]
}
References *-- "0..*" References : references

class EventTargetClient {
    user    : string
    client  : string
}

class EntityError {
    id       : string
    type     : EntityErrorType
    message? : string
}
EntityError *-- "1" EntityErrorType : type

class EntityErrorType:::cssEnum {
    <<enumeration>>
    Undefined
    ParseError
    ReadError
    WriteError
    DeleteError
    PatchError
}


class ReferencesResult {
    error?      : string
    container?  : string
    count?      : int32
    ids         : string[]
    references? : ReferencesResult[]
}
ReferencesResult *-- "0..*" ReferencesResult : references

class SyncRequestTask {
    <<abstract>>
    info? : any
}

SyncRequestTask <|-- CreateEntities
class CreateEntities {
    task           : "create"
    container      : string
    reservedToken? : Guid
    keyName?       : string
    entities       : any[]
}

SyncRequestTask <|-- UpsertEntities
class UpsertEntities {
    task       : "upsert"
    container  : string
    keyName?   : string
    entities   : any[]
}

SyncRequestTask <|-- ReadEntities
class ReadEntities {
    task        : "read"
    container   : string
    keyName?    : string
    isIntKey?   : boolean
    ids         : string[]
    references? : References[]
}
ReadEntities *-- "0..*" References : references

SyncRequestTask <|-- QueryEntities
class QueryEntities {
    task        : "query"
    container   : string
    keyName?    : string
    isIntKey?   : boolean
    filterTree? : any
    filter?     : string
    references? : References[]
    limit?      : int32
    maxCount?   : int32
    cursor?     : string
}
QueryEntities *-- "0..*" References : references

SyncRequestTask <|-- AggregateEntities
class AggregateEntities {
    task        : "aggregate"
    container   : string
    type        : AggregateType
    filterTree? : any
    filter?     : string
}
AggregateEntities *-- "1" AggregateType : type

class AggregateType:::cssEnum {
    <<enumeration>>
    count
}


SyncRequestTask <|-- PatchEntities
class PatchEntities {
    task       : "patch"
    container  : string
    keyName?   : string
    patches    : EntityPatch[]
}
PatchEntities *-- "0..*" EntityPatch : patches

class EntityPatch {
    id       : string
    patches  : JsonPatch[]
}
EntityPatch *-- "0..*" JsonPatch : patches

SyncRequestTask <|-- DeleteEntities
class DeleteEntities {
    task       : "delete"
    container  : string
    ids?       : string[]
    all?       : boolean
}

SyncRequestTask <|-- SyncMessageTask
class SyncMessageTask {
    <<abstract>>
    name           : string
    param?         : any
    targetUsers?   : string[]
    targetClients? : EventTargetClient[]
}
SyncMessageTask *-- "0..*" EventTargetClient : targetClients

SyncMessageTask <|-- SendMessage
class SendMessage {
    task           : "message"
}

SyncMessageTask <|-- SendCommand
class SendCommand {
    task           : "command"
}

SyncRequestTask <|-- CloseCursors
class CloseCursors {
    task       : "closeCursors"
    container  : string
    cursors?   : string[]
}

SyncRequestTask <|-- SubscribeChanges
class SubscribeChanges {
    task       : "subscribeChanges"
    container  : string
    changes    : Change[]
    filter?    : any
}
SubscribeChanges *-- "0..*" Change : changes

class Change:::cssEnum {
    <<enumeration>>
    create
    upsert
    patch
    delete
}


SyncRequestTask <|-- SubscribeMessage
class SubscribeMessage {
    task    : "subscribeMessage"
    name    : string
    remove? : boolean
}

SyncRequestTask <|-- ReserveKeys
class ReserveKeys {
    task       : "reserveKeys"
    container  : string
    count      : int32
}

class SyncTaskResult {
    <<abstract>>
}

SyncTaskResult <|-- CreateEntitiesResult
class CreateEntitiesResult {
    task    : "create"
    errors? : EntityError[]
}
CreateEntitiesResult *-- "0..*" EntityError : errors

SyncTaskResult <|-- UpsertEntitiesResult
class UpsertEntitiesResult {
    task    : "upsert"
    errors? : EntityError[]
}
UpsertEntitiesResult *-- "0..*" EntityError : errors

SyncTaskResult <|-- ReadEntitiesResult
class ReadEntitiesResult {
    task        : "read"
    references? : ReferencesResult[]
}
ReadEntitiesResult *-- "0..*" ReferencesResult : references

SyncTaskResult <|-- QueryEntitiesResult
class QueryEntitiesResult {
    task        : "query"
    container?  : string
    cursor?     : string
    count?      : int32
    ids         : string[]
    references? : ReferencesResult[]
}
QueryEntitiesResult *-- "0..*" ReferencesResult : references

SyncTaskResult <|-- AggregateEntitiesResult
class AggregateEntitiesResult {
    task       : "aggregate"
    container? : string
    value?     : double
}

SyncTaskResult <|-- PatchEntitiesResult
class PatchEntitiesResult {
    task    : "patch"
    errors? : EntityError[]
}
PatchEntitiesResult *-- "0..*" EntityError : errors

SyncTaskResult <|-- DeleteEntitiesResult
class DeleteEntitiesResult {
    task    : "delete"
    errors? : EntityError[]
}
DeleteEntitiesResult *-- "0..*" EntityError : errors

SyncTaskResult <|-- SyncMessageResult
class SyncMessageResult {
    <<abstract>>
}

SyncMessageResult <|-- SendMessageResult
class SendMessageResult {
    task  : "message"
}

SyncMessageResult <|-- SendCommandResult
class SendCommandResult {
    task    : "command"
    result? : any
}

SyncTaskResult <|-- CloseCursorsResult
class CloseCursorsResult {
    task   : "closeCursors"
    count  : int32
}

SyncTaskResult <|-- SubscribeChangesResult
class SubscribeChangesResult {
    task  : "subscribeChanges"
}

SyncTaskResult <|-- SubscribeMessageResult
class SubscribeMessageResult {
    task  : "subscribeMessage"
}

SyncTaskResult <|-- ReserveKeysResult
class ReserveKeysResult {
    task  : "reserveKeys"
    keys? : ReservedKeys
}
ReserveKeysResult *-- "0..1" ReservedKeys : keys

class ReservedKeys {
    start  : int64
    count  : int32
    token  : Guid
}

SyncTaskResult <|-- TaskErrorResult
class TaskErrorResult {
    task        : "error"
    type        : TaskErrorResultType
    message?    : string
    stacktrace? : string
}
TaskErrorResult *-- "1" TaskErrorResultType : type

class TaskErrorResultType:::cssEnum {
    <<enumeration>>
    None
    UnhandledException
    DatabaseError
    FilterError
    ValidationError
    CommandError
    InvalidTask
    NotImplemented
    PermissionDenied
    SyncError
}


class JsonPatch {
    <<abstract>>
}

JsonPatch <|-- PatchReplace
class PatchReplace {
    op     : "replace"
    path   : string
    value  : any
}

JsonPatch <|-- PatchAdd
class PatchAdd {
    op     : "add"
    path   : string
    value  : any
}

JsonPatch <|-- PatchRemove
class PatchRemove {
    op    : "remove"
    path  : string
}

JsonPatch <|-- PatchCopy
class PatchCopy {
    op    : "copy"
    path  : string
    from? : string
}

JsonPatch <|-- PatchMove
class PatchMove {
    op    : "move"
    path  : string
    from? : string
}

JsonPatch <|-- PatchTest
class PatchTest {
    op     : "test"
    path   : string
    value? : any
}


```
