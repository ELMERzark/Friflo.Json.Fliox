// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol
{
    static class Gen_SyncRequest
    {
        private const int Gen_reqId = 0;
        private const int Gen_clientId = 1;
        private const int Gen_userId = 2;
        private const int Gen_token = 3;
        private const int Gen_eventAck = 4;
        private const int Gen_tasks = 5;
        private const int Gen_database = 6;
        private const int Gen_info = 7;

        private static bool ReadField (ref SyncRequest obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_reqId:    obj.reqId    = reader.ReadInt32Null (field, out success);  return success;
                case Gen_clientId: obj.clientId = reader.ReadShortString (field, obj.clientId, out success);  return success;
                case Gen_userId:   obj.userId   = reader.ReadShortString (field, obj.userId,   out success);  return success;
                case Gen_token:    obj.token    = reader.ReadString    (field, obj.token,    out success);  return success;
                case Gen_eventAck: obj.eventAck = reader.ReadInt32Null (field, out success);  return success;
                case Gen_tasks:    obj.tasks    = reader.ReadClass     (field, obj.tasks,    out success);  return success;
                case Gen_database: obj.database = reader.ReadShortString (field, obj.database, out success);  return success;
                case Gen_info:     obj.info     = reader.ReadJsonValue (field, out success);  return success;
            }
            return false;
        }

        private static void Write(ref SyncRequest obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteInt32Null (fields[Gen_reqId],    obj.reqId,    ref firstMember);
            writer.WriteShortString (fields[Gen_clientId], obj.clientId, ref firstMember);
            writer.WriteShortString (fields[Gen_userId],   obj.userId,   ref firstMember);
            writer.WriteString    (fields[Gen_token],    obj.token,    ref firstMember);
            writer.WriteInt32Null (fields[Gen_eventAck], obj.eventAck, ref firstMember);
            writer.WriteClass     (fields[Gen_tasks],    obj.tasks,    ref firstMember);
            writer.WriteShortString (fields[Gen_database], obj.database, ref firstMember);
            writer.WriteJsonValue (fields[Gen_info],     obj.info,     ref firstMember);
        }
    }
}

