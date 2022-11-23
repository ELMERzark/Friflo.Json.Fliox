// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol
{
    static class Gen_SyncEvent
    {
        private const int Gen_seq = 0;
        private const int Gen_srcUserId = 1;
        private const int Gen_isOrigin = 2;
        private const int Gen_db = 3;
        private const int Gen_tasks = 4;

        private static bool ReadField (ref SyncEvent obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_seq:       obj.seq       = reader.ReadInt32       (field, out success);  return success;
                case Gen_srcUserId: obj.srcUserId = reader.ReadJsonKey     (field, out success);  return success;
                case Gen_isOrigin:  obj.isOrigin  = reader.ReadBooleanNull (field, out success);  return success;
                case Gen_db:        obj.db        = reader.ReadString      (field, obj.db,        out success);  return success;
                case Gen_tasks:     obj.tasks     = reader.ReadClass       (field, obj.tasks,     out success);  return success;
            }
            return false;
        }

        private static void Write(ref SyncEvent obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteInt32       (fields[Gen_seq],       obj.seq,       ref firstMember);
            writer.WriteJsonKey     (fields[Gen_srcUserId], obj.srcUserId, ref firstMember);
            writer.WriteBooleanNull (fields[Gen_isOrigin],  obj.isOrigin,  ref firstMember);
            writer.WriteString      (fields[Gen_db],        obj.db,        ref firstMember);
            writer.WriteClass       (fields[Gen_tasks],     obj.tasks,     ref firstMember);
        }
    }
}

