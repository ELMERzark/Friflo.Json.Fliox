// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public static class TestJobQueuePoc
    {
        private static readonly ConcurrentQueue<Job> Queue = new ConcurrentQueue<Job>();

        [Test]
        public static async Task Test() {
            
            var thread = new Thread(() =>
            {
                Console.WriteLine($"queue - thread {Environment.CurrentManagedThreadId}");

                while(true) {
                    if (!Queue.TryDequeue(out var job))
                        continue;
                    if (job == null)
                        return;
                    job.action();
                    job.tcs.SetResult(11);
                }
            });
            thread.Start();
            
            var myJob = new Job(() => {
                Console.WriteLine($"myJob - thread {Environment.CurrentManagedThreadId}");
            });
            Queue.Enqueue(myJob);
            await myJob.tcs.Task.ConfigureAwait(false);
            Queue.Enqueue(null);
        }

        private sealed class Job
        {
            internal readonly TaskCompletionSource<int>  tcs = new TaskCompletionSource<int>();
            internal readonly Action                     action;
            
            internal Job(Action action) {
                this.action = action;
            }
        }
    }
}
