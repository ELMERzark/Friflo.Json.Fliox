// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Linq;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable ConvertToConstant.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test]
        public static void TestEventTargets() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new MemoryDatabase("test", new PocHandler()))
            using (var hub              = new FlioxHub(database))
            using (var client1          = new PocStore(hub))
            using (var client2          = new PocStore(hub))
            using (hub.EventDispatcher  = new EventDispatcher(false)) // dispatch events synchronous to simplify test
            {
                AssertEventTargets(client1, client2);
            }
        }
        
        /// <summary>
        /// all events targeting only <paramref name="client1"/> either by userId or clientId. <br/>
        /// <paramref name="client2"/> is used to verify no events are received
        /// </summary>
        private static void AssertEventTargets(PocStore client1, PocStore client2) {
            // --- client1 receive all events
            var clientId1   = "client-1";
            var userId1     = "user-1";
            var client1Key  = new JsonKey(clientId1);   // test interface using a JsonKey
            var user1Key    = new JsonKey(userId1);     // test interface using a JsonKey
            client1.ClientId  = clientId1;
            client1.UserId    = userId1;
            client1.SubscribeMessage("*", (message, context) => { });
            client1.SubscriptionEventHandler += context => {
                AreEqual("user-1", context.SrcUserId.ToString());
                var expect = new [] { "msg-1", "msg-2", "msg-3", "msg-4", "msg-5", "msg-6", "msg-7", "msg-8", "Command1" };
                var actual = context.Messages.Select(msg => msg.Name);
                AreEqual(expect, actual);
            };
            
            // --- client2 receive no events
            client2.ClientId  = "client-2";
            client2.SubscribeMessage("*", (message, context) => {
                Fail("unexpected events");
            });

            // --- single target
            IsTask(client1.SendMessage("msg-1").EventTargetUser(userId1));
            IsTask(client1.SendMessage("msg-2").EventTargetUser(user1Key));
            
            IsTask(client1.SendMessage("msg-3").EventTargetClient(clientId1));
            IsTask(client1.SendMessage("msg-4").EventTargetClient(client1Key));

            // --- multi target
            IsTask(client1.SendMessage("msg-5").EventTargetUsers (new[] { userId1 }));
            IsTask(client1.SendMessage("msg-6").EventTargetUsers (new[] { user1Key }));

            IsTask(client1.SendMessage("msg-7").EventTargetClients (new[] { clientId1 }));
            IsTask(client1.SendMessage("msg-8").EventTargetClients (new[] { client1Key }));
            
            // var eventTargets = new EventTargets();
            // store.SendMessage("msg-5" ).EventTargets = eventTargets;
            
            client1.Command1().EventTargetUser(userId1);
        //  todo store.CommandInt(111).EventTargetUser(user1Key);
            
            client1.SyncTasks().Wait();
        }
        
        private static void IsTask(MessageTask _) {
            
        }
    }
}