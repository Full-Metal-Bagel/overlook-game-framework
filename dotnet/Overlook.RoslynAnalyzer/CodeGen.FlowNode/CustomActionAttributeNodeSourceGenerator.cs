using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen.FlowNode;

[Generator]
public class CustomActionAttributeNodeSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Ensure the receiver is of the expected type
        if (context.SyntaxReceiver is not SyntaxContextReceiver receiver) return;
        if (receiver.DecoratedAttributes.Count == 0)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;"); // Add necessary usings at the top
        builder.AppendLine();
        builder.AppendLine("namespace Game");
        builder.AppendLine("{");
        foreach (var (typeDeclarationSyntax, attribute) in receiver.DecoratedAttributes)
        {
            var semanticModel = context.Compilation.GetSemanticModel(typeDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is not ITypeSymbol symbol)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    id: "FC0004",
                    title: "invalid action attribute",
                    messageFormat: $"invalid attribute type {typeDeclarationSyntax.Identifier.Text}",
                    category: "NODE",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                ), location: typeDeclarationSyntax.GetLocation(), messageArgs: null));
                continue;
            }

            var interfaceImplementation = symbol.Interfaces.FirstOrDefault(i => i.Name == "IAttribute" && i.TypeArguments.Length == 1);
            var delegateType = interfaceImplementation?.TypeArguments[0] as INamedTypeSymbol;
            if (delegateType == null || delegateType.DelegateInvokeMethod == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    id: "FC0005",
                    title: "invalid action attribute",
                    messageFormat: $"action attribute {symbol.ToDisplayString()} must implement the interface of `IAttribute<T> where T : Delegate`",
                    category: "NODE",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                ), location: typeDeclarationSyntax.GetLocation(), messageArgs: null));
                continue;
            }

            var parameters = delegateType.DelegateInvokeMethod.Parameters;
            if (parameters.FirstOrDefault(p => p.RefKind is RefKind.Ref or RefKind.Out or RefKind.RefReadOnly) != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    id: "FC0006",
                    title: "invalid action attribute",
                    messageFormat: $"ref parameter is not supported yet {symbol.ToDisplayString()}",
                    category: "NODE",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                ), location: typeDeclarationSyntax.GetLocation(), messageArgs: null));
                continue;
            }

            var typeName = symbol.ToDisplayString();
            var category = FindArgumentValue(attribute, "Category");
            if (string.IsNullOrEmpty(category))
            {
                var index = typeName.LastIndexOf('.');
                if (index >= 0) category = typeName.Substring(0, index).Replace('.', '/');
            }
            var nodeName = FindArgumentValue(attribute, "Name") ?? typeDeclarationSyntax.Identifier.Text;
            var description = FindArgumentValue(attribute, "Description");
            var typeId = symbol.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "TypeGuidAttribute")
                ?.ConstructorArguments.FirstOrDefault().Value?.ToString()?.Replace("-", "")
                ?? nodeName;
            builder.AppendLine($$"""
                                 [ParadoxNotion.Design.Category("{{category}}")]
                                 [ParadoxNotion.Design.Name("{{nodeName}}")]
                                 [ParadoxNotion.Design.Icon("Icons/NotifyIcon")]
                                 [ParadoxNotion.Design.Color("ff5c5c")]
                                 [ParadoxNotion.Design.Description("{{description}}")]
                                 public class CustomActionAttributeNode_{{typeId}} : FlowCanvas.FlowNode, Game.IActionAttributeNode
                                 {
                                     public System.Type AttributeType => typeof({{typeName}});
                                     public System.Type ActionType => typeof({{delegateType.ToDisplayString()}});
                                     private FlowCanvas.FlowOutput _on = default!;

                                     [UnityEngine.SerializeField, ParadoxNotion.Design.ExposeField]
                                     private bool _createActionAttribute = true;

                                     private {{delegateType.ToDisplayString()}} _action = default!;

                                     public override void OnGraphStarted()
                                     {
                                         AddAction(((Game.IEntityGraphAgent)graphAgent).GameEntity);
                                     }

                                     public override void OnGraphStoped()
                                     {
                                         RemoveAction(((Game.IEntityGraphAgent)graphAgent).GameEntity);
                                     }

                                     public void AddAction(in GameEntity entity)
                                     {
                                        if (!entity.Has<{{typeName}}>())
                                        {
                                            if (!_createActionAttribute)
                                            {
                                                Debug.LogWarning("cannot found {{typeName}}");
                                                return;
                                            }
                                            entity.Add(new {{typeName}}(default));
                                        }
                                        entity.Get<{{typeName}}>().Value += GetAction();
                                     }

                                     public void RemoveAction(in GameEntity entity)
                                     {
                                        if (entity.Has<{{typeName}}>())
                                        {
                                            var action = entity.Get<{{typeName}}>().Value;
                                            if (action != null) action -= GetAction();
                                        }
                                     }

                                     protected override void RegisterPorts()
                                     {
                                         _on = AddFlowOutput("On");
                                 """);

            var actionParameters = parameters.Select(parameter =>
            {
                var displayName = parameter.Name;
                var displayType = parameter.Type.ToDisplayString();
                var attributes = parameter.GetAttributes();
                var id = attributes.FirstOrDefault(attr => attr.AttributeClass?.Name == "ParameterGuidAttribute")
                    ?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";
                var variableName = $"_{(string.IsNullOrEmpty(id) ? displayName : id.Replace("-", ""))}";
                return (displayName, displayType, variableName, id);
            }).ToArray();

            foreach (var (displayName, displayType, variableName, id) in actionParameters)
            {
                builder.AppendLine($"        AddValueOutput<{displayType}>(\"{displayName}\", () => {variableName}, ID: \"{id}\");");
            }

            builder.AppendLine("    }");
            builder.AppendLine($"    private {delegateType.ToDisplayString()} GetAction()");
            builder.AppendLine("    {");
            builder.AppendLine("        if (_action != null) return _action;");
            builder.AppendLine($"        _action = ({string.Join(", ", actionParameters.Select(p => p.displayName))}) =>");
            builder.AppendLine("        {");
            foreach (var (displayName, displayType, variableName, id) in actionParameters)
            {
                builder.AppendLine($"            {variableName} = {displayName};");
            }
            builder.AppendLine("            _on.Call(new FlowCanvas.Flow());");
            builder.AppendLine("        };");
            builder.AppendLine("        return _action;");
            builder.AppendLine("    }");

            foreach (var (displayName, displayType, variableName, id) in actionParameters)
            {
                builder.AppendLine($"    private {displayType} {variableName};");
            }

            builder.AppendLine("}");
        }

        builder.AppendLine("}");
        context.AddSource("GameActionAttributeNodes.g.cs", builder.ToString());
        return;

        string? FindArgumentValue(AttributeSyntax attribute, string name)
        {
            return attribute.ArgumentList?.Arguments.FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.ValueText == name)?.Expression?.ToString()?.Trim('"');
        }
    }

    private sealed class SyntaxContextReceiver : ISyntaxReceiver
    {
        public List<(TypeDeclarationSyntax type, AttributeSyntax attribute)> DecoratedAttributes { get; } = new();

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
                    if (attribute.Name.ToString() == "CustomActionAttributeNode")
                    {
                        DecoratedAttributes.Add((typeDeclarationSyntax, attribute));
                        break; // Break from the inner loop once we find the attribute
                    }
                }
            }
        }
    }
}
