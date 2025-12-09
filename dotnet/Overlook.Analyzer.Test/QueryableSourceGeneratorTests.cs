using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Overlook.Analyzers.Test;

[TestClass]
public class QueryableSourceGeneratorTests
{
    private static (string GeneratedCode, Diagnostic[] Diagnostics) RunGenerator(string sourceCode)
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

        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(SourceText.From(sourceCode)),
            CSharpSyntaxTree.ParseText(SourceText.From(attributeSource))
        };

        var compilation = CSharpCompilation.Create("TestAssembly",
            syntaxTrees,
            ReferenceAssemblies.Net.Net60.ResolveAsync(null, default).Result,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new QueryableSourceGenerator();
        CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees.FirstOrDefault()?.GetText().ToString() ?? "";

        return (generatedCode, diagnostics.ToArray());
    }

    [TestMethod]
    public void GeneratesCodeForTypeWithQueryComponents()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Position { public float X; public float Y; }
public struct Velocity { public float X; public float Y; }

[QueryComponent(typeof(Position))]
[QueryComponent(typeof(Velocity))]
public partial record struct MovableEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error),
            $"Expected no errors but got: {string.Join(", ", diagnostics.Select(d => d.GetMessage()))}");

        // Verify generated code structure
        Assert.IsTrue(generatedCode.Contains("public partial record struct MovableEntity(global::Overlook.Ecs.WorldEntity Entity)"),
            "Should generate partial record struct with WorldEntity parameter");

        // Verify properties are generated
        Assert.IsTrue(generatedCode.Contains("public ref global::TestNamespace.Position Position => ref Entity.Get<global::TestNamespace.Position>()"),
            "Should generate Position property with ref return");
        Assert.IsTrue(generatedCode.Contains("public ref global::TestNamespace.Velocity Velocity => ref Entity.Get<global::TestNamespace.Velocity>()"),
            "Should generate Velocity property with ref return");

        // Verify operators
        Assert.IsTrue(generatedCode.Contains("public static implicit operator global::Overlook.Ecs.Entity(MovableEntity entity)"),
            "Should generate implicit operator to Entity");
        Assert.IsTrue(generatedCode.Contains("public static implicit operator global::Overlook.Ecs.WorldEntity(MovableEntity entity)"),
            "Should generate implicit operator to WorldEntity");

        // Verify ReadOnly struct
        Assert.IsTrue(generatedCode.Contains("public readonly record struct ReadOnly(MovableEntity Entity)"),
            "Should generate ReadOnly nested struct");

        // Verify Query struct
        Assert.IsTrue(generatedCode.Contains("public readonly record struct Query(global::Overlook.Ecs.Query EcsQuery, global::Overlook.Ecs.World World)"),
            "Should generate Query nested struct");

        // Verify extension methods
        Assert.IsTrue(generatedCode.Contains("public static MovableEntity AsMovableEntity(this global::Overlook.Ecs.WorldEntity entity)"),
            "Should generate AsMovableEntity extension method");
        Assert.IsTrue(generatedCode.Contains("public static bool IsMovableEntity(this global::Overlook.Ecs.WorldEntity entity)"),
            "Should generate IsMovableEntity extension method");
        Assert.IsTrue(generatedCode.Contains("public static global::Overlook.Ecs.QueryBuilder HasMovableEntity(this global::Overlook.Ecs.QueryBuilder builder)"),
            "Should generate HasMovableEntity extension method");
    }

    [TestMethod]
    public void DoesNotGenerateForTypeWithoutQueryComponents()
    {
        const string source = @"
namespace TestNamespace;

public partial record struct EmptyEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify no generated code
        Assert.IsTrue(string.IsNullOrEmpty(generatedCode),
            "Should not generate code for types without QueryComponent attributes");
    }

    [TestMethod]
    public void GeneratesOptionalComponentHandling()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Health { public float Value; }

[QueryComponent(typeof(Health), IsOptional = true)]
public partial record struct OptionalHealthEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error),
            $"Expected no errors but got: {string.Join(", ", diagnostics.Select(d => d.GetMessage()))}");

        // Verify Has property
        Assert.IsTrue(generatedCode.Contains("public bool HasHealth => Entity.Has<global::TestNamespace.Health>()"),
            "Should generate HasHealth property for optional component");

        // Verify TryGet method
        Assert.IsTrue(generatedCode.Contains("public global::TestNamespace.Health TryGetHealth(global::TestNamespace.Health defaultValue = default)"),
            "Should generate TryGetHealth method for optional component");

        // Verify TrySet method
        Assert.IsTrue(generatedCode.Contains("public bool TrySetHealth(global::TestNamespace.Health value)"),
            "Should generate TrySetHealth method for optional component");

        // Should NOT generate regular property getter for optional
        Assert.IsFalse(generatedCode.Contains("public ref global::TestNamespace.Health Health =>"),
            "Should NOT generate regular property for optional component");
    }

    [TestMethod]
    public void GeneratesReadOnlyProperties()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Config { public int Value; }

[QueryComponent(typeof(Config), IsReadOnly = true)]
public partial record struct ReadOnlyConfigEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify read-only property (no ref, expression body)
        Assert.IsTrue(generatedCode.Contains("public global::TestNamespace.Config Config => Entity.Get<global::TestNamespace.Config>()"),
            "Should generate read-only property without ref for IsReadOnly component");
    }

    [TestMethod]
    public void GeneratesQueryOnlyComponents()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct TagPlayer { }
public struct Health { public float Value; }

[QueryComponent(typeof(TagPlayer), QueryOnly = true)]
[QueryComponent(typeof(Health))]
public partial record struct PlayerEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify QueryOnly component does NOT generate property
        Assert.IsFalse(generatedCode.Contains("public global::TestNamespace.TagPlayer TagPlayer"),
            "Should NOT generate property for QueryOnly component");

        // Verify regular component does generate property
        Assert.IsTrue(generatedCode.Contains("public ref global::TestNamespace.Health Health"),
            "Should generate property for non-QueryOnly component");

        // Verify QueryOnly component is still in query builder
        Assert.IsTrue(generatedCode.Contains(".Has<global::TestNamespace.TagPlayer>()"),
            "Should include QueryOnly component in HasPlayerEntity query builder");

        // Verify IsPlayerEntity includes QueryOnly component
        Assert.IsTrue(generatedCode.Contains("entity.Has<global::TestNamespace.TagPlayer>()"),
            "Should include QueryOnly component in IsPlayerEntity check");
    }

    [TestMethod]
    public void GeneratesCustomPropertyNames()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct HealthComponent { public float Value; }

[QueryComponent(typeof(HealthComponent), Name = ""HP"")]
public partial record struct CustomNameEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify custom property name
        Assert.IsTrue(generatedCode.Contains("public ref global::TestNamespace.HealthComponent HP =>"),
            "Should use custom property name 'HP'");

        // Verify extension methods still use type name
        Assert.IsTrue(generatedCode.Contains("AsCustomNameEntity"),
            "Extension method should use type name");
    }

    [TestMethod]
    public void GeneratesReferenceTypeComponentHandling()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public class Transform { public float X; public float Y; }

[QueryComponent(typeof(Transform))]
public partial record struct TransformEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify reference type uses GetObject and read-only property (no ref)
        Assert.IsTrue(generatedCode.Contains("public global::TestNamespace.Transform Transform => Entity.GetObject<global::TestNamespace.Transform>()"),
            "Should generate read-only property using GetObject for reference type");
    }

    [TestMethod]
    public void GeneratesQueryBuilderExtensionWithMultipleComponents()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Position { public float X; public float Y; }
public struct Velocity { public float X; public float Y; }
public struct Health { public float Value; }

[QueryComponent(typeof(Position))]
[QueryComponent(typeof(Velocity))]
[QueryComponent(typeof(Health))]
public partial record struct ComplexEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify query builder includes all components
        Assert.IsTrue(generatedCode.Contains(".Has<global::TestNamespace.Position>()"),
            "Should include Position in query builder");
        Assert.IsTrue(generatedCode.Contains(".Has<global::TestNamespace.Velocity>()"),
            "Should include Velocity in query builder");
        Assert.IsTrue(generatedCode.Contains(".Has<global::TestNamespace.Health>()"),
            "Should include Health in query builder");

        // Verify IsComplexEntity checks all components
        Assert.IsTrue(generatedCode.Contains("entity.Has<global::TestNamespace.Position>() && entity.Has<global::TestNamespace.Velocity>() && entity.Has<global::TestNamespace.Health>()"),
            "IsComplexEntity should check all required components");
    }

    [TestMethod]
    public void GeneratesReadOnlyQueryStruct()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Position { public float X; }

[QueryComponent(typeof(Position))]
public partial record struct SimpleEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify ReadOnlyQuery struct is generated
        Assert.IsTrue(generatedCode.Contains("public readonly record struct ReadOnlyQuery"),
            "Should generate ReadOnlyQuery nested struct");

        // Verify BuildAsSimpleEntityReadOnly extension method
        Assert.IsTrue(generatedCode.Contains("public static SimpleEntity.ReadOnlyQuery BuildAsSimpleEntityReadOnly"),
            "Should generate BuildAsSimpleEntityReadOnly extension method");

        // Verify AsSimpleEntityReadOnly extension method
        Assert.IsTrue(generatedCode.Contains("public static SimpleEntity.ReadOnly AsSimpleEntityReadOnly"),
            "Should generate AsSimpleEntityReadOnly extension method");
    }

    [TestMethod]
    public void GeneratesToStringOverride()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Position { public float X; }

[QueryComponent(typeof(Position))]
public partial record struct TestEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify ToString is generated in outer type with capacity estimation
        Assert.IsTrue(generatedCode.Contains("public override string ToString()"),
            "Should generate ToString method in outer type");
        Assert.IsTrue(generatedCode.Contains("new global::System.Text.StringBuilder("),
            "Should use StringBuilder with capacity");

        // Verify format includes entity ID first
        Assert.IsTrue(generatedCode.Contains("builder.Append(\"TestEntity { Id = \")"),
            "Should start with type name and Id");
        Assert.IsTrue(generatedCode.Contains("builder.Append(Entity.Entity.Identity)"),
            "Should append entity identity");

        // Verify component is printed with comma separator after Id
        Assert.IsTrue(generatedCode.Contains("builder.Append(\", Position = \")"),
            "Should print Position component with comma separator");
        Assert.IsTrue(generatedCode.Contains("builder.Append(Position)"),
            "Should append Position value");
    }

    [TestMethod]
    public void GeneratesToStringWithOptionalMarker()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Position { public float X; }
public struct Health { public float Value; }

[QueryComponent(typeof(Position))]
[QueryComponent(typeof(Health), IsOptional = true)]
public partial record struct TestEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify required component doesn't have ? marker (comes after Id with comma)
        Assert.IsTrue(generatedCode.Contains("builder.Append(\", Position = \")"),
            "Required component should not have ? marker");

        // Verify optional component has ? marker
        Assert.IsTrue(generatedCode.Contains("builder.Append(\", Health? = \")"),
            "Optional component should have ? marker");

        // Verify optional component prints <none> or value
        Assert.IsTrue(generatedCode.Contains("builder.Append(HasHealth ? TryGetHealth() : \"<none>\")"),
            "Optional component should print <none> when missing");
    }

    [TestMethod]
    public void DoesNotGenerateToStringWhenTypeAlreadyHasOne()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Position { public float X; }

[QueryComponent(typeof(Position))]
public partial record struct TestEntity
{
    public override string ToString() => ""Custom ToString"";
}
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error),
            $"Expected no errors but got: {string.Join(", ", diagnostics.Select(d => d.GetMessage()))}");

        // Verify ToString is NOT generated in outer type (only one occurrence should exist - in ReadOnly)
        var toStringCount = System.Text.RegularExpressions.Regex.Matches(generatedCode, @"public override string ToString\(\)").Count;
        Assert.AreEqual(1, toStringCount,
            "Should only have one ToString (in ReadOnly struct), not generate one for outer type");

        // Verify ReadOnly struct falls back to string manipulation since outer type has custom ToString
        Assert.IsTrue(generatedCode.Contains("Entity.ToString().Insert(nameof(TestEntity).Length, \".ReadOnly\")"),
            "ReadOnly struct should use string manipulation when outer type has custom ToString");
    }

    [TestMethod]
    public void GeneratesProperToStringForReadOnlyStruct()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct Position { public float X; }
public struct Velocity { public float Y; }

[QueryComponent(typeof(Position))]
[QueryComponent(typeof(Velocity))]
public partial record struct TestEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify ReadOnly struct has proper ToString with its own format
        Assert.IsTrue(generatedCode.Contains("builder.Append(\"TestEntity.ReadOnly { Id = \")"),
            "ReadOnly struct should have proper ToString with its own type name");

        // Verify ReadOnly struct accesses entity through Entity.Entity path
        Assert.IsTrue(generatedCode.Contains("builder.Append(Entity.Entity.Entity.Identity)"),
            "ReadOnly struct should access identity through Entity.Entity.Entity.Identity path");
    }

    [TestMethod]
    public void ToStringCapacityEstimation()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct A { public int Value; }
public struct B { public int Value; }
public struct C { public int Value; }

[QueryComponent(typeof(A))]
[QueryComponent(typeof(B))]
[QueryComponent(typeof(C))]
public partial record struct MultiComponentEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify capacity is calculated based on component count
        // Formula: displayName.Length + 15 + (componentCount * 45) + 3
        // "MultiComponentEntity" = 20 chars, 3 components
        // Expected: 20 + 15 + (3 * 45) + 3 = 173
        Assert.IsTrue(generatedCode.Contains("new global::System.Text.StringBuilder(173)"),
            "Should calculate StringBuilder capacity based on type name and component count");
    }

    [TestMethod]
    public void ToStringExcludesQueryOnlyComponents()
    {
        const string source = @"
using Overlook.Ecs;

namespace TestNamespace;

public struct TagPlayer { }
public struct Health { public float Value; }

[QueryComponent(typeof(TagPlayer), QueryOnly = true)]
[QueryComponent(typeof(Health))]
public partial record struct PlayerEntity;
";

        var (generatedCode, diagnostics) = RunGenerator(source);

        // Verify no errors
        Assert.AreEqual(0, diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));

        // Verify QueryOnly component is NOT in ToString
        Assert.IsFalse(generatedCode.Contains("TagPlayer = "),
            "QueryOnly component should NOT appear in ToString");

        // Verify regular component IS in ToString
        Assert.IsTrue(generatedCode.Contains("Health = "),
            "Regular component should appear in ToString");
    }
}
