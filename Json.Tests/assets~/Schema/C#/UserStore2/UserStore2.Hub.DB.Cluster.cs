// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace UserStore2.Hub.DB.Cluster {

public class DbContainers {
    [Fri.Required]
    string        id;
    [Fri.Required]
    string        storage;
    [Fri.Required]
    List<string>  containers;
}

public class DbMessages {
    [Fri.Required]
    string        id;
    [Fri.Required]
    List<string>  commands;
    [Fri.Required]
    List<string>  messages;
}

public class DbSchema {
    [Fri.Required]
    string                         id;
    [Fri.Required]
    string                         schemaName;
    [Fri.Required]
    string                         schemaPath;
    [Fri.Required]
    Dictionary<string, JsonValue>  jsonSchemas;
}

public class DbStats {
    List<ContainerStats>  containers;
}

public class ContainerStats {
    [Fri.Required]
    string  name;
    long    count;
}

public class HostDetails {
    [Fri.Required]
    string        version;
    string        hostName;
    string        projectName;
    string        projectWebsite;
    string        envName;
    string        envColor;
    [Fri.Required]
    List<string>  routes;
}

public class HostCluster {
    [Fri.Required]
    List<DbContainers>  databases;
}

}

