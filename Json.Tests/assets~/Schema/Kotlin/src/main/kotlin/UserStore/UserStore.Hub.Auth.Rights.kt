// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
package UserStore.Hub.Auth.Rights

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
data class RightAllow (
              val database    : String? = null,
              val grant       : Boolean,
    override  val description : String? = null,
) : Right()

@Serializable
@SerialName("task")
data class RightTask (
              val database    : String? = null,
              val types       : List<TaskType>,
    override  val description : String? = null,
) : Right()

@Serializable
@SerialName("message")
data class RightMessage (
              val database    : String? = null,
              val names       : List<String>,
    override  val description : String? = null,
) : Right()

@Serializable
@SerialName("subscribeMessage")
data class RightSubscribeMessage (
              val database    : String? = null,
              val names       : List<String>,
    override  val description : String? = null,
) : Right()

@Serializable
@SerialName("database")
data class RightDatabase (
              val database    : String? = null,
              val containers  : HashMap<String, ContainerAccess>,
    override  val description : String? = null,
) : Right()

@Serializable
data class ContainerAccess (
              val operations       : List<OperationType>? = null,
              val subscribeChanges : List<Change>? = null,
)

enum class OperationType {
    create,
    upsert,
    delete,
    deleteAll,
    patch,
    read,
    query,
    mutate,
    full,
}

@Serializable
@SerialName("predicate")
data class RightPredicate (
              val names       : List<String>,
    override  val description : String? = null,
) : Right()

