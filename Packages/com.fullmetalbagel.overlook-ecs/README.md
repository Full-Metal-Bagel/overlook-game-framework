# Overlook ECS

High-performance Entity Component System (ECS) for the Overlook Game Framework. Built with C# and optimized for Unity development workflows using data-oriented design principles.

## Installation

### Unity Package Manager

```
https://github.com/fullmetalbagel/overlook-game-framework.git?path=Packages/com.fullmetalbagel.overlook-ecs
```

### NuGet

```bash
dotnet add package Overlook.Ecs
```

## Overview

Entity Component System (ECS) is an architectural pattern that emphasizes data-oriented design by separating data (Components) from identity (Entities) and behavior (Systems). This approach promotes:

*   **Improved Performance:** Cache-friendly data layout through archetype-based storage
*   **Enhanced Code Reusability:** Components and systems are modular and independent
*   **Better Data Organization:** Clear separation of concerns for complex game states

`Overlook.Ecs` provides a robust implementation of these core principles.

## Core Concepts in Overlook.Ecs

*   **`World`**: The central container and manager for all ECS data. It holds entities, archetypes, and provides the primary API for interacting with the ECS (e.g., spawning entities, creating queries).
    *   Each `World` is an isolated ECS environment.
*   **`Entity`**: A lightweight `struct` that uniquely identifies a game object or logical element. It's essentially an ID (`Identity`) that groups a set of components.
    *   `Entity.None` and `Entity.Any` are special static values.
*   **Component**: Data structures (primarily `struct` for unmanaged components, but `class` for object components) that define the attributes or state of an entity. Examples: `Position`, `Velocity`, `Health`.
    *   **Unmanaged Components**: `struct` components offer high performance due to direct memory layout within archetypes.
    *   **Object Components**: `class` components allow for reference types and more complex behaviors, managed separately but still associated with entities.
    *   **Tagged Components (`ITaggedComponent`)**: A specialized mechanism for associating components with a "tag" type, allowing for more nuanced component relationships and querying (e.g., an `Equipment` component tagged with an `EquippedByPlayer` tag).
*   **`Archetypes`**: An internal system that manages the unique combinations of component types. Entities with the exact same set of components belong to the same archetype. This is crucial for:
    *   **Efficient Storage**: Components of the same type within an archetype are often stored contiguously in memory (`Table`, `TableStorage`).
    *   **Fast Querying**: Queries can quickly identify matching archetypes and then iterate over their entities.
*   **`StorageType`**: Represents the type of a component, used internally for managing storage and type information.

## Key Features

*   **High-Performance Design:**
    *   Archetype-based component storage for cache efficiency.
    *   Focus on `unmanaged` (struct) components for performance-critical data.
    *   Object pooling (`Overlook.Pool`) is used internally (e.g., in `EntityBuilder` and `Archetypes`) to reduce allocations.
    *   Uses `Mask` (or `NativeBitArrayMask` if `OVERLOOK_ECS_USE_UNITY_COLLECTION` is defined) for efficient component type matching in queries.
*   **Flexible Entity Management:**
    *   `world.Spawn()`: Creates a new entity.
    *   `world.Despawn(entity)`: Destroys an entity and its components.
    *   Component Addition: `world.AddComponent<T>(entity, data)` for unmanaged, `world.AddObjectComponent<T>(entity, data)` for managed.
    *   Component Removal: `world.RemoveComponent<T>(entity)`, `world.RemoveObjectComponent<T>(entity)`.
    *   Component Access: `world.GetComponent<T>(entity)` (ref return for unmanaged), `world.GetObjectComponent<T>(entity)`.
    *   Checking for components: `world.HasComponent<T>(entity)`.
*   **Powerful Querying System:**
    *   `QueryBuilder`: Fluent API to define which components an entity must have (`Has<T>`), must not have (`Not<T>`), or could have (`Any<T>`).
    *   `query.Build(world)`: Constructs a `Query` object.
    *   Iterate over query results: `foreach (var queryEntity in query) { ... }`.
    *   `QueryEntity`: Provides access to the `Entity` and its components within a query loop (e.g., `queryEntity.Get<Position>()`).
    *   `WhereQuery`: Allows for additional filtering on query results using predicates.
*   **Entity Construction with `EntityBuilder`:**
    *   While `World` provides direct methods, `EntityBuilder` (and its extensions `ValueComponentBuilder`, `ObjectComponentBuilder`) offers a composable, fluent way to define an entity's components before instantiation.
    *   `EntityBuilder.Create().Add(new Position { ... }).Add(new Velocity { ... }).Build(world);`
*   **Component Grouping (`ComponentGroupAttribute`):**
    *   An assembly-level attribute to define logical groupings of component types. This can be used for editor tooling or framework-level conventions.
*   **Tagged Components (`TaggedComponent` extensions):**
    *   Provides a way to create and query components that are "tagged" by another type, offering a form of component relationship or categorization.
    *   Example: `world.AddTaggedComponent(entity, myRenderer, typeof(PlayerViewTag));`

## Basic Usage Examples

```csharp
using Overlook.Ecs;

// Define your components
public struct Position // Assuming IComponentData or similar if you have one
{
    public float X, Y, Z;
}

public struct Velocity
{
    public float DX, DY, DZ;
}

public class EnemyAI // Example of a class component
{
    public float AggroRadius;
}

public struct IsPlayerTag { } // A simple unmanaged tag component

public class GameController
{
    private World _world;

    public void Initialize()
    {
        _world = new World();

        // === Entity Creation ===

        // Method 1: Direct with World API
        Entity playerEntity = _world.Spawn();
        _world.AddComponent(playerEntity, new Position { X = 0, Y = 1, Z = 0 });
        _world.AddComponent(playerEntity, new Velocity { DX = 1, DY = 0, DZ = 0 });
        _world.AddComponent<IsPlayerTag>(playerEntity); // Adding a tag

        Entity enemyEntity = _world.Spawn();
        _world.AddComponent(enemyEntity, new Position { X = 10, Y = 1, Z = 5 });
        _world.AddObjectComponent(enemyEntity, new EnemyAI { AggroRadius = 15f });

        // Method 2: Using EntityBuilder (conceptual, actual API from EntityBuilderExtensions)
        // Note: The direct EntityBuilder methods like Add<T>() are part of extensions.
        // The following is a more realistic use of how builders are chained.
        var builder = Overlook.Ecs.EntityBuilder.Create()
            .Add(new Position { X = 5, Y = 0, Z = 5 })
            .Add(new Velocity { DX = 0, DY = 0, DZ = -1 });
        Entity anotherEntity = builder.Build(_world);

        // === Querying Entities ===

        // Query for all entities with Position and Velocity
        var moveableEntitiesQuery = QueryBuilder.Create()
                                       .Has<Position>()
                                       .Has<Velocity>()
                                       .Build(_world);

        Console.WriteLine("Moveable Entities:");
        foreach (var queryEntity in moveableEntitiesQuery) // queryEntity is of type QueryEntity
        {
            ref var pos = ref queryEntity.Get<Position>(); // Get by ref for structs
            ref readonly var vel = ref queryEntity.Get<Velocity>(); // Can also get as readonly ref

            Console.WriteLine($"  Entity {queryEntity.Entity.Identity} at ({pos.X}, {pos.Y}, {pos.Z}) moving at ({vel.DX}, {vel.DY}, {vel.DZ})");

            // Modify component data directly
            pos.X += vel.DX * 0.1f; // Example: simple movement update
        }

        // Query for entities with the IsPlayerTag
        var playerQuery = QueryBuilder.Create().Has<IsPlayerTag>().Build(_world);
        foreach (var queryEntity in playerQuery)
        {
            if (_world.HasComponent<Position>(queryEntity.Entity)) // Check for other components
            {
                ref var playerPos = ref _world.GetComponent<Position>(queryEntity.Entity);
                Console.WriteLine($"Player {queryEntity.Entity.Identity} is at: ({playerPos.X}, {playerPos.Y}, {playerPos.Z})");
            }
        }

        // === Accessing/Modifying Components ===
        if (_world.IsAlive(playerEntity) && _world.HasComponent<Velocity>(playerEntity))
        {
            ref var playerVel = ref _world.GetComponent<Velocity>(playerEntity);
            playerVel.DX = 5f; // Change player velocity
            Console.WriteLine($"Updated player velocity X to {playerVel.DX}");
        }

        // === Despawning ===
        _world.Despawn(enemyEntity);
        Console.WriteLine($"Enemy {enemyEntity.Identity} despawned. IsAlive: {_world.IsAlive(enemyEntity)}");
    }

    public void Shutdown()
    {
        _world?.Dispose(); // Important to dispose the world to clean up resources
    }
}
```

## Systems (Behavior)

`Overlook.Ecs` primarily provides the data container and manipulation APIs. The actual game logic (behavior) is typically implemented in **Systems**. Systems are classes or methods that:

1.  Query the `World` for entities with specific combinations of components.
2.  Iterate over these entities and update their components or perform other game logic.

This package focuses on the ECS core; you would build your systems on top of it.

## Further Considerations

*   **Integration with Unity:** While a general-purpose C# ECS, its design choices (like `NativeBitArrayMask` option) suggest an orientation towards Unity's performance-sensitive environment. Consider how this integrates with Unity's Job System and Burst Compiler for maximal performance if applicable.
*   **Error Handling and Debugging:** The codebase includes `Debug.Assert` and conditional debug messages (`OVERLOOK_ECS_DEBUG`), which are helpful during development.

This README provides a foundational understanding of `Overlook.Ecs`. For more in-depth knowledge, refer to the source code and specific API documentation within the package.

## Acknowledgments

- Modified from [RelEcs](https://github.com/Byteron/RelEcs)

## License

MIT License - see the [LICENSE](../../LICENSE) file for details.