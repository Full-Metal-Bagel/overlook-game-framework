using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Overlook.Analyzers;

[Generator]
public class GlobalSuppressionsImplicityMethodsSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is MethodDeclarationSyntax,
                (ctx, _) => GetMethodDeclarationForSourceGen(ctx))
            .SelectMany((types, _) => types);

        context.RegisterSourceOutput(provider.Collect(), GenerateCode);
    }

    private static IEnumerable<MethodDeclarationSyntax> GetMethodDeclarationForSourceGen(GeneratorSyntaxContext context)
    {
        var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;
        if (methodDeclarationSyntax.AttributeLists.SelectMany(al => al.Attributes).Any())
            yield return methodDeclarationSyntax;
    }

    private void GenerateCode(SourceProductionContext context, ImmutableArray<MethodDeclarationSyntax> methodDeclarations)
    {
        var code = new StringBuilder("// <auto-generated/>");
        code.AppendLine();
        foreach (var methodName in methodDeclarations.Select(node => node.GetFullName()))
        {
            code.AppendLine($$"""[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Suppress warning from Unity", Scope = "member", Target = "{{methodName}}")]""");
        }
        context.AddSource("GlobalSuppressions.g.cs", SourceText.From(code.ToString(), Encoding.UTF8));
    }
}
