// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
package UserStore.UserAuth

import kotlinx.serialization.*
import CustomSerializer.*
import UserStore.Graph.*
import UserStore.Auth.Rights.*

@Serializable
data class UserPermission (
    override  val id    : String,
              val roles : List<String>? = null,
) : Entity()

@Serializable
data class UserCredential (
    override  val id       : String,
              val passHash : String? = null,
              val token    : String? = null,
) : Entity()

@Serializable
data class Role (
    override  val id          : String,
              val rights      : List<Right>,
              val description : String? = null,
) : Entity()

