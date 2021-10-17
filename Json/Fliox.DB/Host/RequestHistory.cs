// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Friflo.Json.Fliox.DB.Host
{
    internal class RequestHistories
    {
        internal readonly   List<RequestHistory>    histories = new List<RequestHistory>();
        private  readonly   Stopwatch               watch = new Stopwatch();
        
        internal RequestHistories() {
            histories.Add(new RequestHistory(1,  10));
            histories.Add(new RequestHistory(10, 10));
            watch.Start();
        }
        
        internal void Update() {
            int elapsed = (int)(watch.ElapsedMilliseconds / 1000);
            foreach (var history in histories) {
                history.Update(elapsed);
            }
            // foreach (var history in histories) { Console.Out.WriteLine(string.Join(", ", history.counters)); }
        }
    }
    
    internal class RequestHistory {
        public   readonly   int     resolution;  // [second]
        private  readonly   int[]   counters;
        public              int     LastUpdate {get; private set; }
        public              int     Length => counters.Length;
        
        internal RequestHistory (int resolution, int size) {
            this.resolution = resolution;
            counters         = new int[size];
        }
        
        public void CopyCounters(int[] dst) {
            counters.CopyTo(dst, 0);
        }
        
        internal void Update(int elapsed) {
            int size        = counters.Length;
            var index       = (elapsed / resolution) % size;
            if (LastUpdate == index) {
                counters[index]++;
                return;
            }
            var clearIndex = (LastUpdate + 1) % size;
            while (clearIndex != index) {
                counters[clearIndex] = 0;
                clearIndex = (clearIndex + 1) % size;
            }
            counters[index] = 1;
            LastUpdate = index;
        }
    }
}