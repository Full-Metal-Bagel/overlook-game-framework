# Overlook.Pool - Object Pooling Framework for Unity and .NET

Overlook.Pool is a high-performance, flexible, and easy-to-use object pooling framework designed for Unity and general .NET applications. It helps manage object lifecycles efficiently, reducing garbage collection overhead and improving application performance by reusing objects instead of constantly creating and destroying them.

## Features

-   **Generic Object Pooling**: Easily pool any class type.
-   **Policy-Based Configuration**: Customize pool behavior (initial size, max size, expansion logic, object creation/destruction) using `IObjectPoolPolicy`.
-   **Static Access**: Convenient static access to pools via `StaticPools.GetPool<T>()`.
-   **Provider Model**: Extensible provider model (`IObjectPoolProvider`) for advanced scenarios and integration with dependency injection frameworks.
-   **Attribute-Based Configuration**: Override pool policies for specific types or generic collections using assembly or class attributes.
-   **Pooled Collections**:
    -   `PooledObject<T>`: A `ref struct` for managing a single pooled object with `IDisposable` pattern for automatic recycling.
    -   `PooledList<T>`: `ref struct` wrapper for `List<T>`.
    -   `PooledDictionary<TKey, TValue>`: `ref struct` wrapper for `Dictionary<TKey, TValue>`.
    -   `PooledHashSet<T>`: `ref struct` wrapper for `HashSet<T>`.
    -   `PooledStringBuilder`: `ref struct` wrapper for `StringBuilder`.
    -   `PooledArray<T>`: `ref struct` wrapper for arrays rented from `ArrayPool<T>.Shared`.
    -   `PooledMemoryList<T>`: `ref struct` wrapper for `List<T>` (often used for byte buffers or similar).
    -   `PooledMemoryDictionary<TKey, TValue>`: `ref struct` wrapper for `Dictionary<TKey, TValue>`.
-   **Thread Safety**: Core `ObjectPool` is designed with thread safety in mind for concurrent rent and recycle operations.
-   **Callbacks**: `IObjectPoolCallback` interface (`OnRent`, `OnRecycle`) for objects to react to pooling events.
-   **Disposable Support**: Objects implementing `IDisposable` are automatically disposed when the pool is disposed or when objects are discarded due to pool limits.
-   **Debug Features**: Conditional `OVERLOOK_DEBUG` compilation symbol enables:
    -   Leak tracking for pooled objects (logs a warning if an object is not recycled).
    -   Provider mismatch exceptions if attempting to register a different provider for an already pooled type via `TypeObjectPoolCache`.

## Installation

### Unity Package Manager

```
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-pool
```

### NuGet

```bash
dotnet add package Overlook.Pool
```

## Core Components

### `ObjectPool<T, TPolicy>`
The main class for object pooling.
-   `T`: The type of object to pool.
-   `TPolicy`: A struct implementing `IObjectPoolPolicy` that defines the pool's behavior.

### `IObjectPoolPolicy`
Interface for defining pooling strategies.
```csharp
public interface IObjectPoolPolicy
{
    object Create(); // Creates a new instance of the pooled object.
    public int InitCount => 0; // Initial number of objects to create.
    public int MaxCount => int.MaxValue >> 2; // Maximum number of objects to store in the pool.
    public int Expand(int currentSize) => currentSize * 2; // Logic to determine how many new objects to create when expanding.
    void OnRent(object instance); // Called when an object is rented from the pool.
    void OnRecycle(object instance); // Called when an object is returned to the pool.
    void OnDispose(object instance); // Called when an object is discarded (e.g., pool is full or disposed).
}
```
Objects can implement `IObjectPoolCallback` and/or `IDisposable` to hook into `OnRent`, `OnRecycle`, and `OnDispose` calls automatically.

### `StaticPools`
Provides static access to shared object pools.
-   `StaticPools.GetPool<T>()`: Retrieves or creates a pool for type `T` using a default policy or a policy registered via attributes.
-   `StaticPools.GetPool(Type type)`: Non-generic version.
-   `StaticPools.Clear()`: Disposes all cached pools.

### `IObjectPool` and `IObjectPool<T>`
Interfaces for interacting with object pools.
-   `Rent()`: Get an object from the pool.
-   `Recycle(T instance)`: Return an object to the pool.
-   `InitCount`, `MaxCount`, `RentedCount`, `PooledCount`: Properties to inspect pool state.
-   `Dispose()`: Clears the pool and disposes pooled objects if they implement `IDisposable`.

## Basic Usage

### Simple Object Pooling

```csharp
public class MyPooledObject : IObjectPoolCallback, IDisposable
{
    public string Data { get; set; }
    private bool _isDisposed = false;

    public MyPooledObject()
    {
        // Constructor
        Console.WriteLine("MyPooledObject Created");
    }

    public void OnRent()
    {
        Console.WriteLine("MyPooledObject Rented");
        Data = "Default Data"; // Reset state on rent
    }

    public void OnRecycle()
    {
        Console.WriteLine("MyPooledObject Recycled");
        Data = null; // Clean up state on recycle
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            Console.WriteLine("MyPooledObject Disposed");
            _isDisposed = true;
            // Release unmanaged resources if any
        }
    }
}

// Get a pool for MyPooledObject (uses default policy)
IObjectPool<MyPooledObject> pool = StaticPools.GetPool<MyPooledObject>();

// Rent an object
MyPooledObject obj1 = pool.Rent();
obj1.Data = "Hello from Pool";
Console.WriteLine(obj1.Data);

// Recycle the object
pool.Recycle(obj1);

// Rent again (might get the same instance, state reset by OnRent)
MyPooledObject obj2 = pool.Rent();
Console.WriteLine(obj2.Data); // Should be "Default Data"
pool.Recycle(obj2);
```

### Using `PooledObject<T>` (Recommended for `IDisposable`-like pattern)

`PooledObject<T>` is a `ref struct` that manages the lifecycle of a pooled object. It automatically recycles the object when it goes out of scope (e.g., at the end of a `using` block).

```csharp
// MyPooledObject class defined as above

void ProcessWithPooledObject()
{
    using (var pooledObj = new PooledObject<MyPooledObject>())
    {
        MyPooledObject instance = pooledObj.Value; // or simply: MyPooledObject instance = pooledObj;
        instance.Data = "Using PooledObject";
        Console.WriteLine(instance.Data);
        // instance is automatically recycled when pooledObj is disposed
    }
    // At this point, the MyPooledObject instance has been returned to the pool.
}
```

### Using `PooledList<T>`

```csharp
void ProcessWithPooledList()
{
    using (var pooledList = new PooledList<int>(capacity: 50))
    {
        List<int> list = pooledList.Value; // or: List<int> list = pooledList;
        for (int i = 0; i < 10; i++)
        {
            list.Add(i * i);
        }
        Console.WriteLine($"List count: {list.Count}, Capacity: {list.Capacity}");
        // list is automatically recycled (and cleared) when pooledList is disposed
    }
}
```
Other pooled collections (`PooledDictionary`, `PooledHashSet`, `PooledStringBuilder`, etc.) follow a similar pattern.

## Customization

### Custom Pool Policy

Define a struct implementing `IObjectPoolPolicy`:

```csharp
public struct MyCustomPolicy : IObjectPoolPolicy
{
    public int InitCount => 5;        // Start with 5 objects
    public int MaxCount => 20;       // Pool can hold up to 20 objects
    public int Expand(int size) => size + 5; // Add 5 objects when expanding

    public object Create() => new MyPooledObject();

    // Optional: Implement OnRent, OnRecycle, OnDispose if needed,
    // otherwise default behavior (calling IObjectPoolCallback/IDisposable) is used.
    public void OnRent(object instance)
    {
        (instance as IObjectPoolCallback)?.OnRent();
        // Custom rent logic
    }

    public void OnRecycle(object instance)
    {
        (instance as IObjectPoolCallback)?.OnRecycle();
        // Custom recycle logic
    }
    
    public void OnDispose(object instance)
    {
        (instance as IDisposable)?.Dispose();
        // Custom dispose logic
    }
}
```

To use this custom policy directly:
```csharp
var customPool = new ObjectPool<MyPooledObject, MyCustomPolicy>();
MyPooledObject obj = customPool.Rent();
// ...
customPool.Recycle(obj);
```

### Attribute-Based Policy Override

#### For a Specific Type:
```csharp
// In AssemblyInfo.cs or any C# file, at the assembly level:
[assembly: Overlook.Pool.OverridePoolPolicy(typeof(MyPooledObject), typeof(MyCustomPolicy))]
```
Now, `StaticPools.GetPool<MyPooledObject>()` will use `MyCustomPolicy`.

#### For a Generic Collection Type:
```csharp
// Example: Custom policy for all List<string>
public struct ListStringPolicy : IObjectPoolPolicy
{
    public int InitCount => 2;
    public int MaxCount => 10;
    public object Create() => new List<string>();
    // Expand, OnRent, OnRecycle, OnDispose as needed
}

// In AssemblyInfo.cs or any C# file:
[assembly: Overlook.Pool.OverrideGenericCollectionPoolPolicy(typeof(List<>), typeof(ListStringPolicy))]
// Note: This example assumes a way to specify List<string> or that the policy handles the generic type arg.
// The actual attribute `OverrideGenericCollectionPoolPolicy` takes the open generic type and a policy type.
// The policy itself or the framework's provider needs to handle the instantiation for the specific closed generic type.
// Overlook.Pool's GenericCollectionPoolProvider handles creating instances like List<T> and clearing them.
```

#### On a Class:
```csharp
[Overlook.Pool.PoolPolicy(typeof(MyCustomPolicy))]
public class AnotherPooledObject
{
    // ...
}
```
When `ObjectPoolProvider.Get<AnotherPooledObject>()` is called (which `StaticPools` uses internally), it will use `MyCustomPolicy`.

## Providers

The framework uses an `IObjectPoolProvider` system to create pools.
-   `ObjectPoolProvider.Get<T>()` or `ObjectPoolProvider.Get(Type type)`: Gets a provider for the specified type. This considers attributes and registered assembly factories.
-   `DefaultObjectPoolProvider<T>`: The default provider used if no custom provider or policy is found.
-   `CustomObjectPoolProvider<T, TPolicy>`: A provider that creates an `ObjectPool<T, TPolicy>`.
-   `GenericCollectionPoolProvider<TCollection, TElement, TPolicy>`: Handles pooling of generic collections like `List<T>`.

You can implement `IObjectPoolProviderFactory` or `IAssemblyObjectPoolProviderFactory` to create custom logic for selecting or creating providers.

## Thread Safety

-   The `ObjectPool<T, TPolicy>` class is designed to be thread-safe for `Rent` and `Recycle` operations. It uses `System.Collections.Concurrent.ConcurrentQueue<T>` internally and `Interlocked` for counter updates.
-   The `StaticPools` class and `TypeObjectPoolCache` also use `ConcurrentDictionary` for thread-safe caching of pools and providers.
-   When implementing custom policies or object callbacks, ensure your custom code is also thread-safe if the pool will be accessed from multiple threads.

## Debugging (`OVERLOOK_DEBUG`)

When the `OVERLOOK_DEBUG` compilation symbol is defined (e.g., via `csc.rsp` in Unity or project settings):

1.  **Leak Tracking**: If an object rented from `ObjectPool<T, TPolicy>` is garbage collected without being recycled, a warning message with a stack trace of where the object was rented will be logged. This is extremely helpful for finding pool usage errors.
2.  **Provider Mismatch Check**: `TypeObjectPoolCache` will throw a `ProviderNotMatchException` if you attempt to get a pool for a type with a specific provider, and then later attempt to get a pool for the *same type* but with a *different* provider instance. This helps catch configuration errors.

### Enabling `OVERLOOK_DEBUG` in Unity:

1.  Create a `csc.rsp` file in your `Assets` directory (or the root of the Overlook.Pool package if modifying the package directly).
2.  Add the following line to `csc.rsp`:
    ```
    -define:OVERLOOK_DEBUG
    ```
    Unity will pick this up during the next compilation.

## Pooled Collections In-Depth

These `ref struct` types are designed to provide an `IDisposable`-like pattern for pooled collections, ensuring they are returned to the pool when they go out of scope.

-   **`PooledObject<T> where T : class, new()`**
    -   Rents an object of type `T` on construction.
    -   Recycles the object on `Dispose()`.
    -   Implicitly convertible to `T`.
    ```csharp
    using(var pObj = new PooledObject<MyClass>()) {
        MyClass instance = pObj; // Use instance
    } // instance is recycled here
    ```

-   **`PooledList<T>`**, **`PooledDictionary<TKey, TValue>`**, **`PooledHashSet<T>`**, **`PooledStringBuilder`**
    -   Rent the respective collection type (`List<T>`, `Dictionary<TKey, TValue>`, etc.) on construction.
    -   Have constructors that accept an initial capacity. Also have parameterless constructors.
    -   Recycle the collection (which also typically clears it) on `Dispose()`.
    -   Implicitly convertible to the underlying collection type.
    ```csharp
    using(var pList = new PooledList<int>(100)) { // or new PooledList<int>()
        List<int> list = pList;
        list.Add(1);
    } // list is recycled and cleared here
    ```

-   **`PooledArray<T>`**
    -   Uses `ArrayPool<T>.Shared.Rent()` on construction.
    -   Returns the array to `ArrayPool<T>.Shared.Return()` on `Dispose()`.
    -   This is for performance-critical scenarios involving arrays, especially large ones, to avoid GC pressure from array allocations. The rented array might be larger than requested and its contents are not cleared by default when returned.
    ```csharp
    using(var pArray = new PooledArray<byte>(1024)) {
        byte[] buffer = pArray;
        // Use buffer. Ensure you only use up to the length you need,
        // as ArrayPool might return a larger array.
    } // buffer is returned to ArrayPool here
    ```

-   **`PooledMemoryList<T>`** and **`PooledMemoryDictionary<TKey, TValue>`**
    -   These are similar to `PooledList` and `PooledDictionary` but are named to indicate they are often used in contexts dealing with memory management or byte buffers (though they pool standard `List<T>` and `Dictionary<TKey, TValue>`). They also feature parameterless constructors and constructors accepting an initial capacity.

## License

MIT License - see the [LICENSE](../../LICENSE) file for details.
