# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Overlook Game Framework is a lightweight, modular game development framework for Unity and .NET applications. It provides high-performance data structures, an Entity Component System (ECS), and efficient memory management tools. The framework supports dual-target deployment - both as Unity packages and standalone .NET libraries via NuGet.

## Key Commands

### Building and Testing (.NET)
```bash
cd dotnet
dotnet restore              # Restore NuGet packages
dotnet build               # Build all projects
dotnet test                # Run all tests
dotnet pack -c Release     # Create NuGet packages
```

### Unity Development
- Unity version: 2022.3.60f1
- Run tests through Unity Test Runner (Window > General > Test Runner)
- Tests cover both Mono2x and IL2CPP backends

### Running a Single Test
```bash
cd dotnet
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

## Architecture Overview

### Package Structure
The framework consists of interconnected packages with clear dependencies:

1. **Overlook Core** (`com.fullmetalbagel.overlook-core`) - Foundation layer
   - Thread-safe collections (`ConcurrentHashSet`, `ConcurrentQueue`)
   - `CircularBuffer<T>` with conditional Unity/NET implementations
   - Core utilities and extensions

2. **Overlook Pool** (`com.fullmetalbagel.overlook-pool`) - Object pooling
   - Generic pooling with policy-based configuration
   - Pooled collections with `ref struct` wrappers for automatic disposal
   - Thread-safe operations via `ConcurrentQueue<T>`

3. **Overlook ECS** (`com.fullmetalbagel.overlook-ecs`) - Entity Component System
   - Archetype-based storage for cache-friendly access
   - `World` contains entities and components
   - `QueryBuilder` for fluent query composition
   - Support for both unmanaged structs and managed classes as components
   - Tagged components for complex relationships

4. **Overlook System** (`com.fullmetalbagel.overlook-system`) - System management
   - `SystemManager` handles system lifecycle
   - `SystemGroup` for logical organization
   - Event system with `SystemEvents<T>`
   - Addressable system loading support

### Dual Build System
- **Unity Packages**: Located in `/Packages/com.fullmetalbagel.*`
- **.NET Projects**: Located in `/dotnet/*` directory
- Unity packages contain source code, .NET projects reference them
- Central package management via `Directory.Packages.props`

### Key Architectural Patterns

1. **Data-Oriented Design**: ECS uses archetype storage for contiguous memory layout
2. **Object Pooling**: Extensive use throughout to minimize GC pressure
3. **Conditional Compilation**: 
   - `OVERLOOK_DEBUG` - Enhanced debugging and leak tracking
   - `OVERLOOK_ECS_USE_UNITY_COLLECTION` - Unity Collections integration
4. **Performance Focus**: Strategic use of unsafe code, ref structs, and cache-friendly data structures

### Code Standards
- Nullable reference types enabled (`#nullable enable`)
- Warnings as errors in release builds
- Full static code analysis with Microsoft analyzers
- C# 11 language features
- Follows Unity package naming convention: `com.fullmetalbagel.*`

### Testing Approach
- Separate test projects for each package in `/dotnet/*Tests`
- Unity tests in `/Assets/Test` directory
- Mock Unity APIs for cross-platform .NET testing
- Custom `AssertUtils` for enhanced Unity test assertions
- CI/CD via GitHub Actions for both Unity and .NET targets