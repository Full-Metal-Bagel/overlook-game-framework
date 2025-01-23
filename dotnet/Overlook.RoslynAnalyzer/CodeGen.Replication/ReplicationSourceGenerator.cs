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
        sb.AppendLine("using MemoryPack;");
        sb.AppendLine("using System.Collections.Generic;");
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
            sb.AppendLine("            storage.AppendEvent(@event);");
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
        sb.AppendLine("        storage.Clear();");
        sb.AppendLine("    }");

        sb.AppendLine("    public static void CopyFrom(this EventStorage self, EventStorage other)");
        sb.AppendLine("    {");
        foreach (TypeDeclarationSyntax node in receiver.Nodes)
        {
            string text = node.Identifier.Text;
            sb.AppendLine($"        self.GetEvents<{text}>().AddRange(other.GetEvents<{text}>());");
        }
        sb.AppendLine("    }");

        sb.AppendLine("    public static void UpdateHistory(this EventStorage self, EventStorage history)");
        sb.AppendLine("    {");
        foreach (TypeDeclarationSyntax node in receiver.Nodes)
        {
            string text = node.Identifier.Text;
            sb.AppendLine($"        history.GetEvents<{text}>().AddRange(self.GetEvents<{text}>());");
            sb.AppendLine($"        self.GetEvents<{text}>().Clear();");
            sb.AppendLine($"        self.GetEvents<{text}>().AddRange(history.GetEvents<{text}>());");
        }
        sb.AppendLine("    }");

        sb.AppendLine("    public static void ComputeDelta(this EventStorage prev, EventStorage history)");
        sb.AppendLine("    {");
        foreach (TypeDeclarationSyntax node in receiver.Nodes)
        {
            string text = node.Identifier.Text;
            sb.AppendLine($"        if (prev.GetEvents<{text}>().Count > 0 && history.GetEvents<{text}>().Count > 0)");
            sb.AppendLine("        {");
            sb.AppendLine($"            history.GetEvents<{text}>().RemoveRange(0, prev.GetEvents<{text}>().Count);");
            sb.AppendLine("        }");
        }
        sb.AppendLine("    }");

        sb.AppendLine("}");

        sb.AppendLine(
            "#pragma warning disable CS9074 // The 'scoped' modifier of parameter doesn't match overridden or implemented member.");
        sb.AppendLine("public class EventStorageFormatter : MemoryPackFormatter<EventStorage>");
        sb.AppendLine("{");
        sb.AppendLine("    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref EventStorage value)");
        sb.AppendLine("    {");
        int i = 0;
        foreach (TypeDeclarationSyntax node in receiver.Nodes)
        {
            string text = node.Identifier.Text;
            sb.AppendLine($"        var events{i} = value!.GetEvents<{text}>();");
            sb.AppendLine($"        writer.GetFormatter<List<{text}>>().Serialize(ref writer, ref events{i});");
            i += 1;
        }
        sb.AppendLine("    }");

        sb.AppendLine("    public override void Deserialize(ref MemoryPackReader reader, ref EventStorage value)");
        sb.AppendLine("    {");
        sb.AppendLine("        value = new EventStorage();");
        i = 0;
        foreach (TypeDeclarationSyntax node in receiver.Nodes)
        {
            string text = node.Identifier.Text;
            sb.AppendLine($"        var events{i} = value!.GetEvents<{text}>();");
            sb.AppendLine($"        reader.GetFormatter<List<{text}>>().Deserialize(ref reader, ref events{i});");
            i += 1;
        }
        sb.AppendLine("    }");
        sb.AppendLine("}");
        context.AddSource("ReplicateEvents.g.cs", sb.ToString());

        sb.Clear();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using MemoryPack;");
        sb.AppendLine("using RelEcs;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("namespace Game;");
        sb.AppendLine(
            "#pragma warning disable CS9074 // The 'scoped' modifier of parameter doesn't match overridden or implemented member.");

        foreach (TypeDeclarationSyntax node in receiver.Nodes)
        {
            if (node is not RecordDeclarationSyntax record)
            {
                continue;
            }
            string text = node.Identifier.Text;
            sb.AppendLine($"public class {text}Formatter : MemoryPackFormatter<{text}>");
            sb.AppendLine("{");
            sb.AppendLine($"    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref {text} value)");
            sb.AppendLine("    {");
            foreach (ParameterSyntax param in record.ParameterList!.Parameters)
            {
                sb.AppendLine($"        writer.WriteValue(value.{param.Identifier.Text});");
            }
            sb.AppendLine("    }");
            sb.AppendLine($"    public override void Deserialize(ref MemoryPackReader reader, ref {text} value)");
            sb.AppendLine("    {");
            i = 0;
            foreach (ParameterSyntax param in record.ParameterList!.Parameters)
            {
                switch (param.Type)
                {
                    case PredefinedTypeSyntax predefinedTypeSyntax:
                        sb.AppendLine($"        {predefinedTypeSyntax.Keyword.Text} param{i} = default;");
                        break;
                    case IdentifierNameSyntax identifierNameSyntax:
                        sb.AppendLine($"        {identifierNameSyntax.Identifier.Text} param{i} = default;");
                        break;
                }
                sb.AppendLine($"        reader.ReadValue(ref param{i});");
                i += 1;
            }

            sb.Append($"        value = new {text}(");
            for (int j = 0; j != i - 1; j += 1)
            {
                sb.Append($"param{j}, ");
            }

            sb.Append($"param{i - 1}");
            sb.Append(");\n");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
        context.AddSource("EventFormatters.g.cs", sb.ToString());
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

