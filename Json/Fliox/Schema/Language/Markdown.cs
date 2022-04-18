﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;

// Allowed namespaces: .Schema.Definition, .Schema.Doc, .Schema.Utils
namespace Friflo.Json.Fliox.Schema.Language
{
    public static partial class MarkdownGenerator
    {
        public static void Generate(Generator generator) {
            EmitHtmlMermaidER(generator);
        }

        private static void EmitHtmlMermaidER(Generator generator) {
            var sb = new StringBuilder();
            var mermaidGenerator   = new Generator(generator.typeSchema, ".mmd");
            MermaidClassDiagramGenerator.Generate(mermaidGenerator);
            var mermaidFile   = mermaidGenerator.files["class-diagram.mmd"];
            sb.AppendLine($"[generated-by]: {Generator.Link}");
            sb.AppendLine();
            sb.AppendLine("```mermaid");
            sb.AppendLine(mermaidFile);
            sb.AppendLine("```");
            var markdownMermaidER = sb.ToString();
            generator.files.Add("class-diagram.md", markdownMermaidER);
        }
    }
}