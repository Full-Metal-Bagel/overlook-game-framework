using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Overlook.Analyzers.Test;

[TestClass]
public class DuplicatedGuidAnalyzerTests
{
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
        await new CSharpAnalyzerTest<DuplicatedGuidAnalyzer, MSTestVerifier>
        {
            TestCode = test,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();
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
        // Use diagnostic descriptors from the analyzer itself and specify the line/column based on test observation
        var expected = new DiagnosticResult(DiagnosticDescriptors.DuplicateTypeGuid.Id, DiagnosticSeverity.Error)
            .WithSpan(12, 7, 12, 8) // Line 11, column 7, class A declaration
            .WithArguments("A", "B", "00000000-0000-0000-0000-000000000001");

        await new CSharpAnalyzerTest<DuplicatedGuidAnalyzer, MSTestVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();
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
        await new CSharpAnalyzerTest<DuplicatedGuidAnalyzer, MSTestVerifier>
        {
            TestCode = test,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();
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
        // Use diagnostic descriptors from the analyzer itself and specify the line/column based on test observation
        var expected = new DiagnosticResult(DiagnosticDescriptors.DuplicateMethodGuid.Id, DiagnosticSeverity.Error)
            .WithSpan(13, 10, 13, 12) // Line 13, column 10, M2 method declaration
            .WithArguments("M2", "M1", "00000000-0000-0000-0000-000000000011");

        await new CSharpAnalyzerTest<DuplicatedGuidAnalyzer, MSTestVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();
    }
}
