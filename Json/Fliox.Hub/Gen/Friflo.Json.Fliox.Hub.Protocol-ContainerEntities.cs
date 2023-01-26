// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol
{
    static class Gen_ContainerEntities
    {
        private const int Gen_container = 0;
        private const int Gen_count = 1;
        private const int Gen_entities = 2;
        private const int Gen_notFound = 3;
        private const int Gen_errors = 4;

        private static bool ReadField (ref ContainerEntities obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_container: obj.container = reader.ReadShortString (field, obj.container, out success);  return success;
                case Gen_count:     obj.count     = reader.ReadInt32Null (field, out success);  return success;
                case Gen_entities:  obj.entities  = reader.ReadClass     (field, obj.entities,  out success);  return success;
                case Gen_notFound:  obj.notFound  = reader.ReadClass     (field, obj.notFound,  out success);  return success;
                case Gen_errors:    obj.errors    = reader.ReadClass     (field, obj.errors,    out success);  return success;
            }
            return false;
        }

        private static void Write(ref ContainerEntities obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteShortString (fields[Gen_container], obj.container, ref firstMember);
            writer.WriteInt32Null (fields[Gen_count],     obj.count,     ref firstMember);
            writer.WriteClass     (fields[Gen_entities],  obj.entities,  ref firstMember);
            writer.WriteClass     (fields[Gen_notFound],  obj.notFound,  ref firstMember);
            writer.WriteClass     (fields[Gen_errors],    obj.errors,    ref firstMember);
        }
    }
}

