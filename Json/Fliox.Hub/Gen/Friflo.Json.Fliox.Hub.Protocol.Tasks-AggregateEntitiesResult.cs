// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    static class Gen_AggregateEntitiesResult
    {
        private const int Gen_container = 0;
        private const int Gen_value = 1;

        private static bool ReadField (ref AggregateEntitiesResult obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_container: obj.container = reader.ReadShortString (field, obj.container, out success);  return success;
                case Gen_value:     obj.value     = reader.ReadDoubleNull (field, out success);  return success;
            }
            return false;
        }

        private static void Write(ref AggregateEntitiesResult obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteShortString (fields[Gen_container], obj.container, ref firstMember);
            writer.WriteDoubleNull (fields[Gen_value],     obj.value,     ref firstMember);
        }
    }
}

