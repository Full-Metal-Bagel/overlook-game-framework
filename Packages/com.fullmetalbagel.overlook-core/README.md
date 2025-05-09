# Overlook Core

`Overlook Core` is a foundational package for the Overlook Game Framework. It provides essential low-level utilities, data structures, and extensions designed to enhance game development within the Unity environment.

## Key Features

*   **Concurrent Collections:** Thread-safe collections like `ConcurrentHashSet` and `ConcurrentQueue` for robust multi-threaded operations.
*   **Debugging Utilities:** Custom `Debug` class for enhanced logging and debugging capabilities.
*   **Unmanaged Type Extensions:** Helper methods for working with unmanaged types, potentially for performance-critical scenarios.
*   **Reference Equality Comparer:** A utility for comparing objects based on their reference, rather than value.
*   **Compiler & Language Features:** Includes support for modern C# features like `IsExternalInit` for init-only properties and attributes like `DisallowDefaultConstructor`.

## Purpose

This package serves as the bedrock for other Overlook Game Framework modules, offering a common set of tools and functionalities to ensure consistency, performance, and reliability across the framework.
