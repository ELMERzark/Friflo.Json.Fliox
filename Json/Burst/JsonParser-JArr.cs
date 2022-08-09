﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System; 
using System.Diagnostics; 
using static Friflo.Json.Burst.Utf8JsonParser;

namespace Friflo.Json.Burst
{
    /* public enum Skip {
        No,
        Auto
    } */
   
    public ref struct JArr {
        private readonly   int     expectedLevel;  // todo exclude in RELEASE
        private            bool    hasIterated;
        private            bool    usedMember;
        
        internal JArr(int level) {

            this.expectedLevel = level;
            hasIterated = false;
            usedMember = false;
        }

        public bool NextArrayElement (ref Utf8JsonParser p) { // , Skip skip)
            if (p.lastEvent == JsonEvent.Error)
                return false;
            
            if (hasIterated) {
#if DEBUG
                int level = p.stateLevel;
                if (p.lastEvent == JsonEvent.ObjectStart || p.lastEvent == JsonEvent.ArrayStart)
                    level--;
                if (level != expectedLevel)
                    throw new InvalidOperationException("Unexpected iterator level in NextArrayElement()");
                State curState = p.state.array[level];
                if (curState != State.ExpectElement) 
                    throw new InvalidOperationException("NextArrayElement() - expect subsequent iteration being inside an array");
#endif
                // if (skip == Skip.Auto) {
                if (usedMember) {
                    usedMember = false; // clear found flag for next iteration
                }
                else {
                    if (!p.SkipEvent())
                        return false;
                }
                // }
            } else {
                // assertion is cheap -> throw exception also in DEBUG & RELEASE
                if (p.lastEvent != JsonEvent.ArrayStart)
                    throw new InvalidOperationException("NextArrayElement() - expect initial iteration with an array (ArrayStart)");
                hasIterated = true;
            }
            JsonEvent ev = p.NextEvent();
            switch (ev) {
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                case JsonEvent.ValueNull:
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                    return true;
                case JsonEvent.ArrayEnd:
                    break;
                case JsonEvent.ObjectEnd:
                    // assertion is cheap -> throw exception also in DEBUG & RELEASE
                    throw new InvalidOperationException("unexpected ObjectEnd in NextArrayElement()");
            }
            return false;
        }
        
        // ----------- array element checks -----------
        [Conditional("DEBUG")]
        private void UseElement(ref Utf8JsonParser p) {
            if (!hasIterated)
                throw new InvalidOperationException("Must call UseElement...() only after NextArrayElement()");

            int level = p.stateLevel;
            if (p.lastEvent == JsonEvent.ObjectStart || p.lastEvent == JsonEvent.ArrayStart)
                level--;
            if (level != expectedLevel)
                throw new InvalidOperationException("Unexpected iterator level in UseElement...() method");
            State curState = p.state.array[level];
            if (curState != State.ExpectElement)
                throw new InvalidOperationException("Must call UseElement...() method on within an array");
        }
        
        public bool UseElementObj(ref Utf8JsonParser p, out JObj obj) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ObjectStart) {
                obj = new JObj(-1);
                return false;
            }
            usedMember = true;
            obj = new JObj(p.stateLevel);
            return true;
        }
        
        public bool UseElementArr(ref Utf8JsonParser p, out JArr arr) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ArrayStart) {
                arr = new JArr(-1);
                return false;
            }
            usedMember = true;
            arr = new JArr(p.stateLevel);
            return true;
        }
        
        public bool UseElementNum(ref Utf8JsonParser p) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ValueNumber)
                return false;
            usedMember = true;
            return true;
        }
        
        public bool UseElementStr(ref Utf8JsonParser p) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ValueString)
                return false;
            usedMember = true;
            return true;
        }
        
        public bool UseElementBln(ref Utf8JsonParser p) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ValueBool)
                return false;
            usedMember = true;
            return true;
        }
        
        public bool UseElementNul(ref Utf8JsonParser p) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ValueNull)
                return false;
            usedMember = true;
            return true;
        }
    }
}
