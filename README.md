# Overlook Game Framework

A lightweight, modular game development framework for Unity and .NET applications. Overlook combines high-performance data structures, a powerful Entity Component System (ECS), and efficient memory management tools to help developers create responsive, scalable games with minimal overhead. Built with simplicity, performance, and flexibility as core principles, it provides essential building blocks while staying out of your creative way.

## Packages

The Overlook Game Framework is composed of six modular packages, each available as both Unity Package Manager (UPM) and NuGet packages:

| Package | Description |
|---------|-------------|
| **[Overlook Core](./Packages/com.fullmetalbagel.overlook-core/README.md)** | Foundation utilities, concurrent collections, and debugging tools |
| **[Overlook Pool](./Packages/com.fullmetalbagel.overlook-pool/README.md)** | Thread-safe, policy-based object pooling framework |
| **[Overlook ECS](./Packages/com.fullmetalbagel.overlook-ecs/README.md)** | High-performance Entity Component System with archetype-based storage |
| **[Overlook Analyzers](./Packages/com.fullmetalbagel.overlook-analyzer/README.md)** | Roslyn analyzers and source generators for compile-time code quality |
| **[Overlook Logging](./Packages/com.fullmetalbagel.overlook-logging/README.md)** | Microsoft.Extensions.Logging abstractions for Unity |
| **[Overlook System](./Packages/com.fullmetalbagel.overlook-system/README.md)** | System management and event framework for game loops |

## Installation

### Unity Package Manager

Add packages via git URL in the Unity Package Manager:

```
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-core
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-ecs
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-pool
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-analyzer
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-logging
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-system
```

### NuGet

```bash
dotnet add package Overlook.Core
dotnet add package Overlook.Pool
dotnet add package Overlook.Ecs
dotnet add package Overlook.Analyzers
dotnet add package Overlook.System
```

## Quick Start

```csharp
using Overlook.Ecs;
using Overlook.Pool;

// Define components
public struct Position { public float X, Y; }
public struct Velocity { public float DX, DY; }

// Create a world and entities
using var world = new World();

var entity = EntityBuilder.Create()
    .Add(new Position { X = 0, Y = 0 })
    .Add(new Velocity { DX = 1, DY = 0.5f })
    .Build(world);

// Query and update entities
var query = QueryBuilder.Create()
    .Has<Position>()
    .Has<Velocity>()
    .Build(world);

foreach (var e in query)
{
    ref var pos = ref e.Get<Position>();
    ref readonly var vel = ref e.Get<Velocity>();
    pos.X += vel.DX;
    pos.Y += vel.DY;
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.