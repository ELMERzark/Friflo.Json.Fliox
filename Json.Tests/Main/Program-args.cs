﻿#if !UNITY_2020_1_OR_NEWER

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Friflo.Json.Tests.Main
{
    internal static partial class Program
    {
        private enum Module
        {
            TestServer,
            FlioxServerAspNetCore,
            //
            MemoryDbThroughput,
            FileDbThroughput,
            WebsocketDbThroughput,
            HttpDbThroughput,
            LoopbackDbThroughput
        }
        
        // run TestServer via one of the following methods:
        //   dotnet run --project ./Json.Tests/Friflo.Json.Tests.csproj -- --module TestServer        (also compiles project)
        //   dotnet ./Json.Tests/.bin/Debug/netcoreapp3.1/Friflo.Json.Tests.dll --module TestServer   (requires Debug build)
        //   VSCode        > Run > TestServer
        //   Rider         > Run > TestServer
        //   Visual Studio > Debug > TestServer
        public static void Main(string[] args)
        {
            Console.WriteLine($"Friflo.Json.Tests - current directory: {Directory.GetCurrentDirectory()}");

            // [Creating Modern And Helpful Command Line Utilities With System.CommandLine - .NET Core Tutorials]
            // https://dotnetcoretutorials.com/2021/01/16/creating-modern-and-helpful-command-line-utilities-with-system-commandline/
            var moduleOpt = new Option<Module>("--module",  "the module inside Friflo.Json.Tests") {IsRequired = true};

            var rootCommand = new RootCommand {
                moduleOpt,
                new Option<string>("--endpoint", () => "http://+:8010/",                    "endpoint the server listen at")
            };
            rootCommand.Description = "small tests within Friflo.Json.Tests";

            rootCommand.Handler = CommandHandler.Create<Module, string>(async (module, endpoint) =>
            {
                Console.WriteLine($"module: {module}");
                switch (module) {
                    case Module.TestServer:
                        TestServer(endpoint);
                        break;
                    case Module.FlioxServerAspNetCore:
                        FlioxServerAspNetCore(endpoint);
                        break;
                    case Module.MemoryDbThroughput:
                        await Throughput.MemoryDbThroughput();
                        break;
                    case Module.FileDbThroughput:
                        await Throughput.FileDbThroughput();
                        break;
                    case Module.WebsocketDbThroughput:
                        await Throughput.WebsocketDbThroughput();
                        break;
                    case Module.HttpDbThroughput:
                        await Throughput.HttpDbThroughput();
                        break;
                    case Module.LoopbackDbThroughput:
                        await Throughput.LoopbackDbThroughput();
                        break;
                }
            });
            rootCommand.Invoke(args);
        }
    }
}

#endif