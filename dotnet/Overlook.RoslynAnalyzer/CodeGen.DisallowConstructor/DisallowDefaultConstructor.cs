using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen.DisallowConstructor;

[Generator]
public class DisallowDefaultConstructor : ISourceGenerator
{
    private const string DiagnosticId = "StructUsageError";
    private const string Title = "Invalid struct usage";
    private const string MessageFormat = "Struct '{0}' with 'SomeAttribute' cannot be used without parameters";
    private const string Description = "Provide constructor parameters when using structs marked with 'SomeAttribute'.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver to find struct declarations and constructor usages
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // The SyntaxReceiver will have collected potential diagnostics
        if (context.SyntaxReceiver is not SyntaxReceiver receiver) return;

        foreach (var creation in receiver.Creations)
        {
            var model = context.Compilation.GetSemanticModel(creation.SyntaxTree);
            var typeInfo = model.GetTypeInfo(creation);

            if (typeInfo.Type is not INamedTypeSymbol typeSymbol) continue;
            if (!typeSymbol.IsValueType) continue;

            // Check for attribute by fully-qualified name
            var hasAttribute = typeSymbol.GetAttributes().Select(attributes => attributes).Any(attr => attr.AttributeClass?.ToDisplayString() == "Game.DisallowDefaultConstructorAttribute");
            if (!hasAttribute) continue;

            var diagnostic = Diagnostic.Create(new DiagnosticDescriptor(
                    "STRUCT001",
                    "Struct Instantiation Without Parameters",
                    "Struct '{0}' annotated with 'DisallowDefaultConstructor' must be instantiated with parameters.",
                    "Usage",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                creation.GetLocation(),
                typeSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    class SyntaxReceiver : ISyntaxReceiver
    {
        private readonly List<ObjectCreationExpressionSyntax> _creations = [];
        public IReadOnlyList<ObjectCreationExpressionSyntax> Creations => _creations;

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ObjectCreationExpressionSyntax creation &&
                (creation.ArgumentList == null || creation.ArgumentList.Arguments.Count == 0)
            ) _creations.Add(creation);
        }
    }
}
