# Overlook Core

Foundation package for the Overlook Game Framework. Provides essential low-level utilities, data structures, and C# extensions for Unity and .NET applications.

## Installation

### Unity Package Manager

```
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-core
```

### NuGet

```bash
dotnet add package Overlook.Core
```

## Features

### Concurrent Collections

Thread-safe collections for multi-threaded game systems:

```csharp
// Thread-safe hash set
var activeEntities = new ConcurrentHashSet<Entity>();
activeEntities.Add(entity);
if (activeEntities.Contains(entity)) { ... }

// Thread-safe queue for job results
var resultQueue = new ConcurrentQueue<JobResult>();
resultQueue.Enqueue(result);
while (resultQueue.TryDequeue(out var item)) { ... }
```

### Debugging Utilities

Enhanced debugging with conditional compilation support:

```csharp
// Only executes in Debug builds
Debug.Assert(entity.IsValid, "Entity must be valid");
Debug.Log("Processing complete");
```

### Circular Buffer

High-performance circular buffer for streaming data:

```csharp
var buffer = new CircularBuffer<float>(capacity: 256);
buffer.Push(value);
ref var oldest = ref buffer.Peek();
buffer.Pop();
```

### Type Utilities

Helper methods for working with unmanaged types and memory:

```csharp
// Reference equality comparison (avoids boxing)
var comparer = new ReferenceEqualityComparer<MyClass>();

// Unmanaged type extensions for direct memory access
var size = UnmanagedExtensions.SizeOf<MyStruct>();
```

### Modern C# Support

Includes polyfills for modern C# features in older Unity versions:

- `IsExternalInit` for init-only properties
- `DisallowDefaultConstructorAttribute` for struct validation
- `CallerArgumentExpressionAttribute` for better error messages

## Dependencies

- Unity 2022.3 or later (for Unity)
- .NET Standard 2.1 / .NET 6.0+ (for standalone .NET)

## License

MIT License - see the [LICENSE](../../LICENSE) file for details.
