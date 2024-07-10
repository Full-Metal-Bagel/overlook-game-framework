using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen.CustomDerived;

// TODO: that would be better if we can extend this code-gen in our code by attribute?
[Generator]
public class SystemEventSourceGenerator : ISourceGenerator
{
    private static readonly string s_hasSystemEventConditionTaskTemplate =
        """
        [ParadoxNotion.Design.Category("KGP/Condition")]
        public sealed class HasSystemEvent{0} : Game.HasSystemEvent<{1}> {{ }}
        """;

    private static readonly string s_systemEventSender = "public sealed class {0}_SystemEventSender : Game.SystemEventSender<{1}> {{ }}";

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Ensure the receiver is of the expected type
        if (context.SyntaxReceiver is not SyntaxContextReceiver receiver) return;
        if (receiver.SystemEventTypes.Count == 0) return;

        Generate("SystemEventConditionTask.g.cs", "Game", s_hasSystemEventConditionTaskTemplate);
        Generate("SystemEventSender.g.cs", "Game.__SystemEventSender__", s_systemEventSender);
        return;

        void Generate(string filename, string @namespace, string template)
        {
            var builder = new StringBuilder();
            builder.AppendLine("#nullable enable");
            builder.AppendLine("using System;"); // Add necessary usings at the top
            builder.AppendLine();
            builder.AppendLine($"namespace {@namespace}");
            builder.AppendLine("{");
            foreach (var typeDeclarationSyntax in receiver.SystemEventTypes)
            {
                var fullTypeName = typeDeclarationSyntax.Identifier.Text;
                var syntaxNode = typeDeclarationSyntax.Parent;
                while (syntaxNode != null)
                {
                    fullTypeName = syntaxNode switch
                    {
                        TypeDeclarationSyntax type => $"{type.Identifier.Text}.{fullTypeName}",
                        NamespaceDeclarationSyntax n => $"{n.Name}.{fullTypeName}",
                        _ => fullTypeName
                    };
                    syntaxNode = syntaxNode.Parent;
                }
                builder.AppendLine(string.Format(template, typeDeclarationSyntax.Identifier.Text, fullTypeName));
            }
            builder.AppendLine("}");
            context.AddSource(filename, builder.ToString());
        }
    }

    private sealed class SyntaxContextReceiver : ISyntaxReceiver
    {
        public List<TypeDeclarationSyntax> SystemEventTypes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Check for class, struct, or interface declarations
            if (syntaxNode is not TypeDeclarationSyntax typeDeclarationSyntax) return;

            foreach (var attributeList in typeDeclarationSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    // Assuming 'SomeAttribute' is the simple name of the attribute
                    // You might need to adjust the comparison for full names or other scenarios
                    if (attribute.Name.ToString() == "SystemEvent")
                    {
                        SystemEventTypes.Add(typeDeclarationSyntax);
                        break; // Break from the inner loop once we find the attribute
                    }
                }
            }
        }
    }
}
