
# UserStore database

This folder contains an example user database used to configure user authentication and authorization by utilizing a
[UserAuthenticator.cs](../../../../Json/Fliox.Hub/DB/UserAuth/UserDatabaseAuthenticator.cs) instance.

The schema of the database is defined via the models in [UserStore.cs](../../../../Json/Fliox.Hub/DB/UserAuth/UserStore.cs).

folders (containers):

## [`roles`](./roles)

Store the roles used for task authorization. These records can be referenced by `roles` in `UserPermission`


## [`credentials`](./credentials)

Used to store data for each user to enable user authentication.


## [`permissions`](./permissions)

Store a set of `roles` for each user. If a user aspire to run a task the specified `rules` are evaluated
and if authorization is successful the task is executed.


## VSCode
To simplify manual editing of entities (files) in VSCode [UserStore - JSON Schema](../../Schema/JSON/UserStore) is used.
The JSON Schema files in this folder are generated from the models by the [Schema Generator](../../../Common/UnitTest/Fliox/Schema).

The mapping of **JSON Schema** files via VSCode is explained here:
[Mapping to a schema in the workspace](https://code.visualstudio.com/docs/languages/json#_mapping-to-a-schema-in-the-workspace)

