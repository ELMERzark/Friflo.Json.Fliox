// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
package Sync.DB.Sync

import kotlinx.serialization.*
import CustomSerializer.*
import java.util.*
import kotlinx.serialization.json.*
import Sync.Transform.*

@Serializable
// @JsonClassDiscriminator("type") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class DatabaseMessage  {
}

@Serializable
@SerialName("sub")
data class SubscriptionEvent (
    override  val seq    : Int,
    override  val target : String? = null,
    override  val client : String? = null,
              val tasks  : List<DatabaseTask>? = null,
) : DatabaseEvent()

@Serializable
abstract class DatabaseEvent {
    abstract  val seq    : Int
    abstract  val target : String?
    abstract  val client : String?
}

@Serializable
// @JsonClassDiscriminator("task") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class DatabaseTask  {
}

@Serializable
@SerialName("create")
data class CreateEntities (
              val container     : String,
              @Serializable(with = UUIDSerializer::class)
              val reservedToken : UUID? = null,
              val keyName       : String? = null,
              val entities      : List<JsonElement>,
) : DatabaseTask()

@Serializable
@SerialName("upsert")
data class UpsertEntities (
              val container : String,
              val keyName   : String? = null,
              val entities  : List<JsonElement>,
) : DatabaseTask()

@Serializable
@SerialName("read")
data class ReadEntitiesList (
              val container : String,
              val keyName   : String? = null,
              val isIntKey  : Boolean? = null,
              val reads     : List<ReadEntities>,
) : DatabaseTask()

@Serializable
data class ReadEntities (
              val ids        : List<String>,
              val references : List<References>? = null,
)

@Serializable
data class References (
              val selector   : String,
              val container  : String,
              val keyName    : String? = null,
              val isIntKey   : Boolean? = null,
              val references : List<References>? = null,
)

@Serializable
@SerialName("query")
data class QueryEntities (
              val container  : String,
              val keyName    : String? = null,
              val isIntKey   : Boolean? = null,
              val filterLinq : String? = null,
              val filter     : FilterOperation? = null,
              val references : List<References>? = null,
) : DatabaseTask()

@Serializable
@SerialName("patch")
data class PatchEntities (
              val container : String,
              val keyName   : String? = null,
              val patches   : HashMap<String, EntityPatch>,
) : DatabaseTask()

@Serializable
data class EntityPatch (
              val patches : List<JsonPatch>,
)

@Serializable
@SerialName("delete")
data class DeleteEntities (
              val container : String,
              val ids       : List<String>? = null,
              val all       : Boolean? = null,
) : DatabaseTask()

@Serializable
@SerialName("message")
data class SendMessage (
              val name  : String,
              val value : JsonElement,
) : DatabaseTask()

@Serializable
@SerialName("subscribeChanges")
data class SubscribeChanges (
              val container : String,
              val changes   : List<Change>,
              val filter    : FilterOperation? = null,
) : DatabaseTask()

enum class Change {
    create,
    upsert,
    patch,
    delete,
}

@Serializable
@SerialName("subscribeMessage")
data class SubscribeMessage (
              val name   : String,
              val remove : Boolean? = null,
) : DatabaseTask()

@Serializable
@SerialName("reserveKeys")
data class ReserveKeys (
              val container : String,
              val count     : Int,
) : DatabaseTask()

@Serializable
@SerialName("sync")
data class SyncRequest (
    override  val reqId  : Int? = null,
              val client : String? = null,
              val ack    : Int? = null,
              val token  : String? = null,
              val tasks  : List<DatabaseTask>,
) : DatabaseRequest()

@Serializable
abstract class DatabaseRequest {
    abstract  val reqId : Int?
}

@Serializable
@SerialName("syncRes")
data class SyncResponse (
    override  val reqId        : Int? = null,
              val error        : ErrorResponse? = null,
              val tasks        : List<TaskResult>? = null,
              val results      : List<ContainerEntities>? = null,
              val createErrors : HashMap<String, EntityErrors>? = null,
              val upsertErrors : HashMap<String, EntityErrors>? = null,
              val patchErrors  : HashMap<String, EntityErrors>? = null,
              val deleteErrors : HashMap<String, EntityErrors>? = null,
) : DatabaseResponse()

@Serializable
abstract class DatabaseResponse {
    abstract  val reqId : Int?
}

@Serializable
@SerialName("error")
data class ErrorResponse (
    override  val reqId   : Int? = null,
              val message : String? = null,
) : DatabaseResponse()

@Serializable
// @JsonClassDiscriminator("task") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class TaskResult  {
}

@Serializable
@SerialName("create")
data class CreateEntitiesResult (
              val Error : CommandError? = null,
) : TaskResult()

@Serializable
data class CommandError (
              val message : String? = null,
)

@Serializable
@SerialName("upsert")
data class UpsertEntitiesResult (
              val Error : CommandError? = null,
) : TaskResult()

@Serializable
@SerialName("read")
data class ReadEntitiesListResult (
              val reads : List<ReadEntitiesResult>,
) : TaskResult()

@Serializable
data class ReadEntitiesResult (
              val Error      : CommandError? = null,
              val references : List<ReferencesResult>,
)

@Serializable
data class ReferencesResult (
              val error      : String? = null,
              val container  : String? = null,
              val ids        : List<String>,
              val references : List<ReferencesResult>? = null,
)

@Serializable
@SerialName("query")
data class QueryEntitiesResult (
              val Error      : CommandError? = null,
              val container  : String? = null,
              val filterLinq : String? = null,
              val ids        : List<String>,
              val references : List<ReferencesResult>? = null,
) : TaskResult()

@Serializable
@SerialName("patch")
data class PatchEntitiesResult (
              val Error : CommandError? = null,
) : TaskResult()

@Serializable
@SerialName("delete")
data class DeleteEntitiesResult (
              val Error : CommandError? = null,
) : TaskResult()

@Serializable
@SerialName("message")
data class SendMessageResult (
              val Error  : CommandError? = null,
              val result : JsonElement? = null,
) : TaskResult()

@Serializable
@SerialName("subscribeChanges")
class SubscribeChangesResult (
) : TaskResult()

@Serializable
@SerialName("subscribeMessage")
class SubscribeMessageResult (
) : TaskResult()

@Serializable
@SerialName("reserveKeys")
data class ReserveKeysResult (
              val Error : CommandError? = null,
              val keys  : ReservedKeys? = null,
) : TaskResult()

@Serializable
data class ReservedKeys (
              val start : Long,
              val count : Int,
              @Serializable(with = UUIDSerializer::class)
              val token : UUID,
)

@Serializable
@SerialName("error")
data class TaskErrorResult (
              val type       : TaskErrorResultType,
              val message    : String? = null,
              val stacktrace : String? = null,
) : TaskResult()

enum class TaskErrorResultType {
    None,
    UnhandledException,
    DatabaseError,
    InvalidTask,
    PermissionDenied,
    SyncError,
}

@Serializable
data class ContainerEntities (
              val container : String,
              val entities  : List<JsonElement>,
              val notFound  : List<String>? = null,
              val errors    : HashMap<String, EntityError>? = null,
)

@Serializable
data class EntityError (
              val type    : EntityErrorType,
              val message : String? = null,
)

enum class EntityErrorType {
    Undefined,
    ParseError,
    ReadError,
    WriteError,
    DeleteError,
    PatchError,
}

@Serializable
data class EntityErrors (
              val container : String? = null,
              val errors    : HashMap<String, EntityError>,
)

