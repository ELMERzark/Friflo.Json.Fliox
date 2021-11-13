import { Right_Union } from "../UserStore/Friflo.Json.Fliox.Hub.Auth.Rights"
import { Role } from "../UserStore/Friflo.Json.Fliox.Hub.UserAuth"

// check assignment with using a type compiles successful
var exampleRole: Role = {
    id: "some-id",
    rights: [
        {
            type:           "allow",
            description:    "allow description"
        },
        {
            type:           "operation",
            containers:     { "Article": { operations:["read", "query", "upsert"], subscribeChanges: ["upsert"] }}
        },
        {
            type:           "message",
            names:          ["test-mess*"]
        },
        {
            type:           "subscribeMessage",
            names:          ["test-sub*"]
        },
        { 
            type:           "predicate",
            names:          ["TestPredicate"]
        },
        {
            type:           "task",
            types:          ["read"]
        }
    ]
}

// check using a Discriminated Union compiles successful
function usePolymorphType (right: Right_Union) {
    switch (right.type) {
        case "allow":
            break;
        case "message":
            var names: string[] = right.names;
            break;
        case "predicate":
            var names: string[] = right.names;
            break;
    }
}

export function testUserStore() {
}

