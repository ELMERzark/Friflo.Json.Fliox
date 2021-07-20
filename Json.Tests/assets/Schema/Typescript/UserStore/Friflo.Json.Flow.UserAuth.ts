﻿// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
import { Right } from "./Friflo.Json.Flow.Auth.Rights"
import { Right_Union } from "./Friflo.Json.Flow.Auth.Rights"

export class Role {
    id?:     string;
    rights: Right_Union[];
}

export class UserCredential {
    id?:       string;
    passHash?: string;
    token?:    string;
}

