using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen.OptionalInit;

[Generator]
public class OptionalInit : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver to find struct declarations and constructor usages
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    // https://chatgpt.com/share/62b1ec87-ec70-4d84-aca2-4a3b693a53c2
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            return;

        var compilation = context.Compilation;

        foreach (var creation in receiver.Creations)
        {
            var model = compilation.GetSemanticModel(creation.SyntaxTree);
            var typeInfo = model.GetTypeInfo(creation);
            if (typeInfo.Type is not INamedTypeSymbol typeSymbol)
                continue;

            foreach (var member in typeSymbol.GetMembers())
            {
                if (member is IPropertySymbol property &&
                    property.SetMethod != null &&
                    (property.SetMethod.IsInitOnly || IsRequiredProperty(property)) &&
                    property.SetMethod.DeclaredAccessibility == Accessibility.Public &&
                    !IsOptionalProperty(property)
                )
                {
                    if (creation is ObjectCreationExpressionSyntax objectCreation &&
                        (objectCreation.Initializer == null ||
                         objectCreation.Initializer.Expressions.OfType<AssignmentExpressionSyntax>().All(e => e.Left.ToString() != property.Name))
                    )
                    {
                        // Report diagnostic for missing initialization
                        var diagnostic = Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "SG001",
                                "Missing initialization",
                                $"The property '{property.Name}' must be initialized in struct '{typeSymbol.Name}'",
                                "Usage",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true),
                            creation.GetLocation());

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        bool IsOptionalProperty(IPropertySymbol property)
        {
            return property.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == "Game.OptionalOnInitAttribute");
        }

        bool IsRequiredProperty(IPropertySymbol property)
        {
            return property.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == "Game.RequiredOnInitAttribute");
        }
    }

    class SyntaxReceiver : ISyntaxReceiver
    {
        private readonly List<BaseObjectCreationExpressionSyntax> _creations = [];
        public IReadOnlyList<BaseObjectCreationExpressionSyntax> Creations => _creations;

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is BaseObjectCreationExpressionSyntax creation &&
                (creation.ArgumentList == null || creation.ArgumentList.Arguments.Count == 0)
            ) _creations.Add(creation);
        }
    }
}
