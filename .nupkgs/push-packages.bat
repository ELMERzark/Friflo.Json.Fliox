
rem dotnet pack -p:PackageVersion=0.2.0 /p:Version=0.2.0 /p:FileVersion=0.2.0 /p:AssemblyVersion=0.2.0 --output .nupkgs -c Release
rem cd .nupkgs
rem push-packages.bat

dotnet nuget push Friflo.Json.Burst.0.2.0.nupkg                 --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.0.2.0.nupkg                 --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Annotation.0.2.0.nupkg      --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.0.2.0.nupkg             --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.AspNetCore.0.2.0.nupkg  --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.Cosmos.0.2.0.nupkg      --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.Explorer.0.2.0.nupkg    --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
dotnet nuget push Friflo.Json.Fliox.Hub.GraphQL.0.2.0.nupkg     --source https://api.nuget.org/v3/index.json -k %PUSH_JSON_FLIOX%
