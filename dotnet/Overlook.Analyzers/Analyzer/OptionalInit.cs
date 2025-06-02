using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Overlook.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OptionalInit : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MissingInitialization = new(
        "SG001",
        "Missing initialization",
        "The property '{0}' must be initialized in struct '{1}'",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingInitialization);

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

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol property &&
                property.SetMethod != null &&
                (property.SetMethod.IsInitOnly || IsRequiredProperty(property)) &&
                property.SetMethod.DeclaredAccessibility == Accessibility.Public &&
                !IsOptionalProperty(property)
            )
            {
                if (creation is ObjectCreationExpressionSyntax objectCreation)
                {
                    // If there's no initializer, report missing initialization
                    if (objectCreation.Initializer == null)
                    {
                        ReportMissingInitialization(context, creation, property, typeSymbol);
                        continue;
                    }

                    // Check if the property is assigned in the initializer (any value including default is acceptable)
                    var isPropertyAssigned = objectCreation.Initializer.Expressions
                        .OfType<AssignmentExpressionSyntax>()
                        .Any(assignment => IsPropertyAssignment(assignment, property.Name));

                    if (!isPropertyAssigned)
                    {
                        ReportMissingInitialization(context, creation, property, typeSymbol);
                    }
                }
            }
        }
    }

    private static bool IsPropertyAssignment(AssignmentExpressionSyntax assignment, string propertyName)
    {
        // Handle both simple property names and qualified property names
        return assignment.Left switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText == propertyName,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText == propertyName,
            _ => assignment.Left.ToString() == propertyName
        };
    }

    private static void ReportMissingInitialization(SyntaxNodeAnalysisContext context, SyntaxNode creation, IPropertySymbol property, INamedTypeSymbol typeSymbol)
    {
        var diagnostic = Diagnostic.Create(
            MissingInitialization,
            creation.GetLocation(),
            property.Name,
            typeSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsOptionalProperty(IPropertySymbol property)
    {
        return property.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == "Overlook.OptionalOnInitAttribute");
    }

    private static bool IsRequiredProperty(IPropertySymbol property)
    {
        return property.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == "Overlook.RequiredOnInitAttribute");
    }
}
