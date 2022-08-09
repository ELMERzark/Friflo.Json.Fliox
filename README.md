

# [![JSON Fliox](docs/images/Json-Fliox.svg)](https://github.com/friflo/Friflo.Json.Fliox)    **JSON Fliox** ![SPLASH](docs/images/paint-splatter.svg)

[![nuget](https://img.shields.io/nuget/v/Friflo.Json.Fliox.Hub.svg?color=blue)](https://www.nuget.org/packages/Friflo.Json.Fliox.Hub) 
[![CI](https://github.com/friflo/Friflo.Json.Fliox/workflows/CI/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions/workflows/dotnet.yml) 
[![CD](https://github.com/friflo/Friflo.Json.Fliox/workflows/CD/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions/workflows/nuget.yml) 


**.NET** library supporting **simple** and **performant** access to **NoSQL** databases via C# or Web clients.  
Its **ORM** enables **Schema** creation. Its **Hub** serve hosted databases using these schemas via HTTP.

The **ORM** client - Object Relational Mapper - is used to access NoSQL databases via .NET.  
The **Hub** is a service hosting a set of NoSQL databases via an **ASP.NET Core** server.

As Fliox is an [ORM](https://en.wikipedia.org/wiki/Object-relational_mapping) it has similarities to projects like
[Entity Framework Core](https://en.wikipedia.org/wiki/Entity_Framework),
[Ruby on Rails](https://en.wikipedia.org/wiki/Ruby_on_Rails),
[Django](https://en.wikipedia.org/wiki/Django_(web_framework)) or
[Hibernate](https://de.wikipedia.org/wiki/Hibernate_(Framework)).  
Fliox sets its focus on **NoSQL** databases.
This improves performance and bypass the [object–relational impedance mismatch](https://en.wikipedia.org/wiki/Object%E2%80%93relational_impedance_mismatch).

**TL;DR**

Try the example Hub online running on AWS - [**DemoHub**](http://ec2-174-129-178-18.compute-1.amazonaws.com/) (EC2 instance: t2-micro, us-east-1)  
The **DemoHub** .NET project is available at
[**🚀 friflo/Fliox.Examples**](https://github.com/friflo/Fliox.Examples/blob/main/README.md#-content).

<br/>

| Some numbers                                                              |                                                                                                       |
| ------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| **RTT** Request / Response roundtrip                                      | **0.3 ms**                                                                                            |
| **Pub-Sub delay** send message ➞ subscriber event                        | **sub millisecond latency**                                                                           |
| **Throughput** Request/Response WebSocket, 4 concurrent clients, 4 cores  | **27k requests / sec**                                                                                |
| Hub integration in ASP.NET Core                                           | **4 LOC** [Startup.cs](https://github.com/friflo/Fliox.Examples/blob/main/Demo/Hub/StartupAsp6.cs#L38)|
| Full functional Client & Server: REST, CRUD, Queries, Pub-Sub & Explorer  | **70 LOC** [Client](https://github.com/friflo/Fliox.Examples/blob/main/Todo/Client/TodoClient.cs) & [Server](https://github.com/friflo/Fliox.Examples/blob/main/Todo/Hub/Program.cs) |


*Note*: JSON Fliox is **not** a UI library. It is designed for simple integration in .NET and Web UI frameworks.

Published project on GitHub 2022-08

<br/>


## 🚩 Content

- [Features](#-features)
- [Examples](#-examples)           ❯  [🚀 friflo/Fliox.Examples](https://github.com/friflo/Fliox.Examples/blob/main/README.md#-content)
- [Hub](#-hub)
    - [Client](#client)                 ❯  [README.md](Json/Fliox.Hub/Client/README.md)
    - [Host](#host)                   ❯  [README.md](Json/Fliox.Hub/Host/README.md)
    - [Explorer](#explorer)             ❯  [README.md](Json/Fliox.Hub.Explorer/README.md)
    - [DB](#db)                      ❯  [README.md](Json/Fliox.Hub/DB/README.md)
    - [Protocol](#protocol)             ❯  [README.md](Json/Fliox.Hub/Protocol/README.md)
- [Fliox](#-fliox)
    - [Schema](#schema)              ❯  [README.md](Json/Fliox/Schema/README.md)
    - [Mapper](#mapper)              ❯  [README.md](Json/Fliox/Mapper/README.md)
- [Project](#-project)
    - [Build](#build)                  ❯  [README.md](Json.Tests/README.md)
    - [API](#api)                     ❯  [friflo/fliox-docs](https://github.com/friflo/fliox-docs)
    - [Properties](#properties)
    - [Principles](#principles)
- [Motivation](#-motivation)
- [Credits](#-credits)


<br/>

## 🎨 Features

Compact list of features supported by Clients and Hubs
- ASP.NET Core & HttpListener integration
    - REST API - JSON Schema / OpenAPI
    - GraphQL API
    - Batch API - HTTP & WebSocket
- CRUD
- Queries - LINQ expressions
- Container relations (associations)
- Database Schema
- Code generation
    - C#, Typescript & Kotlin
    - JSON Schema / OpenAPI
    - GraphQL Schema
    - Database Schema diagram
- JSON Validation - Records & DTO's
- Send Messages & Commands using DTO's
- Pub-Sub
- Hub Explorer
- Monitoring
- Authentication / Authorization

The features are explained within the topics (= namespaces) below.  
*Topics*: Client, Host, Hub Explorer, DB - support databases, Protocol, Schema & Mapper.

<br/>


## 🚀 **Examples**
📄   [friflo/Fliox.Examples](https://github.com/friflo/Fliox.Examples/blob/main/README.md#-content)

A separate git repository containing two **ready to run** examples showcasing the usage of Fliox Clients and Servers.  
Build and run a server with [**Gitpod**](https://github.com/friflo/Fliox.Examples/blob/main/README.md#-build) in the browser without installing anything.

[<img src="docs/images/server-log.png" width="647" height="191">](https://github.com/friflo/Fliox.Examples/blob/main/README.md#-content)  
*screenshot: DemoHub server logs*
<br/><br/>


## 📦 **Hub**

Namespace    Friflo.Json.Fliox.Hub.*  
Assembly     Friflo.Json.Fliox.Hub.dll

### **Client**
📄   [README.md](Json/Fliox.Hub/Client/README.md)

Fliox clients are strongly typed C# classes used to access NoSQL databases.  
They are implemented by creating a class e.g. `MyClient` extending `FlioxClient`.  
The database containers are represented as properties in the derived class `MyClient`.  

These classes also acts as a database schemas. They can be assigned to databases hosted on the Hub.  
Doing this enables features like:
- JSON validation of entities aka records
- generate a JSON Schema, an OpenAPI Schema and a GraphQL Schema
- generate a HTML Schema documentation and a UML class diagram
- generate classes for various programming languages: C#, Typescript & Kotlin

The `MyClient` can be used to declare custom database commands using DTO's as input and result types.


### **Host**
📄   [README.md](Json/Fliox.Hub/Host/README.md)

A `HttpHost` instance is used to host multiple NoSQL databases.  
It is designed to be integrated into HTTP servers like **ASP.NET Core**.  
This enables access to hosted databases via HTTP or WebSocket supporting the following Web API's:
- REST
- GraphQL
- Batch API

A `FlioxHub` instance is used to configure the hosted databases, authentication / authorization and Pub-Sub.  
This `FlioxHub` instance need to be passed to the constructor of the `HttpHost`

### **Explorer**
📄   [README.md](Json/Fliox.Hub.Explorer/README.md)  
Assembly     Friflo.Json.Fliox.Hub.Explorer.dll

The Hub Explorer is an admin page used to access
databases, containers and entities hosted by a Fliox Hub.  
The Explorer also enables to execute application specific database commands.

[<img src="docs/images/Fliox-Hub-Explorer.png" width="717" height="278">](Json/Fliox.Hub.Explorer/README.md)  
*screenshot: Hub Explorer*

### **DB**
📄   [README.md](Json/Fliox.Hub/DB/README.md)

Provide a set of support databases used to:
- serve the Hub configuration - used by the Hub Explorer. Schema:
  [ClusterStore](Json.Tests/assets~/Schema/Markdown/ClusterStore/class-diagram.md)
- serve monitoring data. Schema:
  [MonitorStore](Json.Tests/assets~/Schema/Markdown/MonitorStore/class-diagram.md)
- perform user authentication, authorization and management. Schema:
  [UserStore](Json.Tests/assets~/Schema/Markdown/UserStore/class-diagram.md)

### **Protocol**
📄   [README.md](Json/Fliox.Hub/Protocol/README.md)

The Protocol is the communication interface between a `FlioxClient` and a `FlioxHub`.  
Web clients can use this Protocol to access a Hub using the Batch API via HTTP & JSON.  
A language specific API - e.g. written in Typescript, Kotlin, ... - is not a requirement.

The Protocol is not intended to be used by C# .NET clients directly.  
Instead they are using a `FlioxClient` that is optimized to transform API calls into the Protocol.

<br/><br/>



## 📦 **Fliox**

Namespace    Friflo.Json.Fliox.*  
Assembly     Friflo.Json.Fliox.dll


### **Schema**
📄   [README.md](Json/Fliox/Schema/README.md)

This module enables transforming schemas expressed by a set of C# classes into
other programming languages and schema formats like:

- C#, Typescript, Kotlin
- HTML documentation, Schema Class Diagram
- JSON Schema, OpenAPI Schema, GraphQL Schema

Its main purpose is to generate schemas and types for various languages of classes extending `FlioxClient`.  
The screenshots below show Hub pages utilizing the schemas mentioned above.

[<img src="docs/images/MonitorStore-schema.png" width="739" height="226">](Json/Fliox/Schema/README.md#class-diagram)  
*screenshot: MonitorStore schema as class diagram*


[<img src="docs/images/schema-screenshots.png" width="770" height="85">](Json/Fliox/Schema/README.md#html-documentation)  
*screenshots: Schema documentation, Swagger UI & GraphiQL*



### **Mapper**
📄   [README.md](Json/Fliox/Mapper/README.md)

This module enables serialization / deserialization of C# .NET objects to / from JSON.  
Its feature set and API is similar to the .NET packages:
- [JamesNK/Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
- [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json)

The module is utilized by the assembly `Friflo.Json.Fliox.Hub` to serialize entities and DTO's.  
Its also used for serialization of the supported protocols: REST, GraphQL and Batch API.

<br/><br/>



## 🔧 **Project**

### **Build**
📄   [README.md](Json.Tests/README.md)

The project **Json.Tests** contains a console application and unit tests.  
Build and run instructions for .NET and Unity are in the README file.

**unit tests**  
Code coverage: **86%** measured with **JetBrains • docCover**

```yaml
Passed! - Failed:   0, Passed:   6, Skipped:   0, Total:   6, Duration: 2 s -  .../DemoTest.dll
Passed! - Failed:   0, Passed:   7, Skipped:   0, Total:   7, Duration: 1 s -  .../TodoTest.dll
Passed! - Failed:   0, Passed: 347, Skipped:   0, Total: 347, Duration: 15 s - .../Friflo.Json.Tests.dll
```
*summarized logs of test execution - they are executed in*  
[![CI](https://github.com/friflo/Friflo.Json.Fliox/workflows/CI/badge.svg)](https://github.com/friflo/Friflo.Json.Fliox/actions/workflows/dotnet.yml) 


### **API**

The Fliox **C# .NET** API is [CLS-compliant](https://docs.microsoft.com/en-us/dotnet/api/system.clscompliantattribute#remarks)

The API is available at [**fliox-docs API Reference**](https://github.com/friflo/fliox-docs)


### **Properties**

The goal of the library, its components and API is to be easy digestible for software developers.  
The properties describe the characteristics of this project - at least what it aims for.  
These properties are targeted to user of the library. In contrast to the principles - which are strict rules - it is a positive
way describing the design of the software architecture.  
They fit mostly the aspects described in [CUPID-for joyful coding](https://dannorth.net/2022/02/10/cupid-for-joyful-coding/).

Topics of the CUPID properties focused by this project
- Composable
    - Seamless integration into existing ASP.NET Core applications with a handful lines of code
    - Ensure independence from other parts of existing applications
    - The API surface is as small as possible
    - Has no dependencies to other libraries - except the GraphQL assembly
- Predictable
    - Naming of classes, methods and properties are compact, short and easy to pronounce
    - Class names typically are a concatenation of two short words to be expressive and to avoid name collisions
    - Avoid using long, cryptic or scientific words in the API
    - Observable
        - Monitoring is integral part of the Hub
        - The `ToString()` methods of fundamental classes show only relevant state to avoid noise in debugging sessions
        - Error and runtime assertion messages are short and expressive
- Domain based
    - Enable implementing compact applications which are easy to read and to maintain


### **Principles**

- dependencies
    - no 3rd party dependencies
    - small size of Fliox assemblies (*.dll) ~ 850 kb in total, 350 kb zipped  
      source code: library 47k LOC, unit tests: 18k LOC
- target for optimal performance
    - maximize throughput, minimize latency, minimize heap allocations and boxing
    - enable task batching as a unit-of-work
    - support bulk operations for CRUD commands
- compact and strongly typed API
    - type safe access to entities and their keys when dealing with containers  
    - type safe access to DTO's when dealing with database commands
    - absence of using `object` as a type
    - absence of utility classes & methods to
        - to use the API in an explicit manner
        - to avoid confusion implementing the same feature in multiple ways
- serialization of entities and messages - request, response & event - are entirely JSON
- Fliox Clients and Hubs are unit testable without mocking
- the **Zero** principles
    - 0 compiler errors and warnings
    - 0 ReSharper errors, warnings, suggestions and hints
    - 0 unit test errors, no flaky tests
    - 0 typos - observed by spell checker
    - no synchronous calls to API's dealing with **IO** like network or disc    
      Instead using `async` / `await`
    - no 3rd party dependencies
    - no heap allocations if possible
    - no noise in `.ToString()` methods while debugging - only relevant state.  
      E.g. instances of `FlioxClient`, `EntitySet<,>`, `FlioxHub` and `EntityDatabase`
    - no surprise of API behavior.  
      See [Principle of least astonishment](https://en.wikipedia.org/wiki/Principle_of_least_astonishment)
    - no automatic C# Code formatting - as no Code Formatter supports the code style of this project.  
      That concerns tabular indentation of fields, properties, variables and switch cases.
- extensibility
    - support custom database adapters aka providers
    - support custom code / schema generators for new programming languages
- compatibility
    - **.NET Core 3.1** and higher
    - **Unity 2020.1** and higher 

<br/>

## 🔥 Motivation

The main driver of this project is the development of an competitive online multiplayer game -
a still unresolved task in my todo list.  
The foundation to achieve this is commonly a module called *Netcode* in online multiplayer games.  
The key aspects of *Netcode* are: Synchronizing game state, messaging, low latency, high throughput,
minimal use of system resources, reliability & easy to use API.  
As Unity is selected as the Game engine C# .NET is the way to go.

Another objective is to create an open source software project which may have the potential to be popular.  
As I have 15+ years experience as a software developer in enterprise environment - Shout-Out to [HERE Technologies](https://www.here.com/) -
I decided to avoid a Vendor Lock-In to Unity and target for a solution which fits also the needs of common .NET projects.  
So development is entirely done with .NET Core while checking Unity compatibility on a regular basis.

The result is a project with a feature set useful in common & gaming projects and targeting for optimal performance.  
The common ground of both areas is the need of databases.  
In context of game development the game state (Players, NPC, objects, ...) is represented as an in-memory database
to enable low latency, high throughput and minimal use of system resources.  
In common projects databases are used to store any kind of data persistent by using a popular DBMS.  
Specific for online gaming is the ability to send messages from one client to another in *real time*.
This is enabled by supporting Pub-Sub with sub millisecond latency on *localhost*.

<br/>


## 🙏 Credits
|                                                                           |             |                                                                      |
| ------------------------------------------------------------------------- | ----------- | -------------------------------------------------------------------- |
| [NUnit](https://nunit.org/)                                               | C#          | unit testing of the library in the CLR and Unity                     |
| [FluentAssertions](https://github.com/fluentassertions/fluentassertions)  | C#          | unit testing of the library                                          |
| [GraphQL.NET Parser](https://github.com/graphql-dotnet/parser)            | C#          | used by package: Friflo.Json.Fliox.Hub.GraphQL                       |
| [MdDocs](https://github.com/ap0llo/mddocs)                                | C#          | for [fliox-docs API Reference](https://github.com/friflo/fliox-docs) |
| [.NET platform](https://dotnet.microsoft.com/en-us/)                      | C# .NET     | the platform providing compiler, runtime, IDE's & ASP.NET Core       |
| [Swagger](https://swagger.io/)                                            | static JS   | a REST / OpenAPI UI linked by the Hub Explorer                       |
| [GraphiQL](https://github.com/graphql/graphiql)                           | static JS   | a GraphQL UI linked by the Hub Explorer                              |
| [Mermaid](https://github.com/mermaid-js/mermaid)                          | static JS   | class diagram for database schema linked by the Hub Explorer         |
| [Monaco Editor](https://github.com/microsoft/monaco-editor)               | static JS   | used as JSON editor integrated in the Hub Explorer                   |
| [WinMerge](https://github.com/WinMerge/winmerge)                          | Application | heavily used in this project                                         |
| [Inscape](https://gitlab.com/inkscape/inkscape)                           | Application | to create SVG's for this project                                     |

<br/>

💖 *Like this project?*  
*Leave a* ⭐ at  [friflo/Friflo.Json.Fliox](https://github.com/friflo/Friflo.Json.Fliox)

Happy coding!  

<br/>

## License

This project is licensed under AGPLv3.  
Published project on GitHub 2022-08  

friflo JSON Fliox  
Copyright © 2022   Ullrich Praetz
