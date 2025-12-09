# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Overlook Game Framework is a lightweight, modular game development framework for Unity and .NET applications. It provides high-performance data structures, an Entity Component System (ECS), and efficient memory management tools.

## Architecture

### Package Structure
The framework consists of six modular packages with dual distribution (UPM and NuGet):

| Package | UPM | NuGet | Description |
|---------|-----|-------|-------------|
| **Core** | `com.fullmetalbagel.overlook-core` | `Overlook.Core` | Foundation utilities, concurrent collections, debugging tools |
| **Pool** | `com.fullmetalbagel.overlook-pool` | `Overlook.Pool` | Thread-safe, policy-based object pooling |
| **ECS** | `com.fullmetalbagel.overlook-ecs` | `Overlook.Ecs` | High-performance Entity Component System |
| **Analyzers** | `com.fullmetalbagel.overlook-analyzer` | `Overlook.Analyzers` | Roslyn analyzers (OVL001-OVL004) |
| **Logging** | `com.fullmetalbagel.overlook-logging` | N/A | Microsoft.Extensions.Logging abstractions for Unity |
| **System** | `com.fullmetalbagel.overlook-system` | `Overlook.System` | System management and event framework |

### ECS Architecture

The ECS follows a data-oriented design with archetype-based storage:

- **Entity**: Lightweight struct wrapping an `Identity` (generational index)
- **World**: Central container managing all ECS data in an isolated environment
- **Components**: Data attached to entities (unmanaged structs or managed classes)
- **Archetypes**: Groups entities with the same component combination for cache-efficient storage
- **Query System**: Fluent API for querying entities by component composition

Key design patterns:
- Generational indices prevent use-after-free bugs
- Archetype storage enables cache-efficient component access
- Builder pattern for entity construction and queries
- Conditional compilation for Unity/non-Unity backends (`OVERLOOK_ECS_USE_UNITY_COLLECTION`)

### Type System

- Runtime type registry (`TypeIdAssigner`) maps types to ushort IDs (0-1023 limit)
- Automatic classification of managed vs. unmanaged types
- Tag components (zero-sized types) are automatically optimized

## Common Development Tasks

### Building the Project

```bash
# .NET solution (from repository root)
dotnet restore dotnet/OverlookGameFramework.sln
dotnet build dotnet/OverlookGameFramework.sln

# For release builds
dotnet build dotnet/OverlookGameFramework.sln -c Release
```

### Running Tests

```bash
# Run all .NET tests
dotnet test dotnet/OverlookGameFramework.sln

# Run specific test project
dotnet test dotnet/Overlook.Ecs.Tests/Overlook.Ecs.Tests.csproj
dotnet test dotnet/Overlook.Pool.Tests/Overlook.Pool.Tests.csproj
dotnet test dotnet/Overlook.Core.Tests/Overlook.Core.Tests.csproj
dotnet test dotnet/Overlook.System.Tests/Overlook.System.Tests.csproj
dotnet test dotnet/Overlook.Analyzer.Test/Overlook.Analyzer.Test.csproj

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Unity tests - run via Unity Test Runner in the Unity Editor
# Located in: Assets/Test/{Core,ECS,Pool,System,Logging}/
```

### Package Management

The project uses dual distribution:
- **Unity packages**: `/Packages/com.fullmetalbagel.overlook-*`
- **.NET packages**: `/dotnet/Overlook.*`

Version synchronization is critical - update both `package.json` and `.csproj` files when changing versions.

## Code Style

See `.gemini/styleguide.md` for comprehensive coding standards. Key points:

- **C# 9.0+** with file-scoped namespaces
- **Naming**: `_camelCase` (private), `s_camelCase` (static private), `PascalCase` (public)
- **Sealed classes by default** unless inheritance is intended
- **Performance-first**: Use `Span<T>`, unsafe code where justified, no LINQ in hot paths
- **No regions** - organize code logically
- **XML documentation required** for all public APIs
- **Test naming**: `MethodName_Scenario_ExpectedBehavior`

## Code Patterns and Conventions

### ECS Entity Creation
```csharp
// Using EntityBuilder
var entity = EntityBuilder.Create()
    .Add<Position>(new Position { X = 10, Y = 20 })
    .Add<Velocity>(new Velocity { X = 1, Y = 0 })
    .Build(world);

// Using WorldEntity (convenience wrapper)
var worldEntity = new WorldEntity(world, entity);
worldEntity.Add<Health>(new Health { Value = 100 });
```

### ECS Queries
```csharp
// Query entities with Position and Velocity, but not Dead
var query = QueryBuilder.Create()
    .Has<Position>()
    .Has<Velocity>()
    .Not<Dead>()
    .Build(world);

foreach (var e in query)
{
    ref var pos = ref e.Get<Position>();
    ref var vel = ref e.Get<Velocity>();
    pos.X += vel.X;
    pos.Y += vel.Y;
}
```

### Object Pooling
```csharp
// Using pooled collections with automatic disposal
using var list = new PooledList<int>();
list.Add(1);
list.Add(2);
// List automatically returned to pool when disposed
```

## CI/CD Workflows

- **dotnet-unit-test.yml**: Runs .NET tests on Ubuntu, macOS, and Windows
- **unity-test.yml**: Runs Unity tests with Mono2x and IL2CPP backends
- **publish-upm-package.yml**: Publishes UPM packages to GitHub Releases
- **publish-nuget-package.yml**: Publishes NuGet packages (manual workflow_dispatch only)

## Important Files and Locations

- **ECS Core**: `Packages/com.fullmetalbagel.overlook-ecs/`
  - World.cs - Main ECS container
  - Entity.cs - Entity definition
  - WorldEntity.cs - Convenient entity-world wrapper
  - QueryBuilder.cs - Query construction
  - Archetypes.cs - Archetype system

- **Tests**:
  - Unity: `Assets/Test/`
  - .NET: `dotnet/Overlook.*.Tests/`

- **Build Configuration**:
  - `dotnet/Directory.Build.props` - Common build settings, defines `OVERLOOK_DEBUG` for Debug config
  - `dotnet/Directory.Packages.props` - Central package management

- **Style Guide**: `.gemini/styleguide.md` - Comprehensive coding standards

## Compilation Symbols

- `OVERLOOK_DEBUG`: Enables debug assertions and leak tracking (auto-defined in Debug config)
- `OVERLOOK_ECS_USE_UNITY_COLLECTION`: Switches to Unity Collections backend (NativeBitArrayMask)
- `UNITY_5_3_OR_NEWER` / `UNITY_2020_1_OR_NEWER`: Conditional Unity API usage

## Analyzer Diagnostics

| ID | Description |
|----|-------------|
| OVL001 | Missing initialization - Property must be initialized in struct |
| OVL002 | Duplicate TypeGuid detected |
| OVL003 | Duplicate MethodGuid detected |
| OVL004 | Struct must be instantiated with parameters |

See `Packages/com.fullmetalbagel.overlook-analyzer/DIAGNOSTIC_IDS.md` for details
