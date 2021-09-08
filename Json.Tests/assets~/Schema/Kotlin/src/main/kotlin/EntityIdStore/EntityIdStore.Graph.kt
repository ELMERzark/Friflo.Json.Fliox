// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
package EntityIdStore.Graph

import kotlinx.serialization.*
import CustomSerializer.*
import java.util.*

@Serializable
data class GuidEntity (
              @Serializable(with = UUIDSerializer::class)
              val id : UUID,
)

@Serializable
data class GuidNullEntity (
              @Serializable(with = UUIDSerializer::class)
              val id : UUID? = null,
)

@Serializable
data class IntEntity (
              val id : Int,
)

@Serializable
data class LongEntity (
              val Id : Long,
)

@Serializable
data class ShortEntity (
              val id : Short,
)

@Serializable
data class ByteEntity (
              val id : Byte,
)

@Serializable
data class CustomIdEntity (
              val customId : String,
)

@Serializable
data class EntityRefs (
              val id             : String,
              @Serializable(with = UUIDSerializer::class)
              val guidEntity     : UUID,
              @Serializable(with = UUIDSerializer::class)
              val guidNullEntity : UUID? = null,
              val intEntity      : Int,
              val longEntity     : Long,
              val shortEntity    : Short,
              val byteEntity     : Byte,
              val customIdEntity : String? = null,
              val intEntities    : List<Int>? = null,
)

@Serializable
data class CustomIdEntity2 (
              val customId2 : String,
)

