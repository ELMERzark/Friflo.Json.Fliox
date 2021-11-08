﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Text;

namespace Friflo.Json.Fliox.Hub.Protocol
{
    // ----------------------------------- response -----------------------------------
    public sealed class ErrorResponse : ProtocolResponse
    {
        public              string      message;
        
        internal override   MessageType MessageType => MessageType.error;

        public override     string      ToString() => message;

        public static StringBuilder ErrorFromException(Exception e) {
            var sb = new StringBuilder();
            sb.Append("Internal ");
            sb.Append(e.GetType().Name);
            sb.Append(": ");
            sb.Append(e.Message);
            var stack = e.StackTrace;
            if (stack != null) {
                // Remove StackTrace sections starting with:
                // --- End of stack trace from previous location where exception was thrown ---
                // Remove these sections as they bloat the stacktrace assuming the relevant part of the stacktrace
                // is at the beginning.
                var endOfStackTraceFromPreviousLocation = stack.IndexOf("\n--- End of stack", StringComparison.Ordinal);
                if (endOfStackTraceFromPreviousLocation != -1) {
                    stack = stack.Substring(0, endOfStackTraceFromPreviousLocation);
                }
                sb.Append('\n');
                sb.Append(stack);
                sb.Append(" --- Internal End");
            }
            return sb;
        }
    }
}