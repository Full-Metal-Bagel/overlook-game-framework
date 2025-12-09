#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Overlook.Analyzers;

/// <summary>
/// Source generator that creates strongly-typed entity wrappers for the ECS system.
/// It generates properties, query builders, validation methods, and read-only views
/// from declarative [QueryComponent] attributes.
/// </summary>
[SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
[Generator]
public class QueryableSourceGenerator : IIncrementalGenerator
{
    private sealed class ComponentInfo
    {
        public ITypeSymbol? ComponentType { get; set; }
        public string ComponentTypeName { get; set; } = "";
        public string ComponentTypeFullName { get; set; } = "";
        public string? PropertyName { get; set; }
        public bool IsOptional { get; set; }
        public bool IsReadOnly { get; set; }
        public bool QueryOnly { get; set; }
        public bool IsReferenceType { get; set; }
    }

    private sealed class TypeWithComponents
    {
        public INamedTypeSymbol TypeSymbol { get; set; } = null!;
        public List<ComponentInfo> Components { get; set; } = new();
        public List<ComponentInfo> RequiredComponents { get; set; } = new();
        public List<PureMemberInfo> PureMembers { get; set; } = new();
        public TypeDeclarationSyntax TypeDeclaration { get; set; } = null!;
    }

    private enum PureMemberKind
    {
        Property,
        Method
    }

    private sealed class PureMemberInfo
    {
        public PureMemberKind Kind { get; set; }
        public SyntaxNode Syntax { get; set; } = null!;
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Create a syntax provider that finds all types with at least one attribute
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateType(node),
                transform: static (ctx, _) => GetTypeWithComponents(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        // Generate source code for each type with QueryComponent attributes
        context.RegisterSourceOutput(typeDeclarations,
            static (ctx, typeInfo) => Execute(ctx, typeInfo));
    }

    private static bool IsCandidateType(SyntaxNode node)
    {
        // Look for record structs, structs, or classes with attributes
        return node is TypeDeclarationSyntax typeDecl &&
               typeDecl.AttributeLists.Count > 0 &&
               (typeDecl is RecordDeclarationSyntax ||
                typeDecl is StructDeclarationSyntax ||
                typeDecl is ClassDeclarationSyntax);
    }

    private static TypeWithComponents? GetTypeWithComponents(GeneratorSyntaxContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);

        if (typeSymbol == null)
            return null;

        // Parse QueryComponent attributes
        var components = ParseQueryComponents(typeSymbol);

        if (components.Count == 0)
            return null;

        // Extract Pure properties and methods
        var pureMembers = ExtractPureMembers(typeDeclaration, context.SemanticModel);

        // Compute required components (non-optional)
        var requiredComponents = components.Where(c => !c.IsOptional).ToList();

        return new TypeWithComponents
        {
            TypeSymbol = typeSymbol,
            Components = components,
            TypeDeclaration = typeDeclaration,
            PureMembers = pureMembers,
            RequiredComponents = requiredComponents
        };
    }

    private static List<ComponentInfo> ParseQueryComponents(INamedTypeSymbol typeSymbol)
    {
        var components = new List<ComponentInfo>();

        foreach (var attribute in typeSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != "Overlook.Ecs.QueryComponentAttribute")
                continue;

            ITypeSymbol? componentType = null;
            string componentTypeName = "";
            string componentTypeFullName = "";
            string? propertyName = null;
            bool isOptional = false;
            bool isReadOnly = false;
            bool queryOnly = false;

            // Get ComponentType (first constructor argument)
            if (attribute.ConstructorArguments.Length > 0)
            {
                var typeArg = attribute.ConstructorArguments[0];
                if (typeArg.Kind == TypedConstantKind.Type && typeArg.Value is ITypeSymbol ct)
                {
                    componentType = ct;
                    componentTypeName = GetTypeName(ct);
                    componentTypeFullName = "global::" + ct.ToDisplayString();
                }
            }

            // Parse named arguments
            foreach (var namedArg in attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "Name":
                        propertyName = namedArg.Value.Value?.ToString();
                        break;
                    case "IsOptional":
                        isOptional = (bool)(namedArg.Value.Value ?? false);
                        break;
                    case "IsReadOnly":
                        isReadOnly = (bool)(namedArg.Value.Value ?? false);
                        break;
                    case "QueryOnly":
                        queryOnly = (bool)(namedArg.Value.Value ?? false);
                        break;
                }
            }

            // Default property name to component type name if not specified
            if (string.IsNullOrEmpty(propertyName))
                propertyName = componentTypeName;

            // Skip if we couldn't determine the component type name
            if (!string.IsNullOrEmpty(componentTypeName))
            {
                components.Add(new ComponentInfo
                {
                    ComponentType = componentType,
                    ComponentTypeName = componentTypeName,
                    ComponentTypeFullName = componentTypeFullName,
                    PropertyName = propertyName,
                    IsOptional = isOptional,
                    IsReadOnly = isReadOnly,
                    QueryOnly = queryOnly,
                    IsReferenceType = componentType?.IsReferenceType ?? false
                });
            }
        }

        return components;
    }

    private static string GetTypeName(ITypeSymbol typeSymbol)
    {
        // Handle error types (unresolved types)
        if (typeSymbol is IErrorTypeSymbol errorType)
        {
            var displayString = errorType.ToDisplayString();
            var lastDot = displayString.LastIndexOf('.');
            return lastDot >= 0 ? displayString.Substring(lastDot + 1) : displayString;
        }

        return typeSymbol.Name;
    }

    private static List<PureMemberInfo> ExtractPureMembers(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
    {
        var pureMembers = new List<PureMemberInfo>();

        foreach (var member in typeDeclaration.Members)
        {
            // Extract Pure properties
            if (member is PropertyDeclarationSyntax propertySyntax)
            {
                if (propertySyntax.AccessorList != null)
                {
                    foreach (var accessor in propertySyntax.AccessorList.Accessors)
                    {
                        if (accessor.Kind() == SyntaxKind.GetAccessorDeclaration)
                        {
                            if (HasPureAttribute(accessor.AttributeLists))
                            {
                                var typeInfo = semanticModel.GetTypeInfo(propertySyntax.Type);
                                var typeString = typeInfo.Type != null
                                    ? "global::" + typeInfo.Type.ToDisplayString()
                                    : propertySyntax.Type.ToString();

                                pureMembers.Add(new PureMemberInfo
                                {
                                    Kind = PureMemberKind.Property,
                                    Syntax = propertySyntax,
                                    Name = propertySyntax.Identifier.Text,
                                    Type = typeString
                                });
                                break;
                            }
                        }
                    }
                }
            }
            // Extract Pure methods
            else if (member is MethodDeclarationSyntax methodSyntax)
            {
                if (HasPureAttribute(methodSyntax.AttributeLists))
                {
                    var typeInfo = semanticModel.GetTypeInfo(methodSyntax.ReturnType);
                    var typeString = typeInfo.Type != null
                        ? "global::" + typeInfo.Type.ToDisplayString()
                        : methodSyntax.ReturnType.ToString();

                    pureMembers.Add(new PureMemberInfo
                    {
                        Kind = PureMemberKind.Method,
                        Syntax = methodSyntax,
                        Name = methodSyntax.Identifier.Text,
                        Type = typeString
                    });
                }
            }
        }

        return pureMembers;
    }

    private static bool HasPureAttribute(SyntaxList<AttributeListSyntax> attributeLists)
    {
        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name is "Pure" or "PureAttribute")
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static void Execute(SourceProductionContext context, TypeWithComponents typeInfo)
    {
        var source = GenerateQueryableEntity(typeInfo.TypeSymbol, typeInfo.Components, typeInfo.PureMembers, typeInfo.RequiredComponents);

        if (!string.IsNullOrEmpty(source))
        {
            var fileName = $"{typeInfo.TypeSymbol.Name}.g.cs";
            context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateQueryableEntity(INamedTypeSymbol typeSymbol, List<ComponentInfo> components, List<PureMemberInfo> pureMembers, List<ComponentInfo> requiredComponents)
    {
        var sb = new StringBuilder();
        var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
        var typeName = typeSymbol.Name;

        // Generate header
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Generate main partial record struct with WorldEntity parameter
        sb.AppendLine($"public partial record struct {typeName}(global::Overlook.Ecs.WorldEntity Entity)");
        sb.AppendLine("{");

        // Generate properties
        GenerateProperties(sb, components);

        // Generate implicit/explicit operators
        sb.AppendLine();
        sb.AppendLine($"    public static implicit operator global::Overlook.Ecs.Entity({typeName} entity) => entity.Entity.Entity;");
        sb.AppendLine($"    public static implicit operator global::Overlook.Ecs.WorldEntity({typeName} entity) => entity.Entity;");
        sb.AppendLine($"    public static explicit operator {typeName}(global::Overlook.Ecs.WorldEntity entity) => entity.As{typeName}();");
        sb.AppendLine($"    public static implicit operator {typeName}.ReadOnly({typeName} entity) => new(entity);");
        sb.AppendLine();

        // Generate AsReadOnly property
        sb.AppendLine("    public ReadOnly AsReadOnly => new(this);");
        sb.AppendLine();

        // Generate ToString override for the outer type
        GenerateToString(sb, typeName, components);

        // Generate ReadOnly nested struct
        GenerateReadOnlyStruct(sb, typeName, components, pureMembers);

        // Generate Query nested struct
        GenerateQueryStruct(sb, typeName, isReadOnly: false);

        // Generate ReadOnlyQuery nested struct
        GenerateQueryStruct(sb, typeName, isReadOnly: true);

        sb.AppendLine("}");
        sb.AppendLine();

        // Generate extension methods
        GenerateExtensionMethods(sb, namespaceName, typeName, requiredComponents);

        return sb.ToString();
    }

    private static string GetGetterCall(ComponentInfo component, string entityExpression)
    {
        if (component.IsReferenceType)
        {
            return $"{entityExpression}.GetObject<{component.ComponentTypeFullName}>()";
        }
        return $"{entityExpression}.Get<{component.ComponentTypeFullName}>()";
    }

    private static void GenerateProperties(StringBuilder sb, List<ComponentInfo> components)
    {
        foreach (var component in components)
        {
            // Don't generate properties for QueryOnly components
            if (component.QueryOnly) continue;

            if (component.IsOptional)
            {
                // For optional components, generate Has property and TryGet/TrySet methods
                sb.AppendLine($"    public bool Has{component.PropertyName} => Entity.Has<{component.ComponentTypeFullName}>();");
                sb.AppendLine();

                // TryGet method
                sb.AppendLine($"    public {component.ComponentTypeFullName} TryGet{component.PropertyName}({component.ComponentTypeFullName} defaultValue = default)");
                sb.AppendLine("    {");
                sb.AppendLine($"        return Entity.Has<{component.ComponentTypeFullName}>() ? {GetGetterCall(component, "Entity")} : defaultValue;");
                sb.AppendLine("    }");

                // TrySet method (only for non-readonly)
                if (!component.IsReadOnly)
                {
                    sb.AppendLine();
                    sb.AppendLine($"    public bool TrySet{component.PropertyName}({component.ComponentTypeFullName} value)");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        if (Entity.Has<{component.ComponentTypeFullName}>())");
                    sb.AppendLine("        {");
                    if (component.IsReferenceType)
                    {
                        sb.AppendLine($"            Entity.AddObject(value);");
                    }
                    else
                    {
                        sb.AppendLine($"            Entity.Get<{component.ComponentTypeFullName}>() = value;");
                    }
                    sb.AppendLine("            return true;");
                    sb.AppendLine("        }");
                    sb.AppendLine("        return false;");
                    sb.AppendLine("    }");
                }
            }
            else
            {
                // For required components, use regular properties
                if (component.IsReadOnly || component.IsReferenceType)
                {
                    // Read-only property (expression body)
                    sb.AppendLine($"    public {component.ComponentTypeFullName} {component.PropertyName} => {GetGetterCall(component, "Entity")};");
                }
                else
                {
                    // Read-write property for unmanaged types
                    sb.AppendLine($"    public ref {component.ComponentTypeFullName} {component.PropertyName} => ref Entity.Get<{component.ComponentTypeFullName}>();");
                }
            }
            sb.AppendLine();
        }
    }

    private static void GenerateReadOnlyStruct(StringBuilder sb, string typeName, List<ComponentInfo> components, List<PureMemberInfo> pureMembers)
    {
        sb.AppendLine($"    public readonly record struct ReadOnly({typeName} Entity)");
        sb.AppendLine("    {");

        foreach (var component in components)
        {
            // Don't generate properties for QueryOnly components
            if (component.QueryOnly) continue;

            if (component.IsOptional)
            {
                // For optional components, generate Has property and TryGet method
                sb.AppendLine($"        public bool Has{component.PropertyName} => Entity.Has{component.PropertyName};");
                sb.AppendLine();
                sb.AppendLine($"        public {component.ComponentTypeFullName} TryGet{component.PropertyName}({component.ComponentTypeFullName} defaultValue = default) => Entity.TryGet{component.PropertyName}(defaultValue);");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"        public {component.ComponentTypeFullName} {component.PropertyName} => Entity.{component.PropertyName};");
                sb.AppendLine();
            }
        }

        // Generate Pure properties and methods
        GeneratePureMembers(sb, pureMembers);

        sb.AppendLine("        public static implicit operator global::Overlook.Ecs.Entity(ReadOnly entity) => entity.Entity;");
        sb.AppendLine("        public static implicit operator global::Overlook.Ecs.WorldEntity(ReadOnly entity) => entity.Entity;");
        sb.AppendLine();

        // Override ToString to use proper type name
        sb.AppendLine($"        public override string ToString() => Entity.ToString().Insert(nameof({typeName}).Length, \".ReadOnly\");");

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GeneratePureMembers(StringBuilder sb, List<PureMemberInfo> pureMembers)
    {
        foreach (var pureMember in pureMembers)
        {
            if (pureMember.Kind == PureMemberKind.Property)
            {
                sb.AppendLine($"        public {pureMember.Type} {pureMember.Name} => Entity.{pureMember.Name};");
                sb.AppendLine();
            }
            else if (pureMember.Kind == PureMemberKind.Method)
            {
                var methodSyntax = (MethodDeclarationSyntax)pureMember.Syntax;
                var parameters = methodSyntax.ParameterList.ToString();
                var parameterNames = string.Join(", ", methodSyntax.ParameterList.Parameters.Select(p => p.Identifier.Text));

                sb.AppendLine($"        public {pureMember.Type} {pureMember.Name}{parameters} => Entity.{pureMember.Name}({parameterNames});");
                sb.AppendLine();
            }
        }
    }

    private static void GenerateToString(StringBuilder sb, string typeName, List<ComponentInfo> components)
    {
        sb.AppendLine("    public override string ToString()");
        sb.AppendLine("    {");
        sb.AppendLine($"        var builder = new global::System.Text.StringBuilder(\"{typeName} {{ \");");

        var printableComponents = components.Where(c => !c.QueryOnly).ToList();
        for (int i = 0; i < printableComponents.Count; i++)
        {
            var component = printableComponents[i];
            var propertyName = component.PropertyName;
            var optionalMarker = component.IsOptional ? "?" : "";
            var separator = i > 0 ? ", " : "";

            sb.AppendLine($"        builder.Append(\"{separator}{propertyName}{optionalMarker} = \");");

            if (component.IsOptional)
            {
                sb.AppendLine($"        builder.Append(Has{propertyName} ? TryGet{propertyName}() : \"<none>\");");
            }
            else
            {
                sb.AppendLine($"        builder.Append({propertyName});");
            }
        }

        sb.AppendLine("        builder.Append(\" }\");");
        sb.AppendLine("        return builder.ToString();");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateQueryStruct(StringBuilder sb, string typeName, bool isReadOnly)
    {
        var queryStructName = isReadOnly ? "ReadOnlyQuery" : "Query";
        var elementType = isReadOnly ? $"{typeName}.ReadOnly" : typeName;
        var conversionMethod = isReadOnly ? $"As{typeName}ReadOnly" : $"As{typeName}";

        if (isReadOnly)
            sb.AppendLine();

        sb.AppendLine($"    public readonly record struct {queryStructName}(global::Overlook.Ecs.Query EcsQuery, global::Overlook.Ecs.World World) : global::Overlook.Ecs.IQuery<{queryStructName}.Enumerator, {elementType}>");
        sb.AppendLine("    {");
        sb.AppendLine("        public Enumerator GetEnumerator() => new(EcsQuery.GetEnumerator(), World);");
        sb.AppendLine();
        sb.AppendLine($"        public struct Enumerator : global::Overlook.Ecs.IQueryEnumerator<{elementType}>, global::System.Collections.Generic.IEnumerator<{elementType}>");
        sb.AppendLine("        {");
        sb.AppendLine("            private global::Overlook.Ecs.Query.Enumerator _enumerator;");
        sb.AppendLine("            private readonly global::Overlook.Ecs.World _world;");
        sb.AppendLine();
        sb.AppendLine("            public Enumerator(global::Overlook.Ecs.Query.Enumerator enumerator, global::Overlook.Ecs.World world)");
        sb.AppendLine("            {");
        sb.AppendLine("                _enumerator = enumerator;");
        sb.AppendLine("                _world = world;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            public bool MoveNext() => _enumerator.MoveNext();");
        sb.AppendLine($"            public {elementType} Current => new global::Overlook.Ecs.WorldEntity(_world, _enumerator.Current).{conversionMethod}();");
        sb.AppendLine("            public void Reset() { }");
        sb.AppendLine("            object global::System.Collections.IEnumerator.Current => Current;");
        sb.AppendLine("            public void Dispose() => _enumerator.Dispose();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void GenerateDebugAssert(StringBuilder sb, string typeName, List<ComponentInfo> requiredComponents, string indent)
    {
        if (requiredComponents.Count == 0)
            return;

        sb.AppendLine($"{indent}global::System.Diagnostics.Debug.Assert(");
        sb.AppendLine($"{indent}    entity.Is{typeName}(),");
        sb.Append($"{indent}    $\"Entity {{entity}} is not a valid {typeName}. Missing components: \"");

        for (int i = 0; i < requiredComponents.Count; i++)
        {
            sb.AppendLine();
            sb.Append($"{indent}        + $\"{{(!entity.Has<{requiredComponents[i].ComponentTypeFullName}>() ? \"{requiredComponents[i].ComponentTypeName} \" : \"\")}}\"");
        }

        sb.AppendLine();
        sb.AppendLine($"{indent});");
    }

    private static void GenerateExtensionMethods(StringBuilder sb, string namespaceName, string typeName, List<ComponentInfo> requiredComponents)
    {
        sb.AppendLine($"public static partial class {typeName}Extensions");
        sb.AppendLine("{");

        // Generate As{TypeName} extension method for WorldEntity
        sb.AppendLine($"    public static {typeName} As{typeName}(this global::Overlook.Ecs.WorldEntity entity)");
        sb.AppendLine("    {");
        GenerateDebugAssert(sb, typeName, requiredComponents, "        ");
        sb.AppendLine($"        return new {typeName}(entity);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate Has{TypeName} query builder extension
        sb.AppendLine($"    public static global::Overlook.Ecs.QueryBuilder Has{typeName}(this global::Overlook.Ecs.QueryBuilder builder)");
        sb.AppendLine("    {");
        sb.AppendLine("        return builder");

        for (int i = 0; i < requiredComponents.Count; i++)
        {
            var component = requiredComponents[i];
            sb.AppendLine($"            .Has<{component.ComponentTypeFullName}>()");
        }

        sb.AppendLine("            ;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate BuildAs{TypeName} extension method
        sb.AppendLine($"    public static {typeName}.Query BuildAs{typeName}(this global::Overlook.Ecs.QueryBuilder builder, global::Overlook.Ecs.World world)");
        sb.AppendLine("    {");
        sb.AppendLine($"        builder = builder.Has{typeName}();");
        sb.AppendLine("        var query = builder.Build(world);");
        sb.AppendLine($"        return new {typeName}.Query(query, world);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate As{TypeName}ReadOnly extension method
        sb.AppendLine($"    public static {typeName}.ReadOnly As{typeName}ReadOnly(this global::Overlook.Ecs.WorldEntity entity)");
        sb.AppendLine("    {");
        GenerateDebugAssert(sb, typeName, requiredComponents, "        ");
        sb.AppendLine($"        return new {typeName}.ReadOnly(new {typeName}(entity));");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate BuildAs{TypeName}ReadOnly extension method
        sb.AppendLine($"    public static {typeName}.ReadOnlyQuery BuildAs{typeName}ReadOnly(this global::Overlook.Ecs.QueryBuilder builder, global::Overlook.Ecs.World world)");
        sb.AppendLine("    {");
        sb.AppendLine($"        builder = builder.Has{typeName}();");
        sb.AppendLine("        var query = builder.Build(world);");
        sb.AppendLine($"        return new {typeName}.ReadOnlyQuery(query, world);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate Is{TypeName} check method
        sb.AppendLine($"    public static bool Is{typeName}(this global::Overlook.Ecs.WorldEntity entity)");
        sb.AppendLine("    {");
        if (requiredComponents.Count == 0)
        {
            sb.AppendLine("        return true;");
        }
        else if (requiredComponents.Count == 1)
        {
            sb.AppendLine($"        return entity.Has<{requiredComponents[0].ComponentTypeFullName}>();");
        }
        else
        {
            sb.Append("        return ");
            for (int i = 0; i < requiredComponents.Count; i++)
            {
                if (i > 0)
                    sb.Append(" && ");
                sb.Append($"entity.Has<{requiredComponents[i].ComponentTypeFullName}>()");
            }
            sb.AppendLine(";");
        }
        sb.AppendLine("    }");

        sb.AppendLine("}");
    }
}
