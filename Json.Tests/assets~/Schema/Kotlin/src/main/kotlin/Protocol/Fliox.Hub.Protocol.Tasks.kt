// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
package Fliox.Hub.Protocol.Tasks

import kotlinx.serialization.*
import CustomSerializer.*
import kotlinx.serialization.json.*
import java.util.*
import Fliox.Hub.Protocol.Models.*
import Fliox.Transform.*

@Serializable
// @JsonClassDiscriminator("task") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class SyncRequestTask  {
    abstract  val info : JsonElement?
}

@Serializable
@SerialName("create")
data class CreateEntities (
    override  val info          : JsonElement? = null,
              val container     : String,
              @Serializable(with = UUIDSerializer::class)
              val reservedToken : UUID? = null,
              val keyName       : String? = null,
              val entities      : List<JsonElement>,
) : SyncRequestTask()

@Serializable
@SerialName("upsert")
data class UpsertEntities (
    override  val info      : JsonElement? = null,
              val container : String,
              val keyName   : String? = null,
              val entities  : List<JsonElement>,
) : SyncRequestTask()

@Serializable
@SerialName("read")
data class ReadEntities (
    override  val info       : JsonElement? = null,
              val container  : String,
              val keyName    : String? = null,
              val isIntKey   : Boolean? = null,
              val ids        : List<String>,
              val references : List<References>? = null,
) : SyncRequestTask()

@Serializable
@SerialName("query")
data class QueryEntities (
    override  val info       : JsonElement? = null,
              val container  : String,
              val keyName    : String? = null,
              val isIntKey   : Boolean? = null,
              val filterTree : JsonElement? = null,
              val filter     : String? = null,
              val references : List<References>? = null,
              val limit      : Int? = null,
              val maxCount   : Int? = null,
              val cursor     : String? = null,
) : SyncRequestTask()

@Serializable
@SerialName("aggregate")
data class AggregateEntities (
    override  val info       : JsonElement? = null,
              val container  : String,
              val type       : AggregateType,
              val filterTree : JsonElement? = null,
              val filter     : String? = null,
) : SyncRequestTask()

enum class AggregateType {
    count,
}

@Serializable
@SerialName("patch")
data class PatchEntities (
    override  val info      : JsonElement? = null,
              val container : String,
              val keyName   : String? = null,
              val patches   : List<EntityPatch>,
) : SyncRequestTask()

@Serializable
data class EntityPatch (
              val id      : String,
              val patches : List<JsonPatch>,
)

@Serializable
@SerialName("merge")
data class MergeEntities (
    override  val info      : JsonElement? = null,
              val container : String,
              val keyName   : String? = null,
              val patches   : List<JsonElement>,
) : SyncRequestTask()

@Serializable
@SerialName("delete")
data class DeleteEntities (
    override  val info      : JsonElement? = null,
              val container : String,
              val ids       : List<String>? = null,
              val all       : Boolean? = null,
) : SyncRequestTask()

@Serializable
@SerialName("message")
data class SendMessage (
    override  val info    : JsonElement? = null,
    override  val name    : String,
    override  val param   : JsonElement? = null,
    override  val users   : List<String>? = null,
    override  val clients : List<String>? = null,
    override  val groups  : List<String>? = null,
) : SyncMessageTask()

@Serializable
abstract class SyncMessageTask {
    abstract  val info    : JsonElement?
    abstract  val name    : String
    abstract  val param   : JsonElement?
    abstract  val users   : List<String>?
    abstract  val clients : List<String>?
    abstract  val groups  : List<String>?
}

@Serializable
@SerialName("command")
data class SendCommand (
    override  val info    : JsonElement? = null,
    override  val name    : String,
    override  val param   : JsonElement? = null,
    override  val users   : List<String>? = null,
    override  val clients : List<String>? = null,
    override  val groups  : List<String>? = null,
) : SyncMessageTask()

@Serializable
@SerialName("closeCursors")
data class CloseCursors (
    override  val info      : JsonElement? = null,
              val container : String,
              val cursors   : List<String>? = null,
) : SyncRequestTask()

@Serializable
@SerialName("subscribeChanges")
data class SubscribeChanges (
    override  val info      : JsonElement? = null,
              val container : String,
              val changes   : List<EntityChange>,
              val filter    : String? = null,
) : SyncRequestTask()

enum class EntityChange {
    create,
    upsert,
    patch,
    delete,
}

@Serializable
@SerialName("subscribeMessage")
data class SubscribeMessage (
    override  val info   : JsonElement? = null,
              val name   : String,
              val remove : Boolean? = null,
) : SyncRequestTask()

@Serializable
@SerialName("reserveKeys")
data class ReserveKeys (
    override  val info      : JsonElement? = null,
              val container : String,
              val count     : Int,
) : SyncRequestTask()

@Serializable
// @JsonClassDiscriminator("task") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class SyncTaskResult  {
}

@Serializable
@SerialName("create")
data class CreateEntitiesResult (
              val errors : List<EntityError>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("upsert")
data class UpsertEntitiesResult (
              val errors : List<EntityError>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("read")
data class ReadEntitiesResult (
              val references : List<ReferencesResult>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("query")
data class QueryEntitiesResult (
              val container  : String? = null,
              val cursor     : String? = null,
              val count      : Int? = null,
              val ids        : List<String>,
              val references : List<ReferencesResult>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("aggregate")
data class AggregateEntitiesResult (
              val container : String? = null,
              val value     : Double? = null,
) : SyncTaskResult()

@Serializable
@SerialName("patch")
data class PatchEntitiesResult (
              val errors : List<EntityError>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("merge")
data class MergeEntitiesResult (
              val errors : List<EntityError>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("delete")
data class DeleteEntitiesResult (
              val errors : List<EntityError>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("message")
class SendMessageResult (
) : SyncMessageResult()

@Serializable
abstract class SyncMessageResult {
}

@Serializable
@SerialName("command")
data class SendCommandResult (
              val result : JsonElement? = null,
) : SyncMessageResult()

@Serializable
@SerialName("closeCursors")
data class CloseCursorsResult (
              val count : Int,
) : SyncTaskResult()

@Serializable
@SerialName("subscribeChanges")
class SubscribeChangesResult (
) : SyncTaskResult()

@Serializable
@SerialName("subscribeMessage")
class SubscribeMessageResult (
) : SyncTaskResult()

@Serializable
@SerialName("reserveKeys")
data class ReserveKeysResult (
              val keys : ReservedKeys? = null,
) : SyncTaskResult()

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
) : SyncTaskResult()

enum class TaskErrorResultType {
    None,
    UnhandledException,
    DatabaseError,
    FilterError,
    ValidationError,
    CommandError,
    InvalidTask,
    NotImplemented,
    PermissionDenied,
    SyncError,
}

