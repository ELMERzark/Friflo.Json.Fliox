// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { DbContainers } from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbMessages }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostDetails }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostCluster }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { Right }        from "./Friflo.Json.Fliox.Hub.Host.Auth.Rights";
import { Right_Union }  from "./Friflo.Json.Fliox.Hub.Host.Auth.Rights";

/**
 * Control individual user access to database containers and commands.   
 * Each **user** has a set of **roles** stored in container **permissions**.   
 * Each **role** in container **roles** has a set of **rights** which grant or deny container access or command execution.
 */
// schema documentation only - not implemented right now
export interface UserStore {
    // --- containers
    credentials  : { [key: string]: UserCredential };
    permissions  : { [key: string]: UserPermission };
    roles        : { [key: string]: Role };

    // --- commands
    ["AuthenticateUser"]     (param: AuthenticateUser | null) : AuthenticateUserResult;
    /** echos the given parameter to assure the database is working appropriately. */
    ["std.Echo"]             (param: any) : any;
    /** list all containers of the database */
    ["std.Containers"]       () : DbContainers;
    /** list all commands exposed by the database */
    ["std.Messages"]         () : DbMessages;
    /** return the JSON Schema assigned to the database */
    ["std.Schema"]           () : DbSchema;
    /** return the number of entities of all containers (or the given container) of the database */
    ["std.Stats"]            (param: string | null) : DbStats;
    /** returns descriptive information about the Hub like version, host, project and environment name */
    ["std.Details"]          () : HostDetails;
    /** list all databases and their containers hosted by the Hub */
    ["std.Cluster"]          () : HostCluster;
}

export class UserCredential {
    id     : string;
    token? : string | null;
}

export class UserPermission {
    id     : string;
    roles? : string[] | null;
}

export class Role {
    id           : string;
    rights       : Right_Union[];
    description? : string | null;
}

export class AuthenticateUser {
    userId  : string;
    token   : string;
}

export class AuthenticateUserResult {
    isValid  : boolean;
}

