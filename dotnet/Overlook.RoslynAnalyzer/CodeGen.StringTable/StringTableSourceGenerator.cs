using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen.StringTable;

[Generator]
public class StringTableSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxContextReceiver receiver)
        {
            return;
        }

        if (receiver.Nodes.Count == 0)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine();
        builder.AppendLine("namespace Game");
        builder.AppendLine("{");
        builder.AppendLine("    public static class StringTableExtensions");
        builder.AppendLine("    {");
        foreach ((EnumDeclarationSyntax enumDecl, AttributeSyntax attribute) in receiver.Nodes)
        {
            string? stringTable = attribute.ArgumentList?.Arguments[0].ToString();
            if (stringTable == null)
            {
                continue;
            }

            if (enumDecl.Members.Count == 0)
            {
                continue;
            }
            string enumType = enumDecl.Identifier.ValueText;

            builder.AppendLine($"        private static readonly string[] s_{enumType}Strings = {{");
            foreach (EnumMemberDeclarationSyntax memberSyntax in enumDecl.Members)
            {
                builder.AppendLine($"            \"{memberSyntax.ToString()}\",");
            }
            builder.AppendLine("        };");

            builder.AppendLine($$$"""
                                          public static string GetString(this {{{enumType}}} value) =>
                                              UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase
                                                  .GetLocalizedString({{{stringTable}}}, s_{{{enumType}}}Strings[(int)value]);
                                  """);
            builder.AppendLine();
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");
        context.AddSource("StringTables.g.cs", builder.ToString());
    }

    private sealed class SyntaxContextReceiver : ISyntaxReceiver
    {
        public HashSet<(EnumDeclarationSyntax type, AttributeSyntax attribute)> Nodes { get; } = [];

        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            if (node is not EnumDeclarationSyntax enumNode)
            {
                return;
            }

            foreach (AttributeListSyntax attributeList in enumNode.AttributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    if (attribute.Name.ToString() == "StringTable")
                    {
                        Nodes.Add((enumNode, attribute));
                        break;
                    }
                }
            }
        }
    }
}
