using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen.FlowNode;

[Generator]
public class CustomMethodNodeSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Ensure the receiver is of the expected type
        if (context.SyntaxReceiver is not SyntaxContextReceiver receiver) return;
        if (receiver.DecoratedMethods.Count == 0)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;"); // Add necessary usings at the top
        builder.AppendLine();
        builder.AppendLine("namespace Game");
        builder.AppendLine("{");
        foreach (var (methodDeclaration, attribute) in receiver.DecoratedMethods)
        {
            var semanticModel = context.Compilation.GetSemanticModel(methodDeclaration.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol methodSymbol)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    id: "FC0002",
                    title: "invalid method",
                    messageFormat: $"invalid method {methodDeclaration.Identifier.Text}",
                    category: "NODE",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                ), location: methodDeclaration.GetLocation(), messageArgs: null));
                continue;
            }

            var refParameter = methodSymbol.Parameters.FirstOrDefault(p => p.RefKind is RefKind.Ref or RefKind.Out or RefKind.RefReadOnly);
            if (refParameter != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    id: "FC0003",
                    title: "invalid method",
                    messageFormat: $"ref parameter is not supported yet {methodSymbol.ToDisplayString()}",
                    category: "NODE",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                ), location: methodDeclaration.GetLocation(), messageArgs: null));
                continue;
            }

            var declaringType = methodSymbol.ContainingType.ToDisplayString();
            var methodName = methodSymbol.ToDisplayString().Substring(0, methodSymbol.ToDisplayString().IndexOf('('));
            var methodAttributes = methodSymbol.GetAttributes();
            var isPure = methodAttributes.Any(attr => attr.AttributeClass?.Name == "PureAttribute");
            var methodId = methodAttributes
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "MethodGuidAttribute")
                ?.ConstructorArguments.FirstOrDefault().Value?.ToString();
            var nodeName = FindArgumentValue(attribute, "Name") ?? methodDeclaration.Identifier.Text;
            var category = FindArgumentValue(attribute, "Category");
            var returnName = FindArgumentValue(attribute, "ReturnName") ?? "Result";
            var description = FindArgumentValue(attribute, "Description");
            if (string.IsNullOrEmpty(category))
            {
                var index = methodName.LastIndexOf('.');
                if (index >= 0) category = methodName.Substring(0, index).Replace('.', '/');
            }
            builder.AppendLine($$"""
                                 [ParadoxNotion.Design.Category("{{category}}")]
                                 [ParadoxNotion.Design.Name("{{nodeName}}")]
                                 [ParadoxNotion.Design.Description("{{description}}")]
                                 public class CustomFunctionNode_{{(string.IsNullOrEmpty(methodId) ? nodeName : methodId!.Replace("-", ""))}} : FlowCanvas.FlowNode
                                 {
                                     protected override void RegisterPorts()
                                     {
                                 """);

            var parameters = methodSymbol.Parameters.Select(parameter =>
            {
                var displayName = parameter.Name;
                var displayType = parameter.Type.ToDisplayString();
                var attributes = parameter.GetAttributes();
                var isBlackboardParameter = attributes.Any(attr => attr.AttributeClass?.Name == "CustomMethodNodeBlackboardParameterAttribute");
                var id = attributes.FirstOrDefault(attr => attr.AttributeClass?.Name == "ParameterGuidAttribute")
                    ?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";
                var variableName = isBlackboardParameter ? $"_{(string.IsNullOrEmpty(id) ? displayName : id.Replace("-", ""))}" : displayName;
                return (displayName, displayType, variableName, id, isBlackboardParameter);
            }).ToArray();

            var callString = methodSymbol.IsStatic
                ? $"{methodName}({string.Join(",", parameters.Select(t => t.variableName).Select(name => name + ".value"))})"
                : $"instance.GetValue().{methodSymbol.Name}({string.Join(",", methodSymbol.Parameters.Select(p => p.Name + ".GetValue()"))})";
            if (!methodSymbol.IsStatic) builder.AppendLine($"        var instance = AddValueInput<{declaringType}>(\"Instance\");");
            foreach (var (displayName, displayType, variableName, id, _) in parameters.Where(t => !t.isBlackboardParameter))
            {
                builder.AppendLine($"        var {variableName} = AddValueInput<{displayType}>(name: \"{displayName}\", ID: \"{id}\");");
            }

            if (methodSymbol.ReturnsVoid)
            {
                builder.AppendLine($$"""
                                             AddFlowInput("Call", flow => {{callString}});
                                         }
                                     """);
            }
            else if (isPure)
            {
                builder.AppendLine($$"""
                                             AddValueOutput<{{methodSymbol.ReturnType.ToDisplayString()}}>(name: "{{returnName}}", ID: "{{methodId}}", () => {{callString}});
                                         }
                                     """);
            }
            else
            {
                builder.AppendLine($$"""
                                             var result = AddValueOutput<{{methodSymbol.ReturnType.ToDisplayString()}}>(name: "{{returnName}}", ID: "{{methodId}}", () => {{callString}});
                                             var @out = AddFlowOutput("Out");
                                             AddFlowInput("Call", flow =>
                                             {
                                                 var r = ({{methodSymbol.ReturnType.ToDisplayString()}})result;
                                                 if (!string.IsNullOrEmpty(_resultParameterName)) flow.WriteParameter(_resultParameterName, r);
                                                 flow.Call(@out);
                                             });
                                         }

                                         [ParadoxNotion.Design.ExposeField, UnityEngine.SerializeField]
                                         private string _resultParameterName = "";
                                     """);
            }

            foreach (var (displayName, displayType, variableName, _, _) in parameters.Where(t => t.isBlackboardParameter))
            {
                builder.AppendLine($"    [ParadoxNotion.Design.ExposeField, UnityEngine.SerializeField, ParadoxNotion.Design.Name(\"{displayName}\")]");
                builder.AppendLine($"    private NodeCanvas.Framework.BBParameter<{displayType}> {variableName} = default!;");
            }

            builder.AppendLine("}");
        }

        builder.AppendLine("}");
        context.AddSource("GameMethodNodes.g.cs", builder.ToString());
        return;

        string? FindArgumentValue(AttributeSyntax attribute, string name)
        {
            return attribute.ArgumentList?.Arguments.FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.ValueText == name)?.Expression?.ToString()?.Trim('"');
        }
    }

    private sealed class SyntaxContextReceiver : ISyntaxReceiver
    {
        public List<(MethodDeclarationSyntax type, AttributeSyntax attribute)> DecoratedMethods { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Check for class, struct, or interface declarations
            if (syntaxNode is not MethodDeclarationSyntax methodDeclarationSyntax) return;

            foreach (var attributeList in methodDeclarationSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    // Assuming 'SomeAttribute' is the simple name of the attribute
                    // You might need to adjust the comparison for full names or other scenarios
                    if (attribute.Name.ToString() == "CustomMethodNode")
                    {
                        DecoratedMethods.Add((methodDeclarationSyntax, attribute));
                        break; // Break from the inner loop once we find the attribute
                    }
                }
            }
        }
    }
}
