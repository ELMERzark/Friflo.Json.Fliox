// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
import { Entity }      from "./Friflo.Json.Flow.Graph"
import { Right }       from "./Friflo.Json.Flow.Auth.Rights"
import { Right_Union } from "./Friflo.Json.Flow.Auth.Rights"

export abstract class UserStore {
    permissions  : { [key: string]: UserPermission };
    credentials  : { [key: string]: UserCredential };
    roles        : { [key: string]: Role };
}

export class Role extends Entity {
    rights       : Right_Union[];
    description? : string | null;
}

export class UserCredential extends Entity {
    passHash? : string | null;
    token?    : string | null;
}

export class UserPermission extends Entity {
    roles? : string[] | null;
}

