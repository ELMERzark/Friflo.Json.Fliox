// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { int32 }      from "./Standard"
import { DateTime }   from "./Standard"
import { BigInteger } from "./Standard"
import { uint8 }      from "./Standard"
import { int16 }      from "./Standard"
import { int64 }      from "./Standard"
import { float }      from "./Standard"
import { double }     from "./Standard"

export abstract class PocEntity {
    id  : string;
}

export class Article extends PocEntity {
    name      : string;
    producer? : string | null;
}

export class Customer extends PocEntity {
    name  : string;
}

export class Employee extends PocEntity {
    firstName  : string;
    lastName?  : string | null;
}

export abstract class PocStore {
    orders       : { [key: string]: Order };
    customers    : { [key: string]: Customer };
    articles     : { [key: string]: Article };
    producers    : { [key: string]: Producer };
    employees    : { [key: string]: Employee };
    types        : { [key: string]: TestType };
    TestCommand  : (command: TestCommand) => boolean;
    Echo         : (command: any) => any;
}

export class OrderItem {
    article  : string;
    amount   : int32;
    name?    : string | null;
}

export class PocStruct {
    value  : int32;
}

export class DerivedClass extends OrderItem {
    derivedVal  : int32;
}

export class TestCommand {
    text? : string | null;
}

export class Order extends PocEntity {
    customer? : string | null;
    created   : DateTime;
    items?    : OrderItem[] | null;
}

export class Producer extends PocEntity {
    name       : string;
    employees? : string[] | null;
}

export class TestType extends PocEntity {
    dateTime          : DateTime;
    dateTimeNull?     : DateTime | null;
    bigInt            : BigInteger;
    bigIntNull?       : BigInteger | null;
    boolean           : boolean;
    booleanNull?      : boolean | null;
    uint8             : uint8;
    uint8Null?        : uint8 | null;
    int16             : int16;
    int16Null?        : int16 | null;
    int32             : int32;
    int32Null?        : int32 | null;
    int64             : int64;
    int64Null?        : int64 | null;
    float32           : float;
    float32Null?      : float | null;
    float64           : double;
    float64Null?      : double | null;
    pocStruct         : PocStruct;
    pocStructNull?    : PocStruct | null;
    intArray          : int32[];
    intArrayNull?     : int32[] | null;
    intNullArray?     : (int32 | null)[] | null;
    jsonValue?        : any | null;
    derivedClass      : DerivedClass;
    derivedClassNull? : DerivedClass | null;
}

