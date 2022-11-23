// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    static class Gen_ReserveKeys
    {
        private const int Gen_info = 0;
        private const int Gen_container = 1;
        private const int Gen_count = 2;

        private static bool ReadField (ref ReserveKeys obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_info:      obj.info      = reader.ReadJsonValue (field, out success);  return success;
                case Gen_container: obj.container = reader.ReadString    (field, obj.container, out success);  return success;
                case Gen_count:     obj.count     = reader.ReadInt32     (field, out success);  return success;
            }
            return false;
        }

        private static void Write(ref ReserveKeys obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteJsonValue (fields[Gen_info],      obj.info,      ref firstMember);
            writer.WriteString    (fields[Gen_container], obj.container, ref firstMember);
            writer.WriteInt32     (fields[Gen_count],     obj.count,     ref firstMember);
        }
    }
}

