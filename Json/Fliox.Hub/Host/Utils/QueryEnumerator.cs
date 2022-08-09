// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Host.Utils
{
    public abstract class QueryEnumerator : IEnumerator<JsonKey>
    {
        /// A non detached enumerator free resources of its internal enumerator when calling <see cref="Dispose"/> 
        private     bool            detached;
        private     EntityContainer container;
        /// Ensure a stored cursor can be accessed only by the user created this cursor
        public      JsonKey         UserId  { get; private set; }
        public      string          Cursor  { get; private set; }

        public abstract bool MoveNext();

        public void Reset() {
            throw new System.NotImplementedException();
        }

        public abstract JsonKey Current { get; }

        object IEnumerator.Current => throw new System.NotImplementedException();

        public void Dispose() {
            if (detached)
                return;
            DisposeEnumerator();
            var cursor = Cursor;
            if (cursor != null) {
                container.cursors.Remove(cursor);   
            }
        }
        
        // ---
        public      abstract bool               IsAsync             { get; }
        /// <summary> Preferred in favor of <see cref="CurrentValueAsync"/> to avoid a task allocation per entity </summary>
        public      abstract JsonValue          CurrentValue        { get; }
        /// <summary> If possible use <see cref="CurrentValue"/> instead </summary>
        public      abstract Task<JsonValue>    CurrentValueAsync();
        protected   abstract void               DisposeEnumerator();

        public void Attach() {
            detached = false;
        }
        
        public void Detach(string cursor, EntityContainer container, in JsonKey userId) {
            detached        = true;
            Cursor          = cursor;
            UserId          = userId;
            this.container  = container;
        }
    }
}