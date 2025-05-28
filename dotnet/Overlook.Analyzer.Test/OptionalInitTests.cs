using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Overlook.Analyzers.Test;

[TestClass]
public class OptionalInitTests
{
    [TestMethod]
    public async Task RequiredProperty_NotInitialized_ReportsDiagnostic()
    {
        var test = @"
using System;
namespace System.Runtime.CompilerServices { public class IsExternalInit {} }
namespace Game {
    public class RequiredOnInitAttribute : Attribute {}
    public class OptionalOnInitAttribute : Attribute {}
}
struct S {
    [Game.RequiredOnInit]
    public int X { get; init; }
}
class C { void M() { var s = new S(); } }
";
        var expected = new DiagnosticResult("SG001", DiagnosticSeverity.Error)
            .WithSpan(12, 30, 12, 37);

        await new CSharpSourceGeneratorTest<OptionalInit, MSTestVerifier>
        {
            TestState = {
                Sources = { test },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60
            },
            ExpectedDiagnostics = { expected }
        }.RunAsync();
    }

    [TestMethod]
    public async Task RequiredProperty_Initialized_NoDiagnostic()
    {
        var test = @"
using System;
namespace System.Runtime.CompilerServices { public class IsExternalInit {} }
namespace Game {
    public class RequiredOnInitAttribute : Attribute {}
    public class OptionalOnInitAttribute : Attribute {}
}
struct S {
    [Game.RequiredOnInit]
    public int X { get; init; }
}
class C { void M() { var s = new S { X = 1 }; } }
";
        await new CSharpSourceGeneratorTest<OptionalInit, MSTestVerifier>
        {
            TestState = {
                Sources = { test },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60
            }
        }.RunAsync();
    }

    [TestMethod]
    public async Task OptionalProperty_NotInitialized_NoDiagnostic()
    {
        var test = @"
using System;
namespace System.Runtime.CompilerServices { public class IsExternalInit {} }
namespace Game {
    public class RequiredOnInitAttribute : Attribute {}
    public class OptionalOnInitAttribute : Attribute {}
}
struct S {
    [Game.OptionalOnInit]
    public int X { get; init; }
}
class C { void M() { var s = new S(); } }
";
        await new CSharpSourceGeneratorTest<OptionalInit, MSTestVerifier>
        {
            TestState = {
                Sources = { test },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60
            }
        }.RunAsync();
    }
}
