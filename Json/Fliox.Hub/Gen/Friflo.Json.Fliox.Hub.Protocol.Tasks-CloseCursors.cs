// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    static class Gen_CloseCursors
    {
        private const int Gen_info = 0;
        private const int Gen_container = 1;
        private const int Gen_cursors = 2;

        private static bool ReadField (ref CloseCursors obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_info:      obj.info      = reader.ReadJsonValue (field, out success);  return success;
                case Gen_container: obj.container = reader.ReadShortString (field, obj.container, out success);  return success;
                case Gen_cursors:   obj.cursors   = reader.ReadClass     (field, obj.cursors,   out success);  return success;
            }
            return false;
        }

        private static void Write(ref CloseCursors obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteJsonValue (fields[Gen_info],      obj.info,      ref firstMember);
            writer.WriteShortString (fields[Gen_container], obj.container, ref firstMember);
            writer.WriteClass     (fields[Gen_cursors],   obj.cursors,   ref firstMember);
        }
    }
}

