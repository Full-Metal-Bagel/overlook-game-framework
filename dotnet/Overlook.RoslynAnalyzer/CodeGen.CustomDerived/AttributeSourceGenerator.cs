using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen.CustomDerived;

// TODO: that would be better if we can extend this code-gen in our code by attribute?
[Generator]
public class AttributeSourceGenerator : ISourceGenerator
{
    private static readonly string s_attributeClipTemplate = "public sealed class {0}_AttributeClip : Game.AttributeClipVariable<{1}, {2}> {{ }}";

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Ensure the receiver is of the expected type
        if (context.SyntaxReceiver is not SyntaxContextReceiver receiver) return;
        if (receiver.AttributeTypes.Count == 0) return;

        Generate("AttributeClipVariable.g.cs", "Game.__AttributeClipN__", valueTypeTemplate: s_attributeClipTemplate, referenceTypeTemplate: null);
        return;

        void Generate(string filename, string @namespace, string? valueTypeTemplate, string? referenceTypeTemplate)
        {
            var builder = new StringBuilder();
            builder.AppendLine("#nullable enable");
            builder.AppendLine("using System;"); // Add necessary usings at the top
            builder.AppendLine();
            builder.AppendLine($"namespace {@namespace}");
            builder.AppendLine("{");
            foreach (var (typeDeclarationSyntax, valueType) in receiver.AttributeTypes)
            {
                var typeName = typeDeclarationSyntax.Identifier.Text;
                var fullTypeName = typeName;
                var syntaxNode = typeDeclarationSyntax.Parent;
                while (syntaxNode != null)
                {
                    fullTypeName = syntaxNode switch
                    {
                        TypeDeclarationSyntax type => $"{type.Identifier.Text}.{fullTypeName}",
                        BaseNamespaceDeclarationSyntax n => $"{n.Name}.{fullTypeName}",
                        _ => fullTypeName
                    };
                    syntaxNode = syntaxNode.Parent;
                }

                var isValueType = typeDeclarationSyntax.Kind() is SyntaxKind.StructDeclaration or SyntaxKind.RecordStructDeclaration;
                if (valueTypeTemplate != null && isValueType)
                {
                    builder.AppendLine(string.Format(valueTypeTemplate, typeName, fullTypeName, valueType.ToString()));
                }
                if (referenceTypeTemplate != null && isValueType)
                {
                    builder.AppendLine(string.Format(referenceTypeTemplate, typeName, fullTypeName, valueType.ToString()));
                }
            }
            builder.AppendLine("}");
            context.AddSource(filename, builder.ToString());
        }
    }

    private sealed class SyntaxContextReceiver : ISyntaxReceiver
    {
        public List<(TypeDeclarationSyntax attributeType, TypeSyntax valueType)> AttributeTypes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            if (node is TypeDeclarationSyntax typeNode)
            {
                if (typeNode.BaseList == null) return;
                var interfaceType = typeNode.BaseList.Types.FirstOrDefault(type => type.ToString().StartsWith("IAttribute<"))?.Type as GenericNameSyntax;
                if (interfaceType == null) return;
                if (typeNode.AttributeLists.SelectMany(a => a.Attributes).All(attribute => attribute.Name.ToString() != "TypeGuid")) return;
                var valueType = interfaceType.TypeArgumentList.Arguments[0];
                AttributeTypes.Add((typeNode, valueType));
            }
        }
    }
}
