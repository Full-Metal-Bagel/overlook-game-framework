# Overlook Game Framework - Style Guide

This style guide defines the coding standards and patterns for the Overlook Game Framework, a high-performance game development framework for Unity and .NET applications.

## Core Principles

### SOLID Architecture
- **Single Responsibility**: Each class or module has one clear responsibility
- **Open/Closed**: Design code that is open for extension but closed for modification
- **Liskov Substitution**: Derived classes must be substitutable for base classes
- **Interface Segregation**: Don't force clients to implement unused methods
- **Dependency Inversion**: Depend on abstractions, not concrete implementations

### Performance-First Development
- Prioritize performance and memory efficiency
- Use data-oriented design patterns where appropriate
- Minimize garbage collection pressure through object pooling
- Leverage modern C# features for zero-allocation code (Span, stackalloc, ref returns)
- Use unsafe code and pointers where performance benefits justify the complexity

### Keep It Simple (KISS)
- Seek simple and clear solutions; avoid unnecessary complexity
- Use straightforward algorithms and patterns where possible
- Simplicity enhances both performance and maintainability

### You Aren't Gonna Need It (YAGNI)
- Only implement features when they are needed
- Avoid speculative engineering
- Focus on current requirements

## C# Language Standards

### Modern C# Features
- **Target**: C# 9.0+ for file-scoped namespaces and modern features
- **Nullable Reference Types**: Use nullable reference types where appropriate
- **Pattern Matching**: Prefer pattern matching over traditional type checks
- **Records**: Use record types for immutable data structures
- **Spans and Memory**: Use `Span<T>`, `ReadOnlySpan<T>`, and `Memory<T>` for high-performance scenarios

**Example:**
```csharp
// Good - Modern C# with spans and file-scoped namespace
namespace Overlook.Core;

public sealed class DataProcessor
{
    public void Process(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return;
        // Process without allocation
    }
}

// Bad - Old style with namespace blocks and array allocations
namespace Overlook.Core
{
    public class DataProcessor
    {
        public void Process(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            // Allocates memory unnecessarily
        }
    }
}
```

### Naming Conventions

Follow these strict naming patterns (enforced by .editorconfig):

- **Private fields**: `_camelCase` with underscore prefix
- **Static private fields**: `s_camelCase` with 's_' prefix
- **Public fields/properties**: `PascalCase`
- **Methods**: `PascalCase`
- **Parameters**: `camelCase`
- **Const fields**: `PascalCase`
- **Local variables**: `camelCase`

**Example:**
```csharp
public sealed class Example
{
    private int _instanceField;
    private static int s_staticField;
    private const int MaxCapacity = 1024;

    public int PublicProperty { get; set; }

    public void DoSomething(int parameter)
    {
        int localVariable = parameter * 2;
    }
}
```

### File Organization

- **One primary type per file**: Each file should contain one main public type
- **File-scoped namespaces**: Always use file-scoped namespace declarations
- **Using directives**: Place at the top, outside namespace, sorted with System directives first
- **No regions**: Avoid #region directives; organize code logically instead

**Example:**
```csharp
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Overlook.Ecs;

public sealed class EntityManager : IDisposable
{
    // Implementation
}
```

## Documentation

### XML Documentation
All public APIs **must** have XML documentation comments:

- **Summary**: Describe what the type/member does
- **Type parameters**: Document all generic parameters with `<typeparam>`
- **Parameters**: Document all parameters with `<param>`
- **Returns**: Document return values with `<returns>`
- **Exceptions**: Document thrown exceptions with `<exception>`
- **Remarks**: Add additional context with `<remarks>` when needed

**Example:**
```csharp
/// <summary>
/// High-performance circular buffer implementation optimized for single-thread scenarios.
/// Uses Marshal.AllocHGlobal for .NET applications and Unity.Collections.NativeArray for Unity games.
/// </summary>
/// <typeparam name="T">The struct type to store in the buffer</typeparam>
public sealed unsafe class CircularBuffer<T> : IDisposable where T : unmanaged
{
    /// <summary>
    /// Adds an item to the end of the buffer.
    /// If the buffer is full, expands the capacity automatically.
    /// </summary>
    /// <param name="item">The item to add</param>
    public void Push(T item)
    {
        // Implementation
    }
}
```

### Internal Documentation
- Use `// TODO:` comments for planned improvements
- Use `// HACK:` for temporary workarounds that need fixing
- Add inline comments to explain complex algorithms or non-obvious logic
- Don't comment obvious code

## Unity Integration Patterns

### Conditional Compilation
Use conditional compilation to support both Unity and standalone .NET:

```csharp
#if UNITY_2020_1_OR_NEWER
    private NativeArray<T> _nativeArray;
    private T* _buffer;
#else
    private T* _buffer;
#endif
```

### Unity-Specific Types
- Use `Unity.Collections.NativeArray` for Unity builds
- Use `Marshal.AllocHGlobal` for standalone .NET
- Properly dispose Unity-specific resources in `Dispose()` patterns

### Debug Logging
Use Unity's Debug class with conditional compilation:

```csharp
[Conditional("OVERLOOK_ECS_DEBUG")]
private static void WarnSystemType(Type type)
{
    Debug.LogWarning("Warning message");
}
```

## Performance Patterns

### Memory Management

**Object Pooling**: Always use object pools for frequently allocated objects
```csharp
// Good - Use pooling
private readonly Pool<EntityMeta> _meta = new(512, EntityMeta.Invalid);
var instance = _poolsCache.GetPool<T>().Rent();
// Use instance
_poolsCache.GetPool<T>().Recycle(instance);

// Bad - Direct allocation in hot paths
var instance = new T(); // Causes GC pressure
```

**Span Usage**: Use `Span<T>` and `ReadOnlySpan<T>` for memory slices
```csharp
// Good - No allocation
public int CopyTo(Span<T> destination)
{
    // Work with span directly
}

// Bad - Creates array
public T[] CopyTo()
{
    return new T[count]; // Allocates
}
```

### Unsafe Code
Use unsafe code judiciously for performance-critical paths:

```csharp
public sealed unsafe class CircularBuffer<T> where T : unmanaged
{
    private T* _buffer;

    public void Push(T item)
    {
        _buffer[_tail] = item;
    }
}
```

### Generic Constraints
Use appropriate generic constraints to enable optimal code generation:

```csharp
// Good - Enables direct memory operations
public void AddComponent<T>(Identity identity, T data) where T : unmanaged

// Good - Ensures pooling compatibility
public T AddObjectComponent<T>(Identity identity) where T : class, new()
```

## Entity Component System (ECS) Patterns

### Component Types

**Value Components**: Use unmanaged structs
```csharp
public struct Position
{
    public float X;
    public float Y;
    public float Z;
}
```

**Object Components**: Use classes with pooling
```csharp
public sealed class RenderComponent
{
    public Mesh Mesh { get; set; }
    public Material Material { get; set; }
}
```

**Tag Components**: Use empty structs for flags
```csharp
public struct Active { } // Zero-size tag
```

### Entity Operations
Always check entity validity:

```csharp
public void DoSomething(Identity identity)
{
    ThrowIfNotAlive(identity);
    // Safe to operate on entity
}
```

### Query Patterns
Design queries to be cache-friendly and minimize virtual calls

## Error Handling

### Defensive Programming
- Use `Debug.Assert` for internal invariants (compiled out in release)
- Throw `ArgumentException` / `ArgumentNullException` for public API validation
- Use `InvalidOperationException` for state errors

**Example:**
```csharp
public void Push(T item)
{
    if (capacity <= 0)
        throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");

    Debug.Assert(_buffer != null, "Buffer should be initialized");

    if (_count == 0)
        throw new InvalidOperationException("Buffer is empty");
}
```

### Conditional Validation
Use conditional compilation for debug-only checks:

```csharp
[Conditional("OVERLOOK_ECS_DEBUG")]
private static void WarningIfCanBeUnmanaged(Type type)
{
    // Validation that only runs in debug builds
}
```

## Code Style Specifics

### Braces and Indentation
- **Allman style**: Opening braces on new line
- **4 spaces** for indentation (no tabs)
- **Always use braces** for control structures, even single statements

```csharp
// Good
if (condition)
{
    DoSomething();
}

// Bad
if (condition) DoSomething();
```

### Expression Bodies
Use expression bodies for simple members:

```csharp
// Good for simple properties
public int Count => _count;
public bool IsEmpty => Count == 0;

// Good for simple methods
public void Clear() => _count = 0;

// Bad for complex logic
public void ComplexMethod() =>
{
    // Multiple statements - use block body instead
}
```

### Null Handling
- Use null-coalescing and null-conditional operators
- Use pattern matching for null checks
- Validate public API parameters

```csharp
// Good
_expandFunction = expandFunction ?? (capacity => capacity * 2);
var result = items?.Count ?? 0;

// Good - Pattern matching
if (data is null) throw new ArgumentNullException(nameof(data));
```

### LINQ Usage
- Avoid LINQ in hot paths (it allocates)
- Prefer foreach or for loops in performance-critical code
- LINQ is acceptable in initialization or infrequent code paths

```csharp
// Good - Hot path, no allocation
for (int i = 0; i < count; i++)
{
    Process(_buffer[i]);
}

// Bad - Hot path with allocation
foreach (var item in _buffer.Where(x => x.IsActive))
{
    Process(item);
}

// Acceptable - Initialization
var queries = _queries.Values.Where(q => IsCompatible(q)).ToList();
```

## Type Design

### Sealed Classes
Seal classes by default unless designed for inheritance:

```csharp
// Good - Sealed for performance
public sealed class CircularBuffer<T> : IDisposable

// Only when inheritance is intended
public abstract class Component
```

### Struct vs Class
- **Structs** for small, immutable data (<= 16 bytes recommended)
- **Classes** for objects with identity, mutable state, or larger than 16 bytes
- **Readonly structs** for immutable value types

```csharp
// Good - Small immutable data
public readonly struct EntityMeta
{
    public readonly int TableId;
    public readonly int Row;

    public EntityMeta(int tableId, int row)
    {
        TableId = tableId;
        Row = row;
    }
}

// Good - Object with identity
public sealed class Table : IDisposable
```

### IDisposable Pattern
Implement proper disposal for unmanaged resources:

```csharp
public sealed class ResourceHolder : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (!_disposed)
        {
            // Dispose managed resources
            // Free unmanaged resources
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~ResourceHolder()
    {
        Dispose();
    }
}
```

## Testing

- Write unit tests for all core functionality
- Use descriptive test method names following the pattern: `MethodName_Scenario_ExpectedBehavior`
- Keep tests isolated and independent
- Use object pools in tests to match production behavior

```csharp
[Test]
public void Push_WhenBufferFull_ExpandsCapacity()
{
    // Arrange
    var buffer = new CircularBuffer<int>(2);

    // Act
    buffer.Push(1);
    buffer.Push(2);
    buffer.Push(3); // Should expand

    // Assert
    Assert.AreEqual(3, buffer.Count);
}
```

## Avoid Anti-Patterns

### Don't Do These

1. **Boxing in hot paths**
   ```csharp
   // Bad
   object boxed = valueType; // Avoid boxing
   ```

2. **String concatenation in loops**
   ```csharp
   // Bad
   string result = "";
   foreach (var item in items)
       result += item.ToString();

   // Good
   var sb = new StringBuilder();
   foreach (var item in items)
       sb.Append(item);
   ```

3. **Using `var` when type is not obvious**
   ```csharp
   // Bad
   var result = GetValue(); // What type is this?

   // Good
   int result = GetValue(); // Clear type
   ```

4. **Capturing variables in lambda closures unnecessarily**
   ```csharp
   // Bad - Allocates closure
   items.Where(x => x.Value > threshold);

   // Good - Use local function or method
   bool Predicate(Item x) => x.Value > threshold;
   items.Where(Predicate);
   ```

5. **Using `this.` prefix unnecessarily**
   ```csharp
   // Bad
   this._field = value;

   // Good
   _field = value;
   ```

## Project-Specific Patterns

### Storage Types
Use `StorageType` for type identification in ECS:
```csharp
var type = StorageType.Create<T>();
if (type.IsValueType && !type.IsTag)
{
    // Handle value component
}
```

### Identity and Entity
Always pass `Identity` by value (it's a small struct):
```csharp
public void ProcessEntity(Identity identity) // Not ref Identity
```

### Pooled Collections
Use `PooledList<T>` for temporary allocations:
```csharp
using var types = new PooledList<StorageType>(32);
types.Value.Add(type);
// Automatically returned to pool on dispose
```

## Code Review Checklist

Before submitting code, verify:

- [ ] All public APIs have XML documentation
- [ ] Naming conventions follow the style guide
- [ ] No boxing/unboxing in hot paths
- [ ] Appropriate use of Span/Memory for zero-allocation scenarios
- [ ] Object pooling used for frequently allocated objects
- [ ] Proper disposal of unmanaged resources
- [ ] File-scoped namespaces used
- [ ] No LINQ in performance-critical code
- [ ] Defensive checks (Debug.Assert, argument validation)
- [ ] Unity/NET conditional compilation where needed
- [ ] Tests added for new functionality
- [ ] No meta files generated (specified in user's requirements)

## Additional Resources

- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [.NET Performance Tips](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
- [Unity Best Practices](https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity.html)
- [Data-Oriented Design](https://www.dataorienteddesign.com/dodbook/)
