// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
package Friflo.Json.Fliox.Hub.Protocol.Tasks

import kotlinx.serialization.*
import CustomSerializer.*

enum class EntityChange {
    create,
    upsert,
    merge,
    delete,
}

