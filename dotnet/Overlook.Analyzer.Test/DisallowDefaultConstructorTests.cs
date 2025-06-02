using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

namespace Overlook.Analyzers.Test;

[TestClass]
public class DisallowDefaultConstructorTests
{
    private static async Task<Diagnostic[]> GetAnalyzerDiagnosticsAsync(string sourceCode)
    {
        var compilation = CSharpCompilation.Create("TestAssembly")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(ReferenceAssemblies.Net.Net60.ResolveAsync(null, default).Result)
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(sourceCode)));

        var analyzer = new DisallowDefaultConstructor();
        var compilationWithAnalyzers = compilation.WithAnalyzers(new DiagnosticAnalyzer[] { analyzer }.ToImmutableArray());
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        
        return diagnostics.Where(d => d.Id.StartsWith("OVL")).ToArray();
    }

    [TestMethod]
    public async Task StructWithAttribute_ParameterlessInstantiation_ReportsDiagnostic()
    {
        var test = @"
using System;

namespace Overlook {
    public class DisallowDefaultConstructorAttribute : Attribute {}
    [DisallowDefaultConstructor]
    struct S {}
    class C { void M() { var s = new S(); } }
}
";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(1, diagnostics.Length, "Expected exactly one diagnostic");
        Assert.AreEqual("OVL004", diagnostics[0].Id, "Expected OVL004 diagnostic");
    }

    [TestMethod]
    public async Task StructWithAttribute_WithParameters_NoDiagnostic()
    {
        var test = @"
using System;
namespace Overlook {
    public class DisallowDefaultConstructorAttribute : Attribute {}
}
[Overlook.DisallowDefaultConstructor]
struct S { public S(int x) { } }
class C { void M() { var s = new S(1); } }
";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics");
    }

    [TestMethod]
    public async Task StructWithoutAttribute_ParameterlessInstantiation_NoDiagnostic()
    {
        var test = @"
struct S {}
class C { void M() { var s = new S(); } }
";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics");
    }
}
