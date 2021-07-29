// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Flow.Sync;

#pragma warning disable 0169

namespace Flow.Auth.Rights {

[Fri.Discriminator("type")]
[Fri.Polymorph(typeof(RightAllow),            Discriminant = "allow")]
[Fri.Polymorph(typeof(RightTask),             Discriminant = "task")]
[Fri.Polymorph(typeof(RightMessage),          Discriminant = "message")]
[Fri.Polymorph(typeof(RightSubscribeMessage), Discriminant = "subscribeMessage")]
[Fri.Polymorph(typeof(RightDatabase),         Discriminant = "database")]
[Fri.Polymorph(typeof(RightPredicate),        Discriminant = "predicate")]
public  abstract class Right {
    string  description;
}

public class RightAllow : Right {
    bool  grant;
}

public class RightTask : Right {
    [Fri.Property(Required = true)]
    List<TaskType>  types;
}

public class RightMessage : Right {
    [Fri.Property(Required = true)]
    List<string>  names;
}

public class RightSubscribeMessage : Right {
    [Fri.Property(Required = true)]
    List<string>  names;
}

public class RightDatabase : Right {
    [Fri.Property(Required = true)]
    Dictionary<string, ContainerAccess>  containers;
}

public class ContainerAccess {
    List<OperationType>  operations;
    List<Change>         subscribeChanges;
}

public enum OperationType {
    create,
    update,
    delete,
    patch,
    read,
    query,
    mutate,
    full,
}

public class RightPredicate : Right {
    [Fri.Property(Required = true)]
    List<string>  names;
}

}

