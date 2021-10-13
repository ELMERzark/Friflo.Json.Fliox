// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol.Models;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol.Tasks
{
    public sealed class ReserveKeys  : SyncRequestTask {
        [Fri.Required]  public  string          container;
        [Fri.Required]  public  int             count;
        
        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            // var store           = new SequenceStore(database, SyncTypeStore.Get(), null);
            var pools = messageContext.pools;
            using (var pooledStore = pools.Pool(() => new SequenceStore(database, SyncTypeStore.Get(), "ReserveKeys")).Get()) {
                var store = pooledStore.instance;
                var read            = store.sequence.Read();
                var sequenceTask    = read.Find(container);
                var sync            = await store.TrySync().ConfigureAwait(false);
                if (!sync.Success) {
                    return  new ReserveKeysResult { Error = new CommandError(sync.Message) };
                }
                var sequence = sequenceTask.Result;
                if (sequence == null) {
                    sequence = new Sequence {
                        container   = container,
                        autoId      = count 
                    };
                } else {
                    sequence.autoId += count;
                }
                var sequenceKeys = new SequenceKeys {
                    token       = Guid.NewGuid(),
                    container   = container,
                    start       = sequence.autoId,
                    count       = count,
                    user        = messageContext.clientId
                };
                store.sequenceKeys.Upsert(sequenceKeys);
                store.sequence.Upsert(sequence);
                sync = await store.TrySync().ConfigureAwait(false);
                if (!sync.Success) {
                    return  new ReserveKeysResult { Error = new CommandError(sync.Message) };
                }
                var keys = new ReservedKeys {
                    start = sequence.autoId,
                    count = count,
                    token = sequenceKeys.token
                };
                var result = new ReserveKeysResult { keys = keys };
                return result;
            }
        }

        internal override       TaskType        TaskType => TaskType.reserveKeys;
        public   override       string          TaskName => $"container: '{container}'";
    }
    
    public sealed class ReserveKeysResult : SyncTaskResult {
                        public  ReservedKeys?   keys;
        
                        public  CommandError    Error { get; set; }
        internal override       TaskType        TaskType => TaskType.reserveKeys;
    }
    
    public struct ReservedKeys
    {
        [Fri.Required]  public  long    start;
        [Fri.Required]  public  int     count;
        [Fri.Required]  public  Guid    token;
    }
}