# Overlook Analyzers

Roslyn analyzers and source generators for the Overlook Game Framework. Provides compile-time code quality checks and automatic code generation to enforce best practices and reduce boilerplate.

## Installation

### Unity Package Manager

```
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-analyzer
```

### NuGet

```bash
dotnet add package Overlook.Analyzers
```

## Diagnostic IDs

All diagnostics follow the pattern `OVL###`:

| ID | Severity | Description |
|----|----------|-------------|
| OVL001 | Warning | Missing initialization - Property must be initialized in struct |
| OVL002 | Error | Duplicate TypeGuid detected - Each type should have a unique GUID |
| OVL003 | Error | Duplicate MethodGuid detected - Each method should have a unique GUID |
| OVL004 | Warning | Struct instantiation without parameters - Struct must be instantiated with parameters |

## Analyzers

### OptionalInit Analyzer (OVL001)

Ensures properties marked with `[OptionalInit]` are properly initialized in structs:

```csharp
public struct PlayerConfig
{
    [OptionalInit]
    public int MaxHealth { get; init; }

    [OptionalInit]
    public float Speed { get; init; }
}

// Warning OVL001: MaxHealth must be initialized
var config = new PlayerConfig { Speed = 5.0f };

// Correct usage
var config = new PlayerConfig { MaxHealth = 100, Speed = 5.0f };
```

### Duplicated GUID Analyzer (OVL002, OVL003)

Detects duplicate GUIDs across types and methods to prevent identity conflicts:

```csharp
[TypeGuid("550e8400-e29b-41d4-a716-446655440000")]
public class PlayerSystem { }

// Error OVL002: Duplicate TypeGuid detected
[TypeGuid("550e8400-e29b-41d4-a716-446655440000")]
public class EnemySystem { }
```

### DisallowDefaultConstructor Analyzer (OVL004)

Enforces that certain structs must be instantiated with parameters:

```csharp
[DisallowDefaultConstructor]
public struct EntityRef
{
    public int Id { get; }
    public EntityRef(int id) => Id = id;
}

// Warning OVL004: EntityRef must be instantiated with parameters
var invalid = new EntityRef();

// Correct usage
var valid = new EntityRef(42);
```

## Source Generators

### QueryableSourceGenerator

Generates strongly-typed entity wrappers from `[QueryComponent]` attributes for type-safe ECS queries:

```csharp
using Overlook.Ecs;

[QueryComponent(typeof(Position))]
[QueryComponent(typeof(Velocity))]
[QueryComponent(typeof(Health), IsOptional = true)]
public partial record struct MovableEntity;

// Generated code provides:
// - Typed property accessors (Position, Velocity)
// - Optional component helpers (HasHealth, TryGetHealth)
// - Query builder extensions (HasMovableEntity, BuildAsMovableEntity)
// - ReadOnly view for safe read access
// - Is/As conversion methods

// Usage:
var query = QueryBuilder.Create()
    .BuildAsMovableEntity(world);

foreach (var entity in query)
{
    ref var pos = ref entity.Position;
    ref var vel = ref entity.Velocity;
    pos.X += vel.DX;

    if (entity.HasHealth)
    {
        var health = entity.TryGetHealth();
    }
}
```

#### QueryComponent Attribute Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | string | Component type name | Custom property name |
| `IsOptional` | bool | false | Component may not exist on entity |
| `IsReadOnly` | bool | false | Generate read-only accessor |
| `QueryOnly` | bool | false | Include in query but don't generate property |

### Other Source Generators

- **MemberValueAccessorSourceGenerator**: Generates accessor methods for member values
- **PartialConstructorAndDispose**: Generates partial constructors and dispose patterns
- **GlobalSuppressions**: Generates suppression attributes for Unity-specific warnings

## Configuration

### Suppressing Diagnostics

```csharp
// Suppress specific diagnostic
#pragma warning disable OVL001
var config = new PlayerConfig();
#pragma warning restore OVL001

// Or via .editorconfig
[*.cs]
dotnet_diagnostic.OVL001.severity = none
```

### Severity Configuration

In your `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.OVL001.severity = warning
dotnet_diagnostic.OVL002.severity = error
dotnet_diagnostic.OVL003.severity = error
dotnet_diagnostic.OVL004.severity = warning
```

## Adding New Diagnostics

When adding new diagnostic descriptors:
1. Use the next available `OVL###` number in sequence
2. Update the `DIAGNOSTIC_IDS.md` file
3. Ensure all tests filter by the `OVL` prefix
4. Follow the existing pattern for diagnostic severity and categories

## License

MIT License - see the [LICENSE](../../LICENSE) file for details.
