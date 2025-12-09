# Overlook Logging

Microsoft.Extensions.Logging abstractions for Unity. Provides `ILogger`, `LogLevel`, and `LoggerMessage` attribute for structured, high-performance logging in Unity projects.

## Installation

### Unity Package Manager

```
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-logging
```

**Requirements:** Unity 2022.3 or later

## Features

- Standard `ILogger` and `ILogger<T>` interfaces from Microsoft.Extensions.Logging
- `LogLevel` enum for severity filtering (Trace, Debug, Information, Warning, Error, Critical)
- `UnityLogHandler` - bridges `ILogger` to Unity's console
- Source generator support via `[LoggerMessage]` attribute for zero-allocation logging
- Structured logging with named parameters
- Scoping with Unity Object context

## Quick Start

### Basic Usage

```csharp
using Microsoft.Extensions.Logging;

public class PlayerController : MonoBehaviour
{
    private readonly ILogger _logger = new UnityLogHandler(nameof(PlayerController));

    void Start()
    {
        _logger.Log(LogLevel.Information, default, "Player initialized", null,
            (msg, ex) => msg);
    }
}
```

### Generic Logger with Auto Category

```csharp
using Microsoft.Extensions.Logging;

public class EnemyAI : MonoBehaviour
{
    // Category name automatically set to "EnemyAI"
    private readonly ILogger<EnemyAI> _logger = new UnityLogHandler<EnemyAI>();

    void OnDeath()
    {
        _logger.Log(LogLevel.Debug, default, "Enemy defeated", null,
            (msg, ex) => msg);
    }
}
```

### Log Level Filtering

```csharp
// Only log warnings and above
var logger = new UnityLogHandler("GameManager", minLevel: LogLevel.Warning);

// This will be logged
logger.Log(LogLevel.Error, default, "Critical error!", null, (msg, ex) => msg);

// This will be filtered out
logger.Log(LogLevel.Debug, default, "Debug info", null, (msg, ex) => msg);
```

### Using Event IDs

```csharp
var logger = new UnityLogHandler("Network");
var eventId = new EventId(1001, "ConnectionLost");

logger.Log(LogLevel.Error, eventId, "Lost connection to server", null,
    (msg, ex) => msg);
// Output: [ERROR] [Network] [ConnectionLost(1001)] Lost connection to server
```

### Unity Object Context

Associate log messages with specific GameObjects for easy identification in the console:

```csharp
public class ItemPickup : MonoBehaviour
{
    private ILogger _logger;

    void Awake()
    {
        // Pass 'this' as context - clicking the log will highlight this object
        _logger = new UnityLogHandler("ItemPickup", context: this);
    }

    void OnPickup()
    {
        _logger.Log(LogLevel.Information, default, "Item collected", null,
            (msg, ex) => msg);
    }
}
```

### Scoped Context

Temporarily change the Unity Object context using scopes:

```csharp
var logger = new UnityLogHandler("Inventory");

using (logger.BeginScope(targetGameObject))
{
    // Logs within this scope will reference targetGameObject
    logger.Log(LogLevel.Debug, default, "Processing item", null, (msg, ex) => msg);
}
```

## High-Performance Logging

### LoggerMessage.Define

For frequently called logging paths, use `LoggerMessage.Define` to create cached delegates that minimize allocations:

```csharp
public static class GameLogs
{
    private static readonly Action<ILogger, string, int, Exception?> _playerScored =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(100, "PlayerScored"),
            "Player {PlayerName} scored {Points} points");

    public static void PlayerScored(ILogger logger, string playerName, int points)
        => _playerScored(logger, playerName, points, null);
}

// Usage:
GameLogs.PlayerScored(_logger, "Alice", 500);
// Output: [INFO] Player Alice scored 500 points
```

### LoggerMessage Attribute (Source Generator)

The package includes the `Microsoft.Extensions.Logging.Generators` source generator for compile-time log method generation:

```csharp
public partial class CombatSystem
{
    private readonly ILogger _logger = new UnityLogHandler<CombatSystem>();

    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Debug,
        Message = "Entity {EntityId} dealt {Damage} damage to {TargetId}")]
    partial void LogDamageDealt(int entityId, float damage, int targetId);

    public void DealDamage(int attacker, int target, float amount)
    {
        LogDamageDealt(attacker, amount, target);
        // ... combat logic
    }
}
```

The source generator creates optimized implementations that:
- Check `IsEnabled` before formatting
- Avoid boxing for value types
- Reuse formatter instances

## Log Levels

| Level | Value | Unity LogType | Description |
|-------|-------|---------------|-------------|
| Trace | 0 | Log | Most detailed messages, typically for debugging |
| Debug | 1 | Log | Development-time information |
| Information | 2 | Log | General application flow |
| Warning | 3 | Warning | Abnormal or unexpected events |
| Error | 4 | Error | Errors that don't stop execution |
| Critical | 5 | Error | Unrecoverable failures |
| None | 6 | - | Disables logging |

## Output Format

Log messages are formatted as:
```
[LEVEL] [Category] [EventName(EventId)] Message
```

Examples:
```
[INFO] [PlayerController] Player spawned at position (10, 0, 5)
[WARN] [NetworkManager] [Timeout(503)] Connection attempt timed out
[ERROR] [SaveSystem] Failed to write save file
```

## API Reference

### UnityLogHandler

```csharp
// With explicit log handler
new UnityLogHandler(ILogHandler logHandler, string categoryName = "",
    LogLevel minLevel = LogLevel.Trace, UnityEngine.Object? context = null)

// Using Unity's default log handler
new UnityLogHandler(string categoryName = "", LogLevel minLevel = LogLevel.Trace,
    UnityEngine.Object? context = null)
```

### UnityLogHandler\<T\>

```csharp
// Category name derived from type T
new UnityLogHandler<T>(ILogHandler logHandler, LogLevel minLevel = LogLevel.Trace,
    UnityEngine.Object? context = null)

new UnityLogHandler<T>(LogLevel minLevel = LogLevel.Trace,
    UnityEngine.Object? context = null)
```

### ILogger Methods

```csharp
void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
    Exception? exception, Func<TState, Exception?, string> formatter);

bool IsEnabled(LogLevel logLevel);

IDisposable? BeginScope<TState>(TState state) where TState : notnull;
```

## Integration with Dependency Injection

While this package provides standalone logging, the `ILogger` interface is compatible with Microsoft.Extensions.DependencyInjection patterns:

```csharp
public class GameService
{
    private readonly ILogger<GameService> _logger;

    public GameService(ILogger<GameService> logger)
    {
        _logger = logger;
    }
}

// Manual wiring
var service = new GameService(new UnityLogHandler<GameService>());
```

## License

MIT License - see the [LICENSE](../../LICENSE) file for details.

This package includes code from the .NET Foundation licensed under the MIT license.
