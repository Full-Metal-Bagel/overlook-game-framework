using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Overlook.Analyzers.Test;

[TestClass]
public class DisallowDefaultConstructorTests
{
    [TestMethod]
    public async Task StructWithAttribute_ParameterlessInstantiation_ReportsDiagnostic()
    {
        var test = @"
using System;
namespace Game {
    public class DisallowDefaultConstructorAttribute : Attribute {}
}
[Game.DisallowDefaultConstructor]
struct S {}
class C { void M() { var s = new S(); } }
";
        var expected = new DiagnosticResult("STRUCT001", DiagnosticSeverity.Error)
            .WithSpan(8, 30, 8, 37)
            .WithArguments("S");

        await new CSharpSourceGeneratorTest<DisallowDefaultConstructor, MSTestVerifier>
        {
            TestState = {
                Sources = { test },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60
            },
            ExpectedDiagnostics = { expected }
        }.RunAsync();
    }

    [TestMethod]
    public async Task StructWithAttribute_WithParameters_NoDiagnostic()
    {
        var test = @"
using System;
namespace Game {
    public class DisallowDefaultConstructorAttribute : Attribute {}
}
[Game.DisallowDefaultConstructor]
struct S { public S(int x) { } }
class C { void M() { var s = new S(1); } }
";
        await new CSharpSourceGeneratorTest<DisallowDefaultConstructor, MSTestVerifier>
        {
            TestState = {
                Sources = { test },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60
            }
        }.RunAsync();
    }

    [TestMethod]
    public async Task StructWithoutAttribute_ParameterlessInstantiation_NoDiagnostic()
    {
        var test = @"
struct S {}
class C { void M() { var s = new S(); } }
";
        await new CSharpSourceGeneratorTest<DisallowDefaultConstructor, MSTestVerifier>
        {
            TestState = {
                Sources = { test },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60
            }
        }.RunAsync();
    }
}
