#nullable enable

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen.Replication;

[Generator]
public class ReplicationSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
        {
            return;
        }

        if (receiver.Nodes.Count == 0)
        {
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("namespace Game;");
        sb.AppendLine("public static class ReplicationExtension");
        sb.AppendLine("{");
        sb.AppendLine("    public static void CollectEvents(this GameData data, EventStorage storage, int systemIndex)");
        sb.AppendLine("    {");
        foreach (TypeDeclarationSyntax node in receiver.Nodes)
        {
            string text = node.Identifier.Text;
            sb.AppendLine($"        foreach ({text} @event in data.GetEvents<{text}>().GetAllExcluding(systemIndex))");
            sb.AppendLine("        {");
            sb.AppendLine("            storage.AddEvent(@event);");
            sb.AppendLine("        }");
        }
        sb.AppendLine("    }");
        sb.AppendLine("    public static void TriggerEvents(this GameData data, EventStorage storage)");
        sb.AppendLine("    {");
        foreach (TypeDeclarationSyntax node in receiver.Nodes)
        {
            string text = node.Identifier.Text;
            sb.AppendLine($"        foreach ({text} @event in storage.GetEvents<{text}>())");
            sb.AppendLine("        {");
            sb.AppendLine("            data.AppendEvent(@event);");
            sb.AppendLine("        }");
        }
        sb.AppendLine("        storage.ClearEvents();");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        context.AddSource("ReplicateEvents.g.cs", sb.ToString());
    }

    private sealed class SyntaxReceiver : ISyntaxReceiver
    {
        public HashSet<TypeDeclarationSyntax> Nodes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            if (node is TypeDeclarationSyntax typeNode)
            {
                bool hasReplicated = false;
                bool hasSystemEvent = false;
                foreach (AttributeListSyntax attributeList in typeNode.AttributeLists)
                {
                    foreach (AttributeSyntax attribute in attributeList.Attributes)
                    {
                        if (attribute.Name.ToString() == "Replicated")
                        {
                            hasReplicated = true;
                        }

                        if (attribute.Name.ToString() == "SystemEvent")
                        {
                            hasSystemEvent = true;
                        }
                    }
                }
                if (hasReplicated && hasSystemEvent)
                {
                    Nodes.Add(typeNode);
                }
            }
        }
    }
}

