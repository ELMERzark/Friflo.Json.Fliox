﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable StaticMemberInGenericType
// ReSharper disable CoVariantArrayConversion
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;


public sealed class QueryJob<T1, T2, T3, T4, T5> : QueryJob
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
    where T5 : struct, IComponent
{
    internal            QueryChunks<T1, T2, T3, T4, T5>     Chunks      => new (query); // only for debugger
    public  override    string                              ToString()  => query.GetQueryJobString();

    [Browse(Never)]
    private readonly    ArchetypeQuery<T1, T2, T3, T4, T5>                                              query;      //  8
    private readonly    Action<Chunk<T1>, Chunk<T2>, Chunk<T3>, Chunk<T4>, Chunk<T5>, ChunkEntities>    action;     //  8
    [Browse(Never)]
    private             QueryJobTask[]                                                                  jobTasks;   //  8


    private class QueryJobTask : JobTask {
        internal    Action<Chunk<T1>, Chunk<T2>, Chunk<T3>, Chunk<T4>, Chunk<T5>, ChunkEntities>    action;
        internal    Chunks<T1, T2, T3, T4, T5>                                                      chunks;
        
        internal  override void ExecuteTask()  => action(chunks.Chunk1, chunks.Chunk2, chunks.Chunk3, chunks.Chunk4, chunks.Chunk5, chunks.Entities);
    }
    
    internal QueryJob(
        ArchetypeQuery<T1, T2, T3, T4, T5>                                              query,
        Action<Chunk<T1>, Chunk<T2>, Chunk<T3>, Chunk<T4>, Chunk<T5>, ChunkEntities>    action)
    {
        this.query  = query;
        this.action = action;
        jobRunner   = query.Store.JobRunner;
    }
    
    public override void Run()
    {
        foreach (Chunks<T1, T2, T3, T4, T5> chunk in query.Chunks) {
            action(chunk.Chunk1, chunk.Chunk2, chunk.Chunk3, chunk.Chunk4, chunk.Chunk5, chunk.Entities);
        }
    }
    
    /// <summary>Execute the query.
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#parallel-query-job">Example.</a>.<br/>
    /// All chunks having at least <see cref="QueryJob.MinParallelChunkLength"/> * <see cref="ParallelJobRunner.ThreadCount"/>
    /// components are executed <see cref="JobExecution.Parallel"/>. 
    /// </summary>
    public override void RunParallel()
    {
        if (jobRunner == null) throw JobRunnerIsNullException();
        var taskCount   = jobRunner.workerCount + 1;
        
        foreach (Chunks<T1, T2, T3, T4, T5> chunks in query.Chunks)
        {
            var chunkLength = chunks.Length;
            if (ExecuteSequential(taskCount, chunkLength)) {
                action(chunks.Chunk1, chunks.Chunk2, chunks.Chunk3, chunks.Chunk4, chunks.Chunk5, chunks.Entities);
                continue;
            }
            var tasks = jobTasks;
            if (tasks == null || tasks.Length < taskCount) {
                tasks = jobTasks = new QueryJobTask[taskCount];
                for (int n = 0; n < taskCount; n++) {
                    tasks[n] = new QueryJobTask { action = action };
                }
            }
            var sectionSize = GetSectionSize(chunkLength, taskCount, Multiple);
            var start       = 0;
            for (int taskIndex = 0; taskIndex < taskCount; taskIndex++)
            {
                var length = GetSectionLength (chunkLength, start, sectionSize);
                if (length > 0) {
                    tasks[taskIndex].chunks = new Chunks<T1, T2, T3, T4, T5>(chunks, start, length, taskIndex);
                    start += sectionSize;
                    continue;
                }
                for (; taskIndex < taskCount; taskIndex++) {
                    tasks[taskIndex].chunks = new Chunks<T1, T2, T3, T4, T5>(chunks.Entities, taskIndex);
                }
                break;
            }
            jobRunner.ExecuteJob(this, tasks);
        }
    }
    
    public  override        int ParallelComponentMultiple   => Multiple;
    private static readonly int Multiple                    = GetMultiple();
    
    private static int GetMultiple()
    {
        int lcm1 = ComponentType<T1>.ComponentMultiple;
        int lcm2 = ComponentType<T2>.ComponentMultiple;
        int lcm3 = ComponentType<T3>.ComponentMultiple;
        int lcm4 = ComponentType<T4>.ComponentMultiple;
        int lcm5 = ComponentType<T5>.ComponentMultiple;
        int lcm12   =   LeastComponentMultiple(lcm1,    lcm2);
        int lcm34   =   LeastComponentMultiple(lcm3,    lcm4);
        int lcm1234 =   LeastComponentMultiple(lcm12,   lcm34);
        return          LeastComponentMultiple(lcm1234, lcm5);
    }
}
