// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
package UserStore.Hub.Host.Auth.Rights

import kotlinx.serialization.*
import CustomSerializer.*
import UserStore.Hub.Protocol.Tasks.*

@Serializable
// @JsonClassDiscriminator("type") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class Right  {
    abstract  val description : String?
}

@Serializable
@SerialName("allow")
data class AllowRight (
    override  val description : String? = null,
              val database    : String,
) : Right()

@Serializable
@SerialName("task")
data class TaskRight (
    override  val description : String? = null,
              val database    : String,
              val types       : List<TaskType>,
) : Right()

@Serializable
@SerialName("sendMessage")
data class SendMessageRight (
    override  val description : String? = null,
              val database    : String,
              val names       : List<String>,
) : Right()

@Serializable
@SerialName("subscribeMessage")
data class SubscribeMessageRight (
    override  val description : String? = null,
              val database    : String,
              val names       : List<String>,
) : Right()

@Serializable
@SerialName("operation")
data class OperationRight (
    override  val description : String? = null,
              val database    : String,
              val containers  : List<ContainerAccess>,
) : Right()

@Serializable
data class ContainerAccess (
              val name             : String,
              val operations       : List<OperationType>? = null,
              val subscribeChanges : List<EntityChange>? = null,
)

enum class OperationType {
    create,
    upsert,
    delete,
    deleteAll,
    patch,
    read,
    query,
    aggregate,
    mutate,
    full,
}

@Serializable
@SerialName("predicate")
data class PredicateRight (
    override  val description : String? = null,
              val names       : List<String>,
) : Right()

