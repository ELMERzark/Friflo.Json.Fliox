// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable ClassNeverInstantiated.Global
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary> prototype WIP </summary>
    public struct EventTargets
    {
        internal IList<UserClient>   userClients;
        
        public EventTargets (string user) {
            userClients = new List<UserClient> { new UserClient(user) };
        }
        
        public EventTargets (JsonKey user) {
            userClients = new List<UserClient> { new UserClient(user) };
        }
        
        public EventTargets (string user, string  client) {
            userClients = new List<UserClient> { new UserClient(user, client) };
        }
        
        public EventTargets (JsonKey user, JsonKey  client) {
            userClients = new List<UserClient> { new UserClient(user, client) };
        }
        
        public EventTargets (UserClient userClient) {
            userClients = new List<UserClient> { userClient };
        }
        
        public void AddUser(string user, string client = null) {
            AddClient (new UserClient(user, client));
        }
        
        public void AddUser(JsonKey user, JsonKey client) {
            AddClient (new UserClient(user, client));
        }

        public void AddClient(UserClient client) {
            if (userClients == null) {
                userClients = new List<UserClient> { client };
                return;
            }
            userClients.Add(client);
        }
        
        public void AddClients (ICollection<string> users) {
            if (userClients == null) userClients = new List<UserClient>(users.Count);
            foreach (var user in users) {
                userClients.Add(new UserClient(user));
            }
        }
        
        public void AddClients (ICollection<JsonKey> users) {
            if (userClients == null) userClients = new List<UserClient>(users.Count);
            foreach (var user in users) {
                userClients.Add(new UserClient(user));
            }
        }
        
        public void AddClients (ICollection<(string, string)> userClients) {
            if (this.userClients == null) this.userClients = new List<UserClient>(userClients.Count);
            foreach (var (user, client) in userClients) {
                this.userClients.Add(new UserClient(user, client));
            }
        }
        
        internal void AddClients (ICollection<UserClient> userClients) {
            if (this.userClients == null) this.userClients = new List<UserClient>(userClients.Count);
            foreach (var element in userClients) {
                this.userClients.Add(element);
            }
        }
    }
    
    public static class MessageTargetExtension
    {
        // --- user
        public static  TTask  EventTargetUser<TTask> (this TTask messageTask, string  user) where TTask : MessageTask {
            messageTask.EventTargets.AddClient(new UserClient(user));
            return messageTask;
        }
        
        public static  TTask  EventTargetUser<TTask> (this TTask messageTask, JsonKey user) where TTask : MessageTask{
            messageTask.EventTargets.AddClient(new UserClient(user));
            return messageTask;
        }
        
        // --- user client
        public static  TTask  EventTargetClient<TTask> (this TTask messageTask, string  user, string client) where TTask : MessageTask{
            messageTask.EventTargets.AddClient(new UserClient(user, client));
            return messageTask;
        }
        
        public static  TTask  EventTargetClient<TTask> (this TTask messageTask, JsonKey user, JsonKey client) where TTask : MessageTask{
            messageTask.EventTargets.AddClient(new UserClient(user, client));
            return messageTask;
        }
        
        public static  TTask  EventTargetClient<TTask> (this TTask messageTask, UserClient client) where TTask : MessageTask {
            messageTask.EventTargets.AddClient(client);
            return messageTask;
        }
        
        // --- users
        public static  TTask  EventTargetUsers<TTask> (this TTask messageTask, ICollection<string>  users) where TTask : MessageTask {
            messageTask.EventTargets.AddClients(users);
            return messageTask;
        }
        public static  TTask  EventTargetUsers<TTask> (this TTask messageTask, ICollection<JsonKey>  users) where TTask : MessageTask {
            messageTask.EventTargets.AddClients (users);
            return messageTask;
        }
        
        // --- user clients
        public static  TTask  EventTargetClients<TTask> (this TTask messageTask, ICollection<(string, string)>  clients) where TTask : MessageTask {
            messageTask.EventTargets.AddClients(clients);
            return messageTask;
        }
        public static  TTask  EventTargetClients<TTask> (this TTask messageTask, ICollection<UserClient>  clients) where TTask : MessageTask {
            messageTask.EventTargets.AddClients (clients);
            return messageTask;
        }
    }
    
    public readonly struct UserClient {
        public  readonly    JsonKey     user;
        public  readonly    JsonKey     client;
        
        public UserClient (string user, string client = null) {
            this.user   = new JsonKey(user);
            this.client = new JsonKey(client);
        }
        
        public UserClient (JsonKey user, JsonKey client = default) {
            this.user   = user;
            this.client = client;
        }
    }
}