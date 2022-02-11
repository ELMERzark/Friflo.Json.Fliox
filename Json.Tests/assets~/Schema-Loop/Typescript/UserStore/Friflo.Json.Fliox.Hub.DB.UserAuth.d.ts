// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { DbContainers } from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbCommands }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostDetails }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostCluster }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { Right }        from "./Friflo.Json.Fliox.Hub.Host.Auth.Rights";
import { Right_Union }  from "./Friflo.Json.Fliox.Hub.Host.Auth.Rights";

export interface UserStore {
    // --- containers
    credentials  : { [key: string]: UserCredential };
    permissions  : { [key: string]: UserPermission };
    roles        : { [key: string]: Role };

    // --- commands
    ["AuthenticateUser"]     (param: AuthenticateUser) : AuthenticateUserResult;
    ["std.Echo"]             (param: any) : any;
    ["std.Containers"]       (param: any) : DbContainers;
    ["std.Commands"]         (param: any) : DbCommands;
    ["std.Schema"]           (param: any) : DbSchema;
    ["std.Stats"]            (param: any) : DbStats;
    ["std.Details"]          (param: any) : HostDetails;
    ["std.Cluster"]          (param: any) : HostCluster;
}

export class AuthenticateUser {
    userId  : string;
    token   : string;
}

export class AuthenticateUserResult {
    isValid  : boolean;
}

export class Role {
    id           : string;
    rights       : Right_Union[];
    description? : string | null;
}

export class UserCredential {
    id     : string;
    token? : string | null;
}

export class UserPermission {
    id     : string;
    roles? : string[] | null;
}

