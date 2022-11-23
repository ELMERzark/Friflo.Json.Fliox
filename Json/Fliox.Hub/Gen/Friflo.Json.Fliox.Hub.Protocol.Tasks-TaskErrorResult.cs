// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    static class Gen_TaskErrorResult
    {
        private const int Gen_type = 0;
        private const int Gen_message = 1;
        private const int Gen_stacktrace = 2;

        private static bool ReadField (ref TaskErrorResult obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_type:       obj.type       = reader.ReadEnum (field, obj.type,       out success);  return success;
                case Gen_message:    obj.message    = reader.ReadString (field, obj.message,    out success);  return success;
                case Gen_stacktrace: obj.stacktrace = reader.ReadString (field, obj.stacktrace, out success);  return success;
            }
            return false;
        }

        private static void Write(ref TaskErrorResult obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteEnum (fields[Gen_type],       obj.type,       ref firstMember);
            writer.WriteString (fields[Gen_message],    obj.message,    ref firstMember);
            writer.WriteString (fields[Gen_stacktrace], obj.stacktrace, ref firstMember);
        }
    }
}

