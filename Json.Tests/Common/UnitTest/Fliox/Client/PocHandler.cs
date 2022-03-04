// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using static NUnit.Framework.Assert;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class PocHandler : TaskHandler {
        private readonly TestCommandsHandler    test    = new TestCommandsHandler();
        private readonly TestCommandsHandler2   test2   = new TestCommandsHandler2();
        private readonly EmptyCommandsHandler   empty   = new EmptyCommandsHandler();
        
        public PocHandler() {
            // add all command handlers of the passed handler classes
            AddMessageHandlers(this,    "");
            AddMessageHandlers(test,    "test.");
            AddMessageHandlers(empty,   "empty.");
            
            // add command handlers individually
            AddCommandHandler      <string,string>("SyncCommand",  TestCommandsHandler2.SyncCommand);
            AddCommandHandlerAsync <string,string>("AsyncCommand", TestCommandsHandler2.AsyncCommand);
        }
        
        private static bool TestCommand(Param<TestCommand> param, MessageContext command) {
            AreEqual("TestCommand", command.Name);
            AreEqual("TestCommand", command.ToString());
            command.WriteNull = true; // ensure API available
            return true;
        }
    }
    
    /// <summary>
    /// Uses to show adding all its command handlers by <see cref="TaskHandler.AddMessageHandlers{TClass}"/>
    /// </summary>
    public class TestCommandsHandler {
        private string testMessageValue;
            
        private void TestMessage(Param<string> param, MessageContext command) {
            param.Get(out testMessageValue, out _);
        }
        
        private static string Command1(Param<string> param, MessageContext command) {
            return "hello Command1";
        }
        
        private string Command2(Param<string> param, MessageContext command) {
            return testMessageValue;
        }
    }
    
    /// <summary>
    /// Uses to show adding its command handlers individually by <see cref="TaskHandler.AddCommandHandler{TParam,TResult}"/>
    /// or <see cref="TaskHandler.AddCommandHandlerAsync{TParam,TResult}"/>
    /// </summary>
    public class TestCommandsHandler2 {
        public static string SyncCommand(Param<string> param, MessageContext command) {
            return "hello SyncCommand";
        }
        
        public static Task<string> AsyncCommand(Param<string> param, MessageContext command) {
            return Task.FromResult("hello AsyncCommand");
        }
    }
    
    public class EmptyCommandsHandler { }
}