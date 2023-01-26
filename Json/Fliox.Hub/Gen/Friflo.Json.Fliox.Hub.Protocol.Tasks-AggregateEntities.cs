// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    static class Gen_AggregateEntities
    {
        private const int Gen_info = 0;
        private const int Gen_container = 1;
        private const int Gen_type = 2;
        private const int Gen_filterTree = 3;
        private const int Gen_filter = 4;

        private static bool ReadField (ref AggregateEntities obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_info:       obj.info       = reader.ReadJsonValue (field, out success);  return success;
                case Gen_container:  obj.container  = reader.ReadShortString (field, obj.container,  out success);  return success;
                case Gen_type:       obj.type       = reader.ReadEnum      (field, obj.type,       out success);  return success;
                case Gen_filterTree: obj.filterTree = reader.ReadJsonValue (field, out success);  return success;
                case Gen_filter:     obj.filter     = reader.ReadString    (field, obj.filter,     out success);  return success;
            }
            return false;
        }

        private static void Write(ref AggregateEntities obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteJsonValue (fields[Gen_info],       obj.info,       ref firstMember);
            writer.WriteShortString (fields[Gen_container],  obj.container,  ref firstMember);
            writer.WriteEnum      (fields[Gen_type],       obj.type,       ref firstMember);
            writer.WriteJsonValue (fields[Gen_filterTree], obj.filterTree, ref firstMember);
            writer.WriteString    (fields[Gen_filter],     obj.filter,     ref firstMember);
        }
    }
}

