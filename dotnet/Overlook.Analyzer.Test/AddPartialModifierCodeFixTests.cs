using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Overlook.Analyzers.Test;

[TestClass]
public class AddPartialModifierCodeFixTests
{
    private static async Task<string> ApplyCodeFixAsync(string sourceCode)
    {
        var attributeSource = @"
#nullable enable
using System;

namespace Overlook.Ecs
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class QueryComponentAttribute : Attribute
    {
        public Type ComponentType { get; }
        public string? Name { get; set; }
        public bool IsOptional { get; set; }
        public bool IsReadOnly { get; set; }
        public bool QueryOnly { get; set; }

        public QueryComponentAttribute(Type componentType)
        {
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
        }
    }

    public readonly record struct Entity(int Id);
    public readonly record struct WorldEntity(World World, Entity Entity)
    {
        public ref T Get<T>() where T : unmanaged => throw new NotImplementedException();
        public T GetObject<T>() where T : class => throw new NotImplementedException()!;
        public bool Has<T>() => throw new NotImplementedException();
    }
    public class World { }
    public ref struct QueryBuilder
    {
        public QueryBuilder Has<T>() => this;
    }
    public readonly struct Query
    {
        public Enumerator GetEnumerator() => default;
        public struct Enumerator : System.IDisposable
        {
            public bool MoveNext() => false;
            public Entity Current => default;
            public void Dispose() { }
        }
    }
    public interface IQuery<TEnumerator, TElement> where TEnumerator : IQueryEnumerator<TElement>
    {
        TEnumerator GetEnumerator();
    }
    public interface IQueryEnumerator<TElement>
    {
        bool MoveNext();
        TElement Current { get; }
    }
}
";

        // Create compilation with source generator
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(SourceText.From(sourceCode)),
            CSharpSyntaxTree.ParseText(SourceText.From(attributeSource))
        };

        var compilation = CSharpCompilation.Create("TestAssembly",
            syntaxTrees,
            Microsoft.CodeAnalysis.Testing.ReferenceAssemblies.Net.Net60.ResolveAsync(null, default).Result,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run the source generator to get diagnostics
        var generator = new QueryableSourceGenerator();
        CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // Find OVL005 diagnostic
        var diagnostic = diagnostics.FirstOrDefault(d => d.Id == "OVL005");
        if (diagnostic == null)
            return sourceCode; // No fix needed

        // Apply code fix
        var codeFix = new AddPartialModifierCodeFix();
        var document = CreateDocument(sourceCode);
        var root = await document.GetSyntaxRootAsync();

        if (root == null)
            return sourceCode;

        // Find the type declaration
        var typeDeclaration = root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault(t => t.AttributeLists.Any());

        if (typeDeclaration == null)
            return sourceCode;

        // Create partial keyword with space
        var partialKeyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        // Add partial modifier
        var newModifiers = typeDeclaration.Modifiers.Add(partialKeyword);
        var newTypeDeclaration = typeDeclaration.WithModifiers(newModifiers);

        var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
        return newRoot.ToFullString();
    }

    private static Document CreateDocument(string sourceCode)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .AddDocument(documentId, "Test.cs", SourceText.From(sourceCode));

        return solution.GetDocument(documentId)!;
    }

    [TestMethod]
    public async Task AddsPartialModifierToRecordStruct()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Position { public float X; }

[QueryComponent(typeof(Position))]
public record struct NonPartialEntity;
";

        var fixedSource = await ApplyCodeFixAsync(source);

        Assert.IsTrue(fixedSource.Contains("public partial record struct NonPartialEntity"),
            $"Should add partial modifier. Result: {fixedSource}");
    }

    [TestMethod]
    public async Task AddsPartialModifierToStruct()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Position { public float X; }

[QueryComponent(typeof(Position))]
public struct NonPartialStruct;
";

        var fixedSource = await ApplyCodeFixAsync(source);

        Assert.IsTrue(fixedSource.Contains("public partial struct NonPartialStruct"),
            $"Should add partial modifier. Result: {fixedSource}");
    }

    [TestMethod]
    public async Task AddsPartialModifierToClass()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Position { public float X; }

[QueryComponent(typeof(Position))]
public class NonPartialClass { }
";

        var fixedSource = await ApplyCodeFixAsync(source);

        Assert.IsTrue(fixedSource.Contains("public partial class NonPartialClass"),
            $"Should add partial modifier. Result: {fixedSource}");
    }

    [TestMethod]
    public async Task PreservesOtherModifiers()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Position { public float X; }

[QueryComponent(typeof(Position))]
internal sealed class SealedClass { }
";

        var fixedSource = await ApplyCodeFixAsync(source);

        Assert.IsTrue(fixedSource.Contains("internal") && fixedSource.Contains("sealed") && fixedSource.Contains("partial"),
            $"Should preserve existing modifiers. Result: {fixedSource}");
    }
}
