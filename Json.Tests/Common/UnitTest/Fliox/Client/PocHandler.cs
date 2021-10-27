// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class PocHandler : TaskHandler {
        public PocHandler() {
            AddCommandHandler<TestCommand, bool>(TestCommand); // todo add handler via scanning DatabaseHandler
        }
        
        private static bool TestCommand(Command<TestCommand> command) {
            return true;
        }
    }
}