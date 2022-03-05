// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { int64 } from "./Standard";

export class JsonSchema {
    $ref?        : string | null;
    definitions? : { [key: string]: JsonType } | null;
}

export class JsonType {
    extends?              : TypeRef | null;
    discriminator?        : string | null;
    oneOf?                : FieldType[] | null;
    isAbstract?           : boolean | null;
    type?                 : string | null;
    key?                  : string | null;
    properties?           : { [key: string]: FieldType } | null;
    commands?             : { [key: string]: MessageType } | null;
    messages?             : { [key: string]: MessageType } | null;
    isStruct?             : boolean | null;
    required?             : string[] | null;
    additionalProperties  : boolean;
    enum?                 : string[] | null;
    description?          : string | null;
}

export class TypeRef {
    $ref  : string;
    type? : string | null;
}

export class FieldType {
    type?                 : any | null;
    enum?                 : string[] | null;
    items?                : FieldType | null;
    oneOf?                : FieldType[] | null;
    minimum?              : int64 | null;
    maximum?              : int64 | null;
    pattern?              : string | null;
    format?               : string | null;
    $ref?                 : string | null;
    additionalProperties? : FieldType | null;
    isAutoIncrement?      : boolean | null;
    relation?             : string | null;
    description?          : string | null;
}

export class MessageType {
    param?       : FieldType | null;
    result?      : FieldType | null;
    description? : string | null;
}

