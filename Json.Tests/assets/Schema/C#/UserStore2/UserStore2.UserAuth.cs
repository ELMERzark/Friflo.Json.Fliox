// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using UserStore2.Graph;
using UserStore2.Auth.Rights;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace UserStore2.UserAuth {

public class UserPermission : Entity {
    List<string>  roles;
}

public class UserCredential : Entity {
    string  passHash;
    string  token;
}

public class Role : Entity {
    [Fri.Required]
    List<Right>  rights;
    string       description;
}

}

