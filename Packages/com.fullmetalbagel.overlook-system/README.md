# Overlook System

A flexible system management framework for the Overlook Game Framework. Provides system lifecycle management, tick stages, and an event system for coordinating game logic across multiple systems.

## Installation

### Unity Package Manager

```
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-system
```

### NuGet

```bash
dotnet add package Overlook.System
```

**Requirements:** Unity 2022.3 or later

**Dependencies:**
- `com.fullmetalbagel.overlook-logging`
- `com.fullmetalbagel.overlook-pool`
- `com.unity.addressables`

## Features

- **System Lifecycle Management**: Create, tick, and dispose systems in a controlled manner
- **Tick Stages**: Organize systems into different tick stages for ordered execution
- **Event System**: Efficient event propagation with configurable lifetime
- **Factory Pattern**: Flexible system creation via `ISystemFactory`
- **Dependency Injection**: Integrates with OneShot DI container
- **High-Performance Logging**: Uses `LoggerMessage` source generation for zero-allocation logging

## Core Concepts

### ISystem

The base interface for all systems:

```csharp
using Overlook.System;

public class MovementSystem : ISystem
{
    public void Tick()
    {
        // Update entity positions
    }
}
```

### SystemManager

Central manager for all systems. Handles creation, ticking, and disposal:

```csharp
using Microsoft.Extensions.Logging;
using OneShot;
using Overlook.System;

// Create with DI container and system factories
var container = new Container();
var factories = new List<ISystemFactory>
{
    new SystemFactory<InputSystem>("Input", tickStage: 0),
    new SystemFactory<MovementSystem>("Movement", tickStage: 1),
    new SystemFactory<RenderSystem>("Render", tickStage: 2)
};

var logger = new UnityLogHandler<SystemManager>();
var manager = new SystemManager(container, factories, logger);

// Create all systems
manager.CreateSystems();

// Game loop
void Update()
{
    manager.Tick(0); // Input stage
    manager.Tick(1); // Movement stage
    manager.Tick(2); // Render stage
}

// Cleanup
manager.Dispose();
```

### ISystemFactory

Factory interface for system creation with configuration:

```csharp
public interface ISystemFactory : IDisposable
{
    string SystemName { get; }
    bool Enable { get; }
    byte TickStage { get; }
    int TickTimes { get; }  // -1 for infinite, 0 to disable, >0 for limited ticks
    ISystem Resolve(Container container, int systemIndex);
}
```

Built-in implementations:

```csharp
// Generic factory with automatic DI resolution
var factory = new SystemFactory<MySystem>("MySystem", tickStage: 0, enable: true, tickTimes: -1);

// Instance factory for pre-created systems
var instance = new MySystem();
var factory = new InstanceSystemFactory<MySystem>(instance, "MySystem", tickStage: 0);
```

### Tick Stages

Systems are organized into tick stages for ordered execution:

```csharp
// Stage 0: Input processing
// Stage 1: Game logic
// Stage 2: Physics
// Stage 3: Rendering

var factories = new List<ISystemFactory>
{
    new SystemFactory<InputSystem>("Input", tickStage: 0),
    new SystemFactory<PlayerSystem>("Player", tickStage: 1),
    new SystemFactory<EnemySystem>("Enemy", tickStage: 1),
    new SystemFactory<PhysicsSystem>("Physics", tickStage: 2),
    new SystemFactory<RenderSystem>("Render", tickStage: 3)
};

// In your game loop, tick each stage
manager.Tick(0); // All stage 0 systems
manager.Tick(1); // All stage 1 systems
manager.Tick(2); // All stage 2 systems
manager.Tick(3); // All stage 3 systems
```

### System Events

Efficient event system for inter-system communication:

```csharp
using Overlook.System;

// Define an event type
[SystemEvent(InitCapacity = 16)]
public struct DamageEvent
{
    public int TargetId;
    public float Amount;
}

// Create event container
var damageEvents = new SystemEvents<DamageEvent>();

// Publish events (in combat system)
damageEvents.Append(new DamageEvent { TargetId = 42, Amount = 25.5f });

// Events can last multiple frames
damageEvents.Append(new DamageEvent { TargetId = 10, Amount = 10f }, lastingFrames: 3);

// Consume events (in health system)
foreach (var evt in damageEvents)
{
    ApplyDamage(evt.TargetId, evt.Amount);
}

// Or use ForEach with data parameter (avoids closure allocation)
damageEvents.ForEach(healthSystem, static (system, evt) =>
{
    system.ApplyDamage(evt.TargetId, evt.Amount);
});

// Cleanup
damageEvents.Dispose();
```

### SystemEventsManager

Manages events per tick stage:

```csharp
// Access stage-specific events
var stageEvents = manager.GetStageEvents(tickStage: 1);

// Events are automatically ticked when the stage ticks
manager.Tick(1); // Also ticks stage 1 events
```

## Runtime System Information

Query system state at runtime:

```csharp
// Get system count
int count = manager.Count;

// Access systems and their metadata
for (int i = 0; i < manager.Count; i++)
{
    RuntimeSystem info = manager.GetSystem(i);
    Console.WriteLine($"System: {info.Name}, Stage: {info.TickStage}, Remaining: {info.RemainedTimes}");
}

// Direct access to systems
IReadOnlyList<ISystem> systems = manager.Systems;
IReadOnlyList<string> names = manager.SystemNames;
```

## Error Handling

The SystemManager handles errors gracefully:

- System tick failures are logged but don't crash other systems
- Disposal failures are logged but continue disposing remaining systems
- Invalid tick stages are silently ignored

## Debug Features

When `OVERLOOK_DEBUG` is defined:

- Stack traces are captured for event creation
- Additional assertions validate thread safety
- Enhanced error messages for debugging

## Example: Complete Game Loop

```csharp
public class GameRunner : IDisposable
{
    private readonly SystemManager _systemManager;

    public GameRunner()
    {
        var container = new Container();

        // Register services
        container.Register<ILogger<GameRunner>>(new UnityLogHandler<GameRunner>());

        var factories = new List<ISystemFactory>
        {
            new SystemFactory<InputSystem>("Input", 0),
            new SystemFactory<PlayerController>("Player", 1),
            new SystemFactory<AISystem>("AI", 1),
            new SystemFactory<PhysicsSystem>("Physics", 2),
            new SystemFactory<AnimationSystem>("Animation", 3),
            new SystemFactory<RenderSystem>("Render", 4)
        };

        var logger = container.Resolve<ILogger<SystemManager>>();
        _systemManager = new SystemManager(container, factories, logger);
        _systemManager.CreateSystems();
    }

    public void Update()
    {
        // Fixed order execution
        for (byte stage = 0; stage < 5; stage++)
        {
            _systemManager.Tick(stage);
        }
    }

    public void Dispose()
    {
        _systemManager.Dispose();
    }
}
```

## License

MIT License - see the [LICENSE](../../LICENSE) file for details.
