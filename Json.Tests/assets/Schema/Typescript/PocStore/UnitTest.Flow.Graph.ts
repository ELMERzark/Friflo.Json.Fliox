// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
import { DateTime }   from "./System"
import { BigInteger } from "./System.Numerics"

export class Order {
    id        : string;
    customer? : string | null;
    created   : DateTime;
    items?    : OrderItem[] | null;
}

export class OrderItem {
    article? : string | null;
    amount   : number;
    name?    : string | null;
}

export class Customer {
    id    : string;
    name? : string | null;
}

export class Article {
    id        : string;
    name?     : string | null;
    producer? : string | null;
}

export class Producer {
    id         : string;
    name?      : string | null;
    employees? : string[] | null;
}

export class Employee {
    id         : string;
    firstName? : string | null;
    lastName?  : string | null;
}

export class TestType {
    id             : string;
    dateTime       : DateTime;
    dateTimeNull?  : DateTime | null;
    bigInt         : BigInteger;
    bigIntNull?    : BigInteger | null;
    uint8          : number;
    uint8Null?     : number | null;
    int16          : number;
    int16Null?     : number | null;
    int32          : number;
    int32Null?     : number | null;
    int64          : number;
    int64Null?     : number | null;
    pocStruct      : PocStruct;
    pocStructNull? : PocStruct | null;
    intArray       : number[];
    intArrayNull?  : number[] | null;
    jsonValue      : {} | null;
}

export class PocStruct {
    val  : number;
}

