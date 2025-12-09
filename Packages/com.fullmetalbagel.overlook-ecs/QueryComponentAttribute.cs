#nullable enable
using System;

namespace Overlook.Ecs;

/// <summary>
/// Marks a type as having queryable components for source generation.
/// Apply this attribute to partial record structs to generate strongly-typed entity wrappers.
/// </summary>
/// <example>
/// <code>
/// [QueryComponent(typeof(Position))]
/// [QueryComponent(typeof(Velocity))]
/// [QueryComponent(typeof(Health), IsOptional = true)]
/// public partial record struct PlayerEntity;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class QueryComponentAttribute : Attribute
{
    /// <summary>
    /// The component type to generate property for.
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    /// Custom property name. If null, uses the component type name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// If true, generates Has{Name} property and TryGet/TrySet methods for optional component access.
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// If true, property will be read-only (no setter generated).
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// If true, component is used only for querying and no property will be generated.
    /// The component will still be included in Has{TypeName}() query builder method.
    /// </summary>
    public bool QueryOnly { get; set; }

    public QueryComponentAttribute(Type componentType)
    {
        ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
    }
}
