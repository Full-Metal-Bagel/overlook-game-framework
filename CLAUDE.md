# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Overlook Game Framework is a lightweight, modular game development framework for Unity and .NET applications. It provides high-performance data structures, an Entity Component System (ECS), and efficient memory management tools.

## Architecture

### Package Structure
The framework consists of four modular packages, each available as both Unity Package Manager (UPM) and NuGet packages:

1. **Overlook.Core** - Foundation utilities, concurrent collections, and debugging tools
2. **Overlook.Pool** - Object pooling framework with thread-safe, policy-based pools
3. **Overlook.ECS** - High-performance Entity Component System implementation
4. **Overlook.Analyzers** - Roslyn analyzers for compile-time code quality checks

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

# .NET solution (from repository root)
dotnet restore dotnet/OverlookGameFramework.sln
dotnet build dotnet/OverlookGameFramework.sln

# For release builds
dotnet build dotnet/OverlookGameFramework.sln -c Release

### Running Tests

```bash
# Run all .NET tests
dotnet test dotnet/OverlookGameFramework.sln

# Run specific test project
dotnet test dotnet/Overlook.Ecs.Tests/Overlook.Ecs.Tests.csproj
dotnet test dotnet/Overlook.Pool.Tests/Overlook.Pool.Tests.csproj
dotnet test dotnet/Overlook.Analyzer.Test/Overlook.Analyzer.Test.csproj

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Unity tests (requires Unity installed)
# These are typically run via Unity Test Runner in the Unity Editor
# Located in: Assets/Test/ECS/ and Assets/Test/Pool/
```

### Package Management

The project uses dual distribution:
- **Unity packages**: Located in `/Packages/com.fullmetalbagel.overlook-*`
- **.NET packages**: Located in `/dotnet/Overlook.*`

Version synchronization is critical - update both `package.json` and `.csproj` files when changing versions.

### Debugging

Enable debug features by building in Debug configuration:
```bash
dotnet build -c Debug
```

This enables:
- `OVERLOOK_DEBUG` symbol for assertions and leak tracking
- Detailed logging in object pools
- Additional runtime checks

## Code Patterns and Conventions

### ECS Entity Creation
```csharp
// Using EntityBuilder
var entity = EntityBuilder.Create()
    .Add<Position>(new Position { X = 10, Y = 20 })
    .Add<Velocity>(new Velocity { X = 1, Y = 0 })
    .Build(world);

// Using WorldEntity (new convenience wrapper)
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

The project uses GitHub Actions for automated testing and publishing:

- **dotnet-unit-test.yml**: Runs .NET tests on Ubuntu, macOS, and Windows
- **unity-test.yml**: Runs Unity tests with Mono2x and IL2CPP backends
- **publish-upm-package.yml**: Publishes UPM packages to GitHub Releases
- **publish-nuget-package.yml**: Publishes NuGet packages to nuget.org

## Important Files and Locations

- **ECS Core**: `Packages/com.fullmetalbagel.overlook-ecs/`
  - World.cs - Main ECS container
  - Entity.cs - Entity definition
  - WorldEntity.cs - Convenient entity-world wrapper
  - QueryBuilder.cs - Query construction
  - Archetypes.cs - Archetype system

- **Tests**:
  - Unity: `Assets/Test/ECS/`
  - .NET: `dotnet/Overlook.*.Tests/`

- **Build Configuration**:
  - `dotnet/Directory.Build.props` - Common build settings
  - `dotnet/Directory.Packages.props` - Central package management

## Compilation Symbols

- `OVERLOOK_DEBUG`: Enables debug assertions and leak tracking
- `OVERLOOK_ECS_USE_UNITY_COLLECTION`: Switches to Unity Collections backend
- `UNITY_5_3_OR_NEWER`: Conditional Unity API usage

## Recent Changes

Current branch `world-entity` adds the `WorldEntity` struct, providing a more ergonomic API for entity operations without repeatedly passing the world reference.