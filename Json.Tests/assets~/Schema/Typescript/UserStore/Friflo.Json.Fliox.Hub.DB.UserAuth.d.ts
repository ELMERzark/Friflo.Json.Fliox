// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { DbContainers } from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbMessages }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostInfo }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostCluster }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { UserOptions }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { UserResult }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
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
    /** authenticate user **Credentials**: **userId** and **token** */
    ["AuthenticateUser"]     (param: Credentials | null) : AuthResult;
    ["ValidateUserDb"]       () : ValidateUserDbResult;
    ["ClearAuthCache"]       () : boolean;
    /** echos the given parameter to assure the database is working appropriately. */
    ["std.Echo"]             (param: any) : any;
    /** list all database containers */
    ["std.Containers"]       () : DbContainers;
    /** list all database commands and messages */
    ["std.Messages"]         () : DbMessages;
    /** return the Schema assigned to the database */
    ["std.Schema"]           () : DbSchema;
    /** return the number of entities of all containers (or the given container) of the database */
    ["std.Stats"]            (param: string | null) : DbStats;
    /** returns general information about the Hub like version, host, project and environment name */
    ["std.Host"]             () : HostInfo;
    /** list all databases and their containers hosted by the Hub */
    ["std.Cluster"]          () : HostCluster;
    /** return the groups of the current user. Optionally change the groups of the current user */
    ["std.User"]             (param: UserOptions | null) : UserResult;
}

/** contains a **token** assigned to a user used for authentication */
export class UserCredential {
    /** user name */
    id     : string;
    /** user token */
    token? : string | null;
}

/** Set of **roles** assigned to a user used for authorization */
export class UserPermission {
    /** user name */
    id     : string;
    /** set of **roles** assigned to a user */
    roles? : string[] | null;
}

/** Contains a set of **rights** used for task authorization */
export class Role {
    /** **Role** name */
    id           : string;
    /** a set of **rights** used for task authorization */
    rights       : Right_Union[];
    /** optional **description** explaining a **Role** */
    description? : string | null;
}

/** user **Credentials** used for authentication */
export class Credentials {
    userId  : string;
    token   : string;
}

/** Result of **AuthenticateUser()** command */
export class AuthResult {
    /** true if authentication was successful */
    isValid  : boolean;
}

export class ValidateUserDbResult {
    errors? : string[] | null;
}

