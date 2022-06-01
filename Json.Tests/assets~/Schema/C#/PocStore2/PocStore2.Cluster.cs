// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using System.Collections.Generic;
using Friflo.Json.Fliox;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace PocStore2.Cluster {

public class DbContainers {
    [RequiredField]
    string        id;
    [RequiredField]
    string        storage;
    [RequiredField]
    List<string>  containers;
}

public class DbMessages {
    [RequiredField]
    string        id;
    [RequiredField]
    List<string>  commands;
    [RequiredField]
    List<string>  messages;
}

public class DbSchema {
    [RequiredField]
    string                         id;
    [RequiredField]
    string                         schemaName;
    [RequiredField]
    string                         schemaPath;
    [RequiredField]
    Dictionary<string, JsonValue>  jsonSchemas;
}

public class DbStats {
    List<ContainerStats>  containers;
}

public class ContainerStats {
    [RequiredField]
    string  name;
    long    count;
}

public class HostDetails {
    [RequiredField]
    string        version;
    string        hostName;
    string        projectName;
    string        projectWebsite;
    string        envName;
    string        envColor;
    [RequiredField]
    List<string>  routes;
}

public class HostCluster {
    [RequiredField]
    List<DbContainers>  databases;
}

}

