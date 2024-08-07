// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;
using UserStore2.Hub.Host.Auth.Rights;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace UserStore2.Hub.DB.UserAuth {

public abstract class UserStore {
    [Required]
    Dictionary<string, UserCredential>  credentials;
    [Required]
    Dictionary<string, UserPermission>  permissions;
    [Required]
    Dictionary<string, Role>            roles;
    [Required]
    Dictionary<string, UserTarget>      targets;
}

public class UserCredential {
    [Required]
    string  id;
    string  token;
}

public class UserPermission {
    [Required]
    string        id;
    [Required]
    List<string>  roles;
}

public class Role {
    [Required]
    string           id;
    [Required]
    List<TaskRight>  taskRights;
    HubRights        hubRights;
    string           description;
}

public class UserTarget {
    [Required]
    string        id;
    [Required]
    List<string>  groups;
}

public class Credentials {
    [Required]
    string  userId;
    [Required]
    string  token;
}

public class AuthResult {
    bool  isValid;
}

public class ValidateUserDbResult {
    List<string>  errors;
}

}

