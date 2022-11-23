// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    static class Gen_PredicateRight
    {
        private const int Gen_description = 0;
        private const int Gen_names = 1;

        private static bool ReadField (ref PredicateRight obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_description: obj.description = reader.ReadString (field, obj.description, out success);  return success;
                case Gen_names:       obj.names       = reader.ReadClass (field, obj.names,       out success);  return success;
            }
            return false;
        }

        private static void Write(ref PredicateRight obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteString (fields[Gen_description], obj.description, ref firstMember);
            writer.WriteClass (fields[Gen_names],       obj.names,       ref firstMember);
        }
    }
}

