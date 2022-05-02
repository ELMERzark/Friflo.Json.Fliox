// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
package UserStore.Hub.DB.UserAuth

import kotlinx.serialization.*
import CustomSerializer.*
import UserStore.Hub.Host.Auth.Rights.*

@Serializable
abstract class UserStore {
    abstract  val credentials : HashMap<String, UserCredential>
    abstract  val permissions : HashMap<String, UserPermission>
    abstract  val roles       : HashMap<String, Role>
}

@Serializable
data class UserCredential (
              val id    : String,
              val token : String? = null,
)

@Serializable
data class UserPermission (
              val id    : String,
              val roles : List<String>? = null,
)

@Serializable
data class Role (
              val id          : String,
              val rights      : List<Right>,
              val description : String? = null,
)

@Serializable
data class Credentials (
              val userId : String,
              val token  : String,
)

@Serializable
data class AuthResult (
              val isValid : Boolean,
)

