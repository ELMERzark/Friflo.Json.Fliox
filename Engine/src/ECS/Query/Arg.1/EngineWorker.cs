﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

// ReSharper disable InlineTemporaryVariable
// ReSharper disable CheckNamespace
namespace Friflo.Engine.ECS;

internal sealed class EngineWorker
{
    private  readonly   Thread              thread;
    private  readonly   EngineWorkerPool    pool;
    private  readonly   AutoResetEvent      start;
    internal readonly   AutoResetEvent      finished;
    private             Action              action;
    private             bool                running;

    public   override   string              ToString() => GetString();

    internal EngineWorker(EngineWorkerPool pool, int id) {
        this.pool   = pool;
        start       = new AutoResetEvent(false); // false: Not signaled
        finished    = new AutoResetEvent(false); // false: Not signaled
        thread      = new Thread(Run) {
            Name = $"{nameof(EngineWorker)} {id}"
        };
        thread.Start();
    }
    
    internal void Signal(Action action)
    {
        this.action = action;
        start.Set(); // Sets the state of the event to signaled, allowing one or more waiting threads to proceed.
    }
    
    private void Run()
    {
        while (true)
        {
            try {
                start.WaitOne();
                running = true;
                // ReSharper disable once PossibleNullReferenceException - waiting on finished ensures action is not null
                action();
            }
            finally {
                finished.Set();
                running = false;
                action  = null;
                var poolStack = pool.stack;
                lock (poolStack) {
                    poolStack.Push(this);
                }
                pool.availableThreads.Release();
            }
        }
    }
    
    private string GetString()
    {
        if (action == null) {
            return thread.Name + " - idle";
        }
        if (running) {
            return thread.Name + " - running";
        }
        return thread.Name + " - waiting";
    }
}


internal sealed class EngineWorkerPool
{
    internal static readonly EngineWorkerPool   Instance = new();
    
    internal readonly   Stack<EngineWorker>     stack;
    internal readonly   Semaphore               availableThreads;
    private             int                     threadSeq;
    
    private EngineWorkerPool()
    {
        stack               = new Stack<EngineWorker>();
        var count           = Environment.ProcessorCount;
        availableThreads    = new Semaphore(count, count, "available engine threads");
    }
    
    internal EngineWorker Execute(Action action)
    {
        var             poolStack = stack;
        EngineWorker    engineWorker;
        availableThreads.WaitOne();
        
        lock (poolStack)
        {
            if (!poolStack.TryPop(out engineWorker)) {
                engineWorker = new EngineWorker(this, ++threadSeq);
            }
        }
        engineWorker.Signal(action);

        return engineWorker;
    }
}

