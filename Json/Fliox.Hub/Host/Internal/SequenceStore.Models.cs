// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;

// ReSharper disable NotAccessedField.Global
namespace Friflo.Json.Fliox.Hub.Host.Internal
{
    // ---------------------------------- entity models ----------------------------------
    public sealed class Sequence {
        [Key]       public  string  container;
        [Required]  public  long    autoId;
        [Serialize               ("_etag")]
                    public  string  etag;
    }
    
    public sealed class SequenceKeys {
        [Key]       public  Guid    token;  // secret to ensure the client has reserved the keys
        [Required]  public  string  container;
        [Required]  public  long    start;
        [Required]  public  int     count;
                    public  JsonKey user;   // to track back who reserved keys in case of abuse
    }
}