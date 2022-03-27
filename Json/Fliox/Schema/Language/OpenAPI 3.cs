// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;

// Allowed namespaces: .Schema.Definition, .Schema.Doc, .Schema.Utils
namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed class OpenApi3
    {
        private  readonly   Generator                   generator;
        
        private OpenApi3 (Generator generator) {
            this.generator  = generator;
        }
        
        public static void Generate(Generator generator) {
            var emitter = new OpenApi3(generator);
            var paths = "";
            foreach (var type in generator.types) {
                if (!type.IsSchema)
                    continue;
                var sb = new StringBuilder();
                emitter.EmitPaths(type, sb);
                paths = sb.ToString();
            }
            var api = $@"
{{
  ""openapi"": ""3.0.0"",
  ""info"": {{
    ""title"":        ""example API"",
    ""description"":  ""example description"",
    ""version"":      ""0.0.0""
  }},
  ""servers"": [
    {{
      ""url"":          ""http://localhost:8010/fliox/rest/main_db/"",
      ""description"":  ""server description""
    }}
  ],
  ""paths"": {{{paths}
  }}   
}}";
            generator.files.Add("openapi.json", api);
        }
        
        private void EmitPaths(TypeDef type, StringBuilder sb) {
            foreach (var container in type.Fields) {
                EmitContainerApi(container, sb);
            }
        }
        
        private void EmitContainerApi(FieldDef container, StringBuilder sb) {
            var name = container.name;
            if (sb.Length > 0)
                sb.Append(",");
            var typeRef = Ref (container.type, true, generator);
            EmitPath(name, $"/{name}", typeRef, sb);
        }
        
        private void EmitPath(string tag, string path, string typeRef, StringBuilder sb) {
            var methodSb = new StringBuilder();
            EmitMethod(tag, "get",       new ContentRef(typeRef), null, methodSb);
            EmitMethod(tag, "delete",    new ContentText(), new [] { new QueryParam("ids", "string")}, methodSb);
            sb.Append($@"
    ""{path}"": {{");
            sb.Append(methodSb.ToString());
            sb.Append($@"
    }}");
        }
        
        private void EmitMethod(string tag, string method, Content content, ICollection<QueryParam> queryParams, StringBuilder sb) {
            if (sb.Length > 0)
                sb.Append(",");
            var querySb = new StringBuilder();
            var queryStr = "";
            if (queryParams != null) {
                foreach (var queryParam in queryParams) {
                    if (querySb.Length > 0)
                        querySb.Append(",");
                    querySb.Append(
    $@"            {{
                  ""in"":       ""query"",
                  ""name"":     ""{queryParam.name}"",
                  ""schema"":   {{ ""type"": ""{queryParam.type}"" }},
                  ""description"": ""---""
                }}
    ");
                }
                queryStr = $@"
        ""parameters"": [
          {{
            ""in"":       ""query"",
            ""name"":     ""ids"",
            ""schema"":   {{ ""type"": ""string"" }},
            ""description"": ""---""
          }}
        ],";    
            }
            var contentStr = content.Get();
            var methodStr = $@"
      ""{method}"": {{
        ""summary"":    ""return all records in articles"",
        ""tags"":       [""{tag}""],{queryStr}
        ""responses"": {{
          ""200"": {{             
            ""description"": ""OK"",
            ""content"": {contentStr}
          }}
        }}
      }}";
            sb.Append(methodStr);
        }
        
        private static string Ref(TypeDef type, bool required, Generator generator) {
            var name        = type.Name;
            var typePath    = type.Path;
            var prefix      = $"{typePath}{generator.fileExt}";
            var refType = $"\"$ref\": \"{prefix}#/definitions/{name}\"";
            if (!required)
                return $"\"oneOf\": [{{ {refType} }}, {{\"type\": \"null\"}}]";
            return refType;
        }
    }
    
    internal class QueryParam {
        internal    readonly    string  name;
        internal    readonly    string  type;
        
        internal QueryParam(string name, string type) {
            this.name   = name;
            this.type   = type;
        }
    }
    
    internal abstract class Content {
        internal    readonly    string  mimeType;
        
        internal Content(string mimeType) {
            this.mimeType   = mimeType;
        }
        
        internal abstract string Get(); 
    }
    
    internal class ContentText : Content {
        internal ContentText() : base ("text/plain") { }
        
        internal override string Get() {
            return @"{
              ""text/plain"": { }
            }";
        } 
    }

    internal class ContentRef : Content {
        private    readonly    string  type;
        
        internal ContentRef(string type) : base ("application/json") {
            this.type   = type;
        }
        
        internal override string Get() {
            return $@"{{
              ""application/json"": {{
                ""schema"": {{
                  {type}
                }}
              }}
            }}";
        }
    }

}