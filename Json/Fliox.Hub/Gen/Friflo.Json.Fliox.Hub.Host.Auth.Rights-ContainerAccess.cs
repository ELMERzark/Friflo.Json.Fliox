// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Host.Auth.Rights
{
    static class Gen_ContainerAccess
    {
        private const int Gen_name = 0;
        private const int Gen_operations = 1;
        private const int Gen_subscribeChanges = 2;

        private static bool ReadField (ref ContainerAccess obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_name:             obj.name             = reader.ReadString (field, obj.name,             out success);  return success;
                case Gen_operations:       obj.operations       = reader.ReadClass (field, obj.operations,       out success);  return success;
                case Gen_subscribeChanges: obj.subscribeChanges = reader.ReadClass (field, obj.subscribeChanges, out success);  return success;
            }
            return false;
        }

        private static void Write(ref ContainerAccess obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteString (fields[Gen_name],             obj.name,             ref firstMember);
            writer.WriteClass (fields[Gen_operations],       obj.operations,       ref firstMember);
            writer.WriteClass (fields[Gen_subscribeChanges], obj.subscribeChanges, ref firstMember);
        }
    }
}

