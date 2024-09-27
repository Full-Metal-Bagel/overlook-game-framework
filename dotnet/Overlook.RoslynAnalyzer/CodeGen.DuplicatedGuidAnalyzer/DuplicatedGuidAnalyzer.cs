using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeGen;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicatedGuidAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DuplicateTypeGuid, DiagnosticDescriptors.DuplicateMethodGuid);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var typeGuidMap = new Dictionary<Guid, INamedTypeSymbol>();
        var methodGuidMap = new Dictionary<Guid, IMethodSymbol>();
        context.RegisterSymbolAction(ctx => AnalyzeNamedType(ctx, typeGuidMap), SymbolKind.NamedType);
        context.RegisterSymbolAction(ctx => AnalyzeMethod(ctx, methodGuidMap), SymbolKind.Method);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, Dictionary<Guid, INamedTypeSymbol> guidMap)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
        AnalyzeGuidAttribute(context, guidMap, namedTypeSymbol, "TypeGuidAttribute",
            DiagnosticDescriptors.DuplicateTypeGuid);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, Dictionary<Guid, IMethodSymbol> guidMap)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;
        AnalyzeGuidAttribute(context, guidMap, methodSymbol, "MethodGuidAttribute",
            DiagnosticDescriptors.DuplicateMethodGuid);
    }

    private static void AnalyzeGuidAttribute<T>(
        SymbolAnalysisContext context,
        Dictionary<Guid, T> guidMap,
        T symbol,
        string attributeName,
        DiagnosticDescriptor descriptor) where T : ISymbol
    {
        var guidAttribute = symbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == attributeName);

        if (guidAttribute == null)
            return;

        if (guidAttribute.ConstructorArguments.Length == 0 ||
            guidAttribute.ConstructorArguments[0].Value is not string guidString)
            return;

        if (!Guid.TryParse(guidString, out var guid))
            return;

        if (guidMap.TryGetValue(guid, out var existingSymbol))
        {
            var diagnostic = Diagnostic.Create(
                descriptor,
                symbol.Locations[0],
                symbol.Name,
                existingSymbol.Name,
                guid);

            context.ReportDiagnostic(diagnostic);
        }
        else
        {
            guidMap[guid] = symbol;
        }
    }
}

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor DuplicateTypeGuid = new(
        id: "GUID001",
        title: "Duplicate TypeGuid detected",
        messageFormat: "Type '{0}' has the same TypeGuid as '{1}': {2}",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each type with a TypeGuidAttribute should have a unique GUID.");

    public static readonly DiagnosticDescriptor DuplicateMethodGuid = new(
        id: "GUID002",
        title: "Duplicate MethodGuid detected",
        messageFormat: "Method '{0}' has the same MethodGuid as '{1}': {2}",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each method with a MethodGuidAttribute should have a unique GUID.");
}
