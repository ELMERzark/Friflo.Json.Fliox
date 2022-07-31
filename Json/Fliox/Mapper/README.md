

# [![JSON Fliox](../../../docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)     **Fliox Mapper** ![SPLASH](../../../docs/images/paint-splatter.svg)

[![.NET Tests](https://github.com/friflo/Friflo.Json.Fliox/workflows/.NET/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions)


A JSON parser/serializer and object mapper trimmed towards performance.  
The implementation strives towards maximizing CPU utilization and minimizing memory footprint.  
An **unique feature** is to enable the JSON parsing / serializing in 
[Unity Burst Jobs](https://unity.com/dots/packages#burst-compiler).  
As of now no other JSON library provide this possibility.  
This supports running the parser/serializer within a separate thread of a bursted Job while leaving
CPU/memory resources to the main thread being the critical path in game loops.


## Content
- [Features](#features)
    - [JSON Parser/Serializer](#json-parserserializer)
    - [Object Mapper Reader/Writer](#object-mapper-readerwriter)
    - [General Features](#general-features)

- [Examples](#examples)

- [Performance](#performance)




# **Features**

# JSON Parser/Serializer

namespace **`Friflo.Json.Burst`**

- **Clear/Compact API** `Iterator API` for parser - `Appender API` for serializer.

- **Skipping** of JSON object members and elements (array elements and values on root)  
    Provide statistics (counts) about skipped JSON entries:
    arrays, objects, strings, integers, numbers, booleans and nulls

- **Don't throw exceptions** in `Release` build in any case - e.g. of invalid JSON. Provide a concept to return gracefully in application code.

- Throw exceptions in `Debug` build to notice applications errors when using the library.

- **No heap allocation** in case of invalid JSON when creating an error message.

- Support parsing/serializing of JSON objects, arrays and values (string, number, boolean and null) on **root level**.

- Optimization principles

    - Minimize **memory footprint**

        - **No (0) allocations** after a few iterations by using a few internal byte & int buffers

        - Support **reusing** parser & serializer instances to avoid allocations on the heap

    - Minimize **CPU load**

        - Using **only struct's**, no classes (a requirement of Unity/Burst) enabling high memory locality to reduce page misses.  
            As a result the complete parser/serializer state lives on the stack.

        - Pass method parameters of struct's - a value type in .NET - **always by `ref`**.

        - **No string copy or memcpy**

- Compatible to [**Unity Burst Jobs**](https://docs.unity3d.com/Packages/com.unity.burst@1.5/manual/docs/QuickStart.html)
  which requires using a
  [subset of C#/.NET language](https://docs.unity3d.com/Packages/com.unity.burst@1.5/manual/docs/CSharpLanguageSupport_Types.html)
  in the parser implementation.  

  In short this is the absence of using managed objects in any way.
  This exclude the usage of managed types like classes, strings, arrays or exceptions.  
  To support this subset the library need to be compiled with `JSON_BURST`.  
  The default / CLR implementation is a little less restrict: arrays (`byte` & `int`) are used.

- Used .NET API namespaces:
  `System`, `System.Text` .Encoding.UTF8 &
  `System.Globalization` .CultureInfo, .NumberFormatInfo, .NumberStyles



# Object Mapper Reader/Writer

namespace **`Friflo.Json.Fliox.Mapper`**

- Support deserialization in two ways:

    - **Create new object instances** and deserialize by using `Read()` to them which is the common practice of
        many object mapper implementations.

    - **Deserialize to passed object** instances by using `ReadTo()` while reusing also their child objects referenced by fields,
        arrays and `List`'s. Right now `Dictionary` (maps) entries are not reused.  
        This avoids object allocation on the heap for the given instance and all its child objects

- **Support polymorphism**: Currently by a discriminator name `$type` as the first member: e.g. `{ "$type": "Tiger", ... }`

- `ObjectReader` support **two error handling modes** while parsing and deserialization (unmarshalling) -
    e.g. JSON validation errors.  
    By avoiding exceptions performance increases by the fact that throwing exceptions is an expensive operation
    because of object creation the heap. The error mode is set via `ObjectReader.ThrowException`:  

    1. **Don't throw exception** and provide the error state via a boolean and a message.

    2. **Throw exception** in error case - which is useful for debugging.

- Error messages are created **without heap allocation** to avoid vulnerability to DDoS attacks simply by flooding a service
  with invalid JSON.

- Optimized for performance by maximizing CPU utilization and low memory footprint

    - **Dynamically create IL code** ensuring no reflection code is used for mapping to and from data structures.
      Doing so enables JSON processing without heap allocations.

    - Also support **mapping via reflection** by configuration if IL code generation is not wanted or enabled.
      Mapping data structures via reflection is inherent slower as is requires heap allocation caused by
      boxing & unboxing.

    - **Reusing** of `ObjectReader` & `ObjectWriter` instance to avoid unnecessary allocations on the heap

    - **No heap allocations** are performed when using `ReadTo()` and using a subset of supported types:
        arrays, `Lists` and classes ensured by [unit test](../../../Json.Tests/Common/UnitTest/Fliox/Mapper/TestNoAllocation.cs)

- Supported C#/.NET types:

    - Container types: arrays, `List`, `IList`, `Dictionary` & `IDictionary`

    - Primitive types, `Nullable`', enums, `BigInteger` & `DateTime`

    - Support for adding custom types as shown at [CustomTypeMapper](../../../Json.Tests/Common/Examples/Mapper/CustomTypeMapper.cs)

- Uses internally the JSON parser mentioned above

- Used .NET API namespaces additional to Burst: `System.Collections`, `System.Collections.Generic` & `System.Reflection`



# General Features

- **UTF-8** support

- Compatible to **.NET Standard**.
    That is: .NET Core, .NET 5, .NET Framework, Mono, Xamarin (iOS, Mac, Android), UWP, Unity

- **CLS compliant API**. Meaning the API of the **Friflo.Json.Fliox** library is compatible to all languages targeting .NET. These are:
  C#, C++/CLI, Eiffel, F#, IronPython, IronRuby, PowerBuilder, Visual Basic, Visual COBOL, and Windows PowerShell. See more at:
  [Common Language Specification](https://docs.microsoft.com/en-us/dotnet/standard/common-type-system#common-language-specification)

- **No global mutable** state like `static` variables to avoid side effects. A typical candidate would by the `TypeStore` class.
  Avoiding this ensures an application to control its live time and guarantees that unit tests are free from side effect.
  `TypeStore` is thread safe and could be used as a `static` among multiple threads in an application if wanted.

- **Fail safe** in case of JSON and application errors

- **Ensuring a maximum depth** (`maxDepth`) of nested JSON objects and arrays. E.g. a JSON like `[[[...]]]`.
  The default `maxDepth` is set 100.  
  A limit of 3000 (Windows 10) is possible without getting a stack overflow.
  The reason for the limit is that both `ObjectReader` & `ObjectWriter` are using a recursive implementation.
  The low level parser itself  can be used without any limit as it is an iterator used by `JsonParser.NextEvent()`.

    - Reading JSON exceeding `maxDepth` with `JsonParser` or `ObjectReader` will result in an JSON error.
      This avoid application issues in case of a DDoS attack doing this intentionally.

    - When writing JSON the `JsonSerializer` and `ObjectWriter` ensures this constrain via a runtime exception
      to avoid accidentally raising this limit.

- **No dependencies** to 3rd party libraries. The used .NET API namespaces are mentioned above.  
  A Unity specific dependency is required when compiling within Unity with **UNITY_BURST** which is
  [Unity Collections](https://docs.unity3d.com/Packages/com.unity.collections@0.14/manual/index.html)
  to enable using `NativeArray`, `NativeList`, `FixedString32` & `FixedString128`

- Small libraries: `Friflo.Json.Burst.dll` ~ **45 kb**,  `Friflo.Json.Fliox.dll` ~ **150 kb**

- **Expressive error messages** when parsing invalid JSON. E.g.  
    ```
    JsonParser/JSON error: unexpected character > expect key. Found: v path: 'map.key1' at position: 23
    ```


# **Examples**

The unit test also contain a folder explaining single file (self contained) examples illustrating usage and
anti patterns how to use (and how not to use) the `JsonParser` and `JsonSerializer`.

The examples can be found at [Json.Tests/Common/Examples/](../../../Json.Tests/Common/Examples)

## **Parser & Serializer**

A minimal *Hello world* example showing how to parse a given JSON string via the `JsonParser`

```csharp
        public void HelloWorldParser() {
            string say = "", to = "";
            var p = new JsonParser();
            p.InitParser(new Bytes (@"{""say"": ""Hello"", ""to"": ""World 🌎""}"));
            p.ExpectRootObject(out JObj i);
            while (i.NextObjectMember(ref p)) {
                if (i.UseMemberStr (ref p, "say"))  { say = p.value.ToString(); }
                if (i.UseMemberStr (ref p, "to"))   { to =  p.value.ToString(); }
            }
            Console.WriteLine($"Output: {say}, {to}");
            // Output: Hello, World 🌎
        }
```

A minimal *Hello world* using the serializer to create JSON via the `JsonSerializer`

```csharp
        public void HelloWorldSerializer() {
            var s = new JsonSerializer();
            s.InitSerializer();
            s.ObjectStart();
                s.MemberStr ("say", "Hello");
                s.MemberStr ("to",  "World 🌎");
            s.ObjectEnd();
            Console.WriteLine($"Output: {s.json}");
            // Output: {"say":"Hello","to":"World 🌎"}
        }
```

## **Object Mapper - Reader & Writer**

An Object Mapper maps a class to a JSON string and vise vera. Given the following class:

```csharp
        class Message {
            public string say;
            public string to;
        }
```

Use `ObjectMapper` to deserialize / unmarshal a JSON string to a class instance.

```csharp
        public void HelloWorldReader() {
            var m = new ObjectMapper();
            var msg = m.Read<Message>(@"{""say"": ""Hello 👋"", ""to"": ""World""}");
            Console.WriteLine($"Output: {msg.say}, {msg.to}");
            // Output: Hello 👋, World
        }
```

Use `ObjectMapper` to serialize / marshal a class instance to a JSON string.

```csharp
        public void HelloWorldWriter() {
            var m = new ObjectMapper();
            var json = m.Write(new Message {say = "Hello 👋", to = "World"});
            Console.WriteLine($"Output: {json}");
            // Output: {"say":"Hello 👋","to":"World"}
        }
```

# **Performance**

The performance tests are included in the unit tests. They can be executed within the CLR (Common Language Runtime)
and within Unity.

# Performance .NET CLR

The test cases contain also JSON parser performance tests.
Various JSON examples files are parsed by iteration them from begin to end.
The parser returns the JSON tree structure via an iterator. The keys and the JSON
values (strings, numbers, booleans are nulls) are ready to be consumed at this stage.

To reduce side effects in measurement by NUnit of throughput increase `impliedThroughput`
at [TestParserPerformance.cs](../../../Json.Tests/Common/UnitTest/Burst/TestParserPerformance.cs)

On the used development system (Intel Core i7-4790k 4Ghz, Windows 10) the throughput of the example JSON files
within the CLR are at **200-550 MB/sec**. All tests are measured on one core.

# Performance Unity

- With **JSON_BURST** in Unity Editor  
Running the performance inside the Unity Editor in `Edit Mode` or in the `Test Runner` show weak performance numbers.
The reason is using `native container`'s within the Editor are a bottleneck. Throughput: **6-13 MB/sec**.
Imho - this is an acceptable development scenario.

- Without **JSON_BURST** in Unity Editor  
It is faster than *'with JSON_BURST in Unity Editor'* because in this scenario managed container are used instead
of `native container`s. Throughput: **25-88 MB/sec**.  
*Note*: In this mode the parser & serializer cannot be used in Burst Jobs.

- With **JSON_BURST** in a Unity Build  
When building a game as a binary for deployment the numbers are okay. There is mainly no difference between
the `Scripting Backend` `Mono 2x` and `IL2CPP` which can be used for builds. Throughput: **56-116 MB/sec**
