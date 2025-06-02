using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Overlook.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisallowDefaultConstructor : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor StructInstantiationWithoutParameters = new(
        "STRUCT001",
        "Struct Instantiation Without Parameters",
        "Struct '{0}' annotated with 'DisallowDefaultConstructor' must be instantiated with parameters.",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(StructInstantiationWithoutParameters);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (BaseObjectCreationExpressionSyntax)context.Node;

        // Only analyze object creations with no constructor arguments
        if (creation.ArgumentList != null && creation.ArgumentList.Arguments.Count > 0)
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(creation);
        if (typeInfo.Type is not INamedTypeSymbol typeSymbol)
            return;

        if (!typeSymbol.IsValueType)
            return;

        // Check for attribute by fully-qualified name
        var hasAttribute = typeSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == "Overlook.DisallowDefaultConstructorAttribute");
        if (!hasAttribute)
            return;

        var diagnostic = Diagnostic.Create(
            StructInstantiationWithoutParameters,
            creation.GetLocation(),
            typeSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }
}
