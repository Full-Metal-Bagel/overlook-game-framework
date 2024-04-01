using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen.FlowNode;

// https://chat.openai.com/share/c40372f6-9a57-4a8f-8ef7-6e4e534c8b2f
[Generator]
public class CustomEventNodeSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Ensure the receiver is of the expected type
        if (context.SyntaxReceiver is not SyntaxContextReceiver receiver) return;

        var builder = new StringBuilder();
        builder.AppendLine("using System;"); // Add necessary usings at the top
        builder.AppendLine();
        builder.AppendLine("namespace Game");
        builder.AppendLine("{");
        foreach (var (typeDeclaration, attribute) in receiver.DecoratedTypes)
        {
            var semanticModel = context.Compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
            var namedTypeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
            if (namedTypeSymbol == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    id: "FC0001",
                    title: "invalid type",
                    messageFormat: $"invalid type {typeDeclaration.Identifier.Text}",
                    category: "NODE",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                ), location: typeDeclaration.GetLocation(), messageArgs: null));
                continue;
            }

            var typeName = namedTypeSymbol.ToDisplayString();
            var typeId = namedTypeSymbol.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "TypeGuidAttribute")
                ?.ConstructorArguments.FirstOrDefault().Value?.ToString()?.Replace("-", "");
            var nodeName = FindArgumentValue(attribute, "Name") ?? typeDeclaration.Identifier.Text;
            var category = FindArgumentValue(attribute, "Category");
            if (string.IsNullOrEmpty(category))
            {
                var index = typeName.LastIndexOf('.');
                if (index >= 0) category = typeName.Substring(0, index).Replace('.', '/');
            }
            builder.AppendLine($$"""
                                 [ParadoxNotion.Design.Category("{{category}}")]
                                 [ParadoxNotion.Design.Icon("Icons/NotifyIcon")]
                                 [ParadoxNotion.Design.Color("ff5c5c")]
                                 [ParadoxNotion.Design.Name("{{nodeName}}")]
                                 public class CustomEventNode_{{(string.IsNullOrEmpty(typeId) ? nodeName : typeId)}} : FlowCanvas.FlowNode, ICustomEventNode<{{typeName}}>
                                 {
                                     private FlowCanvas.FlowOutput _on = default!;
                                     private {{typeName}} _event = default!;
                                     public Type EventType => typeof({{typeName}});

                                     protected override void RegisterPorts()
                                     {
                                         _on = AddFlowOutput("On");
                                         AddValueOutput("Event", () => _event);
                                 """);
            // Dynamically generate AddValueOutput calls for each property
            GenMembers(namedTypeSymbol);

            builder.AppendLine($$"""
                                     }

                                     public void HandleSystemEvents(GameData data)
                                     {
                                         foreach (var e in data.GetEvents<{{typeName}}>()) ManualInvoke(e);
                                     }

                                     public void ManualInvoke({{typeName}} value)
                                     {
                                         _event = value;
                                         _on.Call(new FlowCanvas.Flow());
                                     }
                                 }
                                 """);
        }

        builder.AppendLine("}");
        context.AddSource("GameEventNodes.g.cs", builder.ToString());
        return;

        string? FindArgumentValue(AttributeSyntax attribute, string name)
        {
            return attribute.ArgumentList?.Arguments.FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.ValueText == name)?.Expression?.ToString()?.Trim('"');
        }

        void GenMembers(INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var member in namedTypeSymbol.GetMembers())
            {
                // Filter based on some criteria, e.g., public properties
                if (member.DeclaredAccessibility != Accessibility.Public || member.IsStatic) continue;

                var name = member.Name;
                if (string.IsNullOrEmpty(name)) continue;

                var type = member switch
                {
                    IPropertySymbol property => property.Type.ToDisplayString(),
                    IFieldSymbol field => field.Type.ToDisplayString(),
                    _ => ""
                };
                if (string.IsNullOrEmpty(type)) continue;

                var guid = member.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Name == "PropertyGuidAttribute")
                    ?.ConstructorArguments.FirstOrDefault().Value?.ToString()
                    ?? ""
                ;

                builder.AppendLine($"        AddValueOutput<{type}>(name: \"{name}\", ID: \"{guid}\", () => _event.{name});");
            }
        }

    }

    private sealed class SyntaxContextReceiver : ISyntaxReceiver
    {
        public List<(TypeDeclarationSyntax type, AttributeSyntax attribute)> DecoratedTypes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Check for class, struct, or interface declarations
            if (syntaxNode is not TypeDeclarationSyntax typeDeclaration) return;

            foreach (var attributeList in typeDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    // Assuming 'SomeAttribute' is the simple name of the attribute
                    // You might need to adjust the comparison for full names or other scenarios
                    if (attribute.Name.ToString() == "CustomEventNode")
                    {
                        DecoratedTypes.Add((typeDeclaration, attribute));
                        break; // Break from the inner loop once we find the attribute
                    }
                }
            }
        }
    }
}
