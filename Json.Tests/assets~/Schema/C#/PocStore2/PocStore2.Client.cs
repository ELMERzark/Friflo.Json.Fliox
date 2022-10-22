// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;
using System;
using System.Numerics;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace PocStore2.Client {

public abstract class PocStore {
    [Required]
    Dictionary<string, Order>     orders;
    [Required]
    Dictionary<string, Customer>  customers;
    [Required]
    Dictionary<string, Article>   articles;
    [Required]
    Dictionary<string, Article>   articles2;
    [Required]
    Dictionary<string, Producer>  producers;
    [Required]
    Dictionary<string, Employee>  employees;
    [Required]
    Dictionary<string, TestType>  types;
}

public class Order {
    [Required]
    string           id;
    string           customer;
    DateTime         created;
    List<OrderItem>  items;
}

public class Customer {
    [Required]
    string  id;
    [Required]
    string  name;
}

public class Article {
    [Required]
    string  id;
    [Required]
    string  name;
    string  producer;
}

public class Producer {
    [Required]
    string        id;
    [Required]
    string        name;
    List<string>  employees;
}

public class Employee {
    [Required]
    string  id;
    [Required]
    string  firstName;
    string  lastName;
}

public class TestType : PocEntity {
    DateTime      dateTime;
    DateTime?     dateTimeNull;
    BigInteger    bigInt;
    BigInteger?   bigIntNull;
    bool          boolean;
    bool?         booleanNull;
    byte          uint8;
    byte?         uint8Null;
    short         int16;
    short?        int16Null;
    int           int32;
    int?          int32Null;
    long          int64;
    long?         int64Null;
    float         float32;
    float?        float32Null;
    double        float64;
    double?       float64Null;
    PocStruct     pocStruct;
    PocStruct?    pocStructNull;
    [Required]
    List<int>     intArray;
    List<int>     intArrayNull;
    List<int?>    intNullArray;
    JsonValue?    jsonValue;
    [Required]
    DerivedClass  derivedClass;
    DerivedClass  derivedClassNull;
}

public class OrderItem {
    [Required]
    string  article;
    int     amount;
    string  name;
}

public abstract class PocEntity {
    [Required]
    string  id;
}

public struct PocStruct {
    int  value;
}

public class DerivedClass : OrderItem {
    int  derivedVal;
}

public class TestCommand {
    string  text;
}

}

