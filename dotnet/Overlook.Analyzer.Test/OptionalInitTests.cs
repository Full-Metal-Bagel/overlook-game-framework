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
public class OptionalInitTests
{
    private static async Task<Diagnostic[]> GetAnalyzerDiagnosticsAsync(string sourceCode)
    {
        var compilation = CSharpCompilation.Create("TestAssembly")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(ReferenceAssemblies.Net.Net60.ResolveAsync(null, default).Result)
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(sourceCode)));

        var analyzer = new OptionalInit();
        var compilationWithAnalyzers = compilation.WithAnalyzers(new DiagnosticAnalyzer[] { analyzer }.ToImmutableArray());
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        
        return diagnostics.Where(d => d.Id.StartsWith("SG")).ToArray();
    }

    [TestMethod]
    public async Task RequiredProperty_NotInitialized_ReportsDiagnostic()
    {
        var test = @"
using System;
namespace System.Runtime.CompilerServices { public class IsExternalInit {} }
namespace Overlook {
    public class RequiredOnInitAttribute : Attribute {}
    public class OptionalOnInitAttribute : Attribute {}
    struct S {
        [RequiredOnInit]
        public int X { get; init; }
    }
    class C { void M() { var s = new S(); } }
}
";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(1, diagnostics.Length, "Expected exactly one diagnostic");
        Assert.AreEqual("SG001", diagnostics[0].Id, "Expected SG001 diagnostic");
    }

    [TestMethod]
    public async Task RequiredProperty_Initialized_NoDiagnostic()
    {
        var test = @"
using System;
namespace System.Runtime.CompilerServices { public class IsExternalInit {} }
namespace Overlook {
    public class RequiredOnInitAttribute : Attribute {}
    public class OptionalOnInitAttribute : Attribute {}
}
struct S {
    [Overlook.RequiredOnInit]
    public int X { get; init; }
}
class C { void M() { var s = new S { X = 1 }; } }
";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics");
    }

    [TestMethod]
    public async Task OptionalProperty_NotInitialized_NoDiagnostic()
    {
        var test = @"
using System;
namespace System.Runtime.CompilerServices { public class IsExternalInit {} }
namespace Overlook {
    public class RequiredOnInitAttribute : Attribute {}
    public class OptionalOnInitAttribute : Attribute {}
}
struct S {
    [Overlook.OptionalOnInit]
    public int X { get; init; }
}
class C { void M() { var s = new S(); } }
";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics");
    }

    [TestMethod]
    public async Task RequiredProperty_InitializedWithDefault_NoDiagnostic()
    {
        var test = @"
using System;
namespace System.Runtime.CompilerServices { public class IsExternalInit {} }
namespace Overlook {
    public class RequiredOnInitAttribute : Attribute {}
    public class OptionalOnInitAttribute : Attribute {}
    struct S {
        [RequiredOnInit]
        public int X { get; init; }
    }
    class C { void M() { var s = new S { X = default }; } }
}
";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics when property is set to default value");
    }

    [TestMethod]
    public async Task RequiredProperty_InitializedWithZero_NoDiagnostic()
    {
        var test = @"
using System;
namespace System.Runtime.CompilerServices { public class IsExternalInit {} }
namespace Overlook {
    public class RequiredOnInitAttribute : Attribute {}
    public class OptionalOnInitAttribute : Attribute {}
    struct S {
        [RequiredOnInit]
        public int X { get; init; }
    }
    class C { void M() { var s = new S { X = 0 }; } }
}
";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(test);
        Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics when property is set to zero");
    }
}
