// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { Right }       from "./Friflo.Json.Fliox.DB.Auth.Rights"
import { Right_Union } from "./Friflo.Json.Fliox.DB.Auth.Rights"

export abstract class UserStore {
    permissions  : { [key: string]: UserPermission };
    credentials  : { [key: string]: UserCredential };
    roles        : { [key: string]: Role };
}

export class UserStoreService {
    AuthenticateUser : (command: AuthenticateUser) => AuthenticateUserResult;
    Echo             : (command: any) => any;
}

export class UserPermission {
    id     : string;
    roles? : string[] | null;
}

export class UserCredential {
    id        : string;
    passHash? : string | null;
    token?    : string | null;
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

