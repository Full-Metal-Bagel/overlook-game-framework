using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Overlook.Analyzers.Test;

[TestClass]
public class DuplicatedGuidAnalyzerTests
{
    private static async Task<Diagnostic[]> GetAnalyzerDiagnosticsAsync(string sourceCode)
    {
        var compilation = CSharpCompilation.Create("TestAssembly")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(ReferenceAssemblies.Net.Net60.ResolveAsync(null, default).Result)
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(sourceCode)));

        var analyzer = new DuplicatedGuidAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(new DiagnosticAnalyzer[] { analyzer }.ToImmutableArray());
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics.Where(d => d.Id.StartsWith("GUID")).ToArray();
    }

    [TestMethod]
    public async Task NoDuplicateTypeGuids_NoDiagnostic()
    {
        var test = """
                   using System;

                   [AttributeUsage(AttributeTargets.Class)]
                   public class TypeGuidAttribute : Attribute
                   {
                       public TypeGuidAttribute(string guid) {}
                   }

                   [TypeGuid("00000000-0000-0000-0000-000000000001")]
                   class A {}
                   [TypeGuid("00000000-0000-0000-0000-000000000002")]
                   class B {}

                   """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics");
    }

    [TestMethod]
    public async Task DuplicateTypeGuids_ReportsDiagnostic()
    {
        var test = """
                   using System;

                   [AttributeUsage(AttributeTargets.Class)]
                   public class TypeGuidAttribute : Attribute
                   {
                       public TypeGuidAttribute(string guid) {}
                   }

                   [TypeGuid("00000000-0000-0000-0000-000000000001")]
                   class A {}
                   [TypeGuid("00000000-0000-0000-0000-000000000001")]
                   class B {}
                   """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(1, diagnostics.Length, "Expected exactly one diagnostic");
        Assert.AreEqual("GUID001", diagnostics[0].Id, "Expected GUID001 diagnostic");
    }

    [TestMethod]
    public async Task NoDuplicateMethodGuids_NoDiagnostic()
    {
        var test = """
                   using System;

                   [AttributeUsage(AttributeTargets.Method)]
                   public class MethodGuidAttribute : Attribute
                   {
                       public MethodGuidAttribute(string guid) {}
                   }

                   class A {
                       [MethodGuid("00000000-0000-0000-0000-000000000011")]
                       void M1() {}
                       [MethodGuid("00000000-0000-0000-0000-000000000012")]
                       void M2() {}
                   }
                   """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics");
    }

    [TestMethod]
    public async Task DuplicateMethodGuids_ReportsDiagnostic()
    {
        var test = """
                   using System;

                   [AttributeUsage(AttributeTargets.Method)]
                   public class MethodGuidAttribute : Attribute
                   {
                       public MethodGuidAttribute(string guid) {}
                   }

                   class A {
                       [MethodGuid("00000000-0000-0000-0000-000000000011")]
                       void M1() {}
                       [MethodGuid("00000000-0000-0000-0000-000000000011")]
                       void M2() {}
                   }
                   """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(1, diagnostics.Length, "Expected exactly one diagnostic");
        Assert.AreEqual("GUID002", diagnostics[0].Id, "Expected GUID002 diagnostic");
    }
}
