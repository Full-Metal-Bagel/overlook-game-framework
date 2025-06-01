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
        // Only validate diagnostic ID (GUID001) - location is required by framework but not important for test
        var expected = new DiagnosticResult("GUID001", DiagnosticSeverity.Error)
            .WithSpan(12, 7, 12, 8);
        
        await new CSharpAnalyzerTest<DuplicatedGuidAnalyzer, MSTestVerifier>
        {
            TestCode = test,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            CompilerDiagnostics = CompilerDiagnostics.Errors,
            ExpectedDiagnostics = { expected }
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
        // Only validate diagnostic ID (GUID002) - location is required by framework but not important for test
        var expected = new DiagnosticResult("GUID002", DiagnosticSeverity.Error)
            .WithSpan(13, 10, 13, 12);
        
        await new CSharpAnalyzerTest<DuplicatedGuidAnalyzer, MSTestVerifier>
        {
            TestCode = test,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            CompilerDiagnostics = CompilerDiagnostics.Errors,
            ExpectedDiagnostics = { expected }
        }.RunAsync();
    }
}
