using System;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace Overlook.Ecs.Tests
{
    [TestFixture]
    public class BuilderEnhancementsTests
    {
        private World _world;

        [SetUp]
        public void Setup()
        {
            _world = new World();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void AddDefaultValue_Type_ComponentAddedWithDefaultValue()
        {
            // Create an entity using the AddDefaultValue method with a specific type
            var entity = EntityBuilder.Create()
                .AddDefaultValue(typeof(Position))
                .Build(_world);
                
            // Component should exist with default values
            Assert.That(_world.HasComponent<Position>(entity), Is.True);
            var position = _world.GetComponent<Position>(entity);
            Assert.That(position.X, Is.EqualTo(0));
            Assert.That(position.Y, Is.EqualTo(0));
        }
        
        [Test]
        public void GetOrCreateObject_WhenObjectDoesNotExist_CreatesObject()
        {
            // Arrange
            var entity = _world.Spawn();
            var archetypes = _world.Archetypes;
            var builder = new ArchetypesBuilder(archetypes);
            
            // Act
            var component = builder.GetOrCreateObject<TestComponent>(entity.Identity);
            
            // Assert
            Assert.That(component, Is.Not.Null);
            Assert.That(_world.HasComponent<TestComponent>(entity), Is.True);
            Assert.That(_world.GetComponent<TestComponent>(entity), Is.EqualTo(component));
        }
        
        [Test]
        public void GetOrCreateObject_WhenObjectExists_ReturnsExistingObject()
        {
            // Arrange
            var entity = _world.Spawn();
            var component = _world.AddObjectComponent<TestComponent>(entity);
            var archetypes = _world.Archetypes;
            var builder = new ArchetypesBuilder(archetypes);
            
            // Act
            var retrievedComponent = builder.GetOrCreateObject<TestComponent>(entity.Identity);
            
            // Assert
            Assert.That(retrievedComponent, Is.SameAs(component));
        }
        
        [Test]
        public void DynamicBuilder_AddDynamicRawData_ComponentAddedWithCorrectValues()
        {
            // Arrange
            var position = new Position(42, 99);
            var positionType = typeof(Position);
            
            // Convert the Position struct to a byte array
            var bytes = StructToBytes(position);
            var memory = new ReadOnlyMemory<byte>(bytes);
            
            // Create a dynamic builder and add the raw data
            var builder = new DynamicBuilder();
            builder.AddDynamicRawData(memory, positionType);
            
            // Act
            var entity = builder.Build(_world);
            
            // Assert
            Assert.That(_world.HasComponent<Position>(entity), Is.True);
            var retrievedPosition = _world.GetComponent<Position>(entity);
            Assert.That(retrievedPosition.X, Is.EqualTo(42));
            Assert.That(retrievedPosition.Y, Is.EqualTo(99));
        }

        [Test]
        public void AddDefaultValue_PrimitiveType_ComponentAddedWithDefaultValue()
        {
            // Create an entity using the AddDefaultValue method with UnmanagedComponent
            var entity = EntityBuilder.Create()
                .AddDefaultValue(typeof(UnmanagedComponent))
                .Build(_world);
                
            // Component should exist with default values
            Assert.That(_world.HasComponent<UnmanagedComponent>(entity), Is.True);
            var component = _world.GetComponent<UnmanagedComponent>(entity);
            Assert.That(component.X, Is.EqualTo(0f));
            Assert.That(component.Y, Is.EqualTo(0f));
            Assert.That(component.Z, Is.EqualTo(0f));
        }
        
        [Test]
        public void Query_Contains_WithNonExistingEntity_ReturnsFalse()
        {
            // Prepare a query
            var query = QueryBuilder.Create()
                .Has<Position>()
                .Build(_world);
                
            // Create a non-existing entity
            var nonExistingEntity = new Entity(new Identity(999, 1));
            
            // Query.Contains should safely return false for non-existing entities
            Assert.That(query.Contains(nonExistingEntity), Is.False);
        }
        
        // Helper method to convert a struct to byte array
        private static byte[] StructToBytes<T>(T structure) where T : struct
        {
            var size = Marshal.SizeOf(structure);
            var bytes = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            
            try
            {
                Marshal.StructureToPtr(structure, ptr, false);
                Marshal.Copy(ptr, bytes, 0, size);
                return bytes;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        
        // Test component class
        private class TestComponent
        {
            public string Name { get; set; } = "Default";
        }
    }
} 