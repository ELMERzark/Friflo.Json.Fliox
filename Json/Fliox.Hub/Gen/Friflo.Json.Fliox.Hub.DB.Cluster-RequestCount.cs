// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.DB.Cluster;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.DB.Cluster
{
    static class Gen_RequestCount
    {
        private const int Gen_db = 0;
        private const int Gen_requests = 1;
        private const int Gen_tasks = 2;

        private static bool ReadField (ref RequestCount obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_db:       obj.db       = reader.ReadString (field, obj.db,       out success);  return success;
                case Gen_requests: obj.requests = reader.ReadInt32 (field, out success);  return success;
                case Gen_tasks:    obj.tasks    = reader.ReadInt32 (field, out success);  return success;
            }
            return false;
        }

        private static void Write(ref RequestCount obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteString (fields[Gen_db],       obj.db,       ref firstMember);
            writer.WriteInt32 (fields[Gen_requests], obj.requests, ref firstMember);
            writer.WriteInt32 (fields[Gen_tasks],    obj.tasks,    ref firstMember);
        }
    }
}

