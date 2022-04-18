```mermaid
classDiagram
direction LR

class PocStore:::cssSchema {
    <<Schema>>
    <<abstract>>
    orders     : [id] ➞ Order
    customers  : [id] ➞ Customer
    articles   : [id] ➞ Article
    producers  : [id] ➞ Producer
    employees  : [id] ➞ Employee
    types      : [id] ➞ TestType
}
PocStore *-- "0..*" Order : orders
PocStore *-- "0..*" Customer : customers
PocStore *-- "0..*" Article : articles
PocStore *-- "0..*" Producer : producers
PocStore *-- "0..*" Employee : employees
PocStore *-- "0..*" TestType : types

class Order:::cssEntity {
    <<Entity · id>>
    id        : string
    customer? : string
    created   : DateTime
    items?    : OrderItem[]
}
Order o.. "0..1" Customer : customer
Order *-- "0..*" OrderItem : items

class Customer:::cssEntity {
    <<Entity · id>>
    id    : string
    name  : string
}

class Article:::cssEntity {
    <<Entity · id>>
    id        : string
    name      : string
    producer? : string
}
Article o.. "0..1" Producer : producer

class Producer:::cssEntity {
    <<Entity · id>>
    id         : string
    name       : string
    employees? : string[]
}
Producer o.. "0..*" Employee : employees

class Employee:::cssEntity {
    <<Entity · id>>
    id         : string
    firstName  : string
    lastName?  : string
}

class PocEntity {
    <<abstract>>
    id  : string
}

PocEntity <|-- TestType
class TestType:::cssEntity {
    <<Entity · id>>
    dateTime          : DateTime
    dateTimeNull?     : DateTime
    bigInt            : BigInteger
    bigIntNull?       : BigInteger
    boolean           : boolean
    booleanNull?      : boolean
    uint8             : uint8
    uint8Null?        : uint8
    int16             : int16
    int16Null?        : int16
    int32             : int32
    int32Null?        : int32
    int64             : int64
    int64Null?        : int64
    float32           : float
    float32Null?      : float
    float64           : double
    float64Null?      : double
    pocStruct         : PocStruct
    pocStructNull?    : PocStruct
    intArray          : int32[]
    intArrayNull?     : int32[]
    intNullArray?     : (int32 | null)[]
    jsonValue?        : any
    derivedClass      : DerivedClass
    derivedClassNull? : DerivedClass
}
TestType *-- "1" PocStruct : pocStruct
TestType *-- "0..1" PocStruct : pocStructNull
TestType *-- "1" DerivedClass : derivedClass
TestType *-- "0..1" DerivedClass : derivedClassNull

class OrderItem {
    article  : string
    amount   : int32
    name?    : string
}
OrderItem o.. "1" Article : article

class PocStruct {
    value  : int32
}

OrderItem <|-- DerivedClass
class DerivedClass {
    derivedVal  : int32
}


```
