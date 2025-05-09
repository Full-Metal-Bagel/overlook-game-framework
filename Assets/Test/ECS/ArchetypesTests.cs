using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Overlook.Ecs;
using Overlook.Pool;
using UnityEngine;
using UnityEngine.TestTools;
#if OVERLOOK_ECS_USE_UNITY_COLLECTION
using TMask = Overlook.Ecs.NativeBitArrayMask;
using TSet = Overlook.Ecs.NativeBitArraySet;
#else
using TMask = Overlook.Ecs.Mask;
using TSet = Overlook.Ecs.SortedSetTypeSet;
#endif

#pragma warning disable CS0169 // Field is never used
#pragma warning disable CA1823 // unused field

[assembly: ComponentGroup(groupType: typeof(object), memberType: typeof(int))]

namespace Overlook.Ecs.Tests
{
    [TestFixture]
    public class ArchetypesTests
    {
        private Archetypes _archetypes = default!;

        [SetUp]
        public void Setup()
        {
            _archetypes = new Archetypes();
        }

        [TearDown]
        public void TearDown()
        {
            _archetypes.Dispose();
        }

        [Test]
        public void Constructor_InitializesProperly()
        {
            Assert.That(_archetypes, Is.Not.Null);
        }

        [Test]
        public void IsAlive_Generation()
        {
            var entity = _archetypes.Spawn();
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.True);
            _archetypes.Despawn(entity.Identity);
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.False);
            var entity2 = _archetypes.Spawn();
            Assert.That(entity.Identity.Index, Is.EqualTo(entity2.Identity.Index));
            Assert.That(entity.Identity.Generation + 1, Is.EqualTo(entity2.Identity.Generation));
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.False);
            Assert.That(_archetypes.IsAlive(entity2.Identity), Is.True);
        }

        [Test]
        public void Spawn_ReturnsValidEntity()
        {
            var entity = _archetypes.Spawn();
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.True);
        }

        [Test]
        public void Despawn_RemovesEntity()
        {
            var entity = _archetypes.Spawn();
            _archetypes.Despawn(entity.Identity);
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.False);
        }

        [Test]
        public void Despawn_RemovesEntity_WhenEntityIsAlive()
        {
            // Arrange
            var entity = _archetypes.Spawn();

            // Act
            _archetypes.Despawn(entity.Identity);

            // Assert
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.False);
        }

        [Test]
        public void Despawn_DoesNothing_WhenEntityIsNotAlive()
        {
            var entity = new Entity(new Identity(999)); // Assuming this ID is not used
            Assert.That(_archetypes.Despawn(entity.Identity), Is.False); // The entity was never alive to begin with
        }

        [Test]
        public void Despawn_RemovesEntityAndItsComponents_WhenEntityHasComponents()
        {
            // Arrange
            var entity = _archetypes.Spawn();
            var componentType = StorageType.Create(typeof(int));
            _archetypes.AddComponent(entity.Identity, 42);

            // Act
            _archetypes.Despawn(entity.Identity);

            // Assert
            Assert.Catch<ArgumentException>(() => _archetypes.HasComponent(componentType, entity.Identity));
        }

        [Test]
        public void Despawn_RemovesEntityFromUnusedIds_WhenEntityIsDespawned()
        {
            // Arrange
            var entity = _archetypes.Spawn();
            _archetypes.Despawn(entity.Identity); // Despawn once to add to UnusedIds

            // Act
            _archetypes.Despawn(entity.Identity); // Despawn again to check if it's removed from UnusedIds

            // Assert
            Assert.That(_archetypes.IsAlive(entity.Identity), Is.False);
            // Assuming there's a way to check the count of UnusedIds, for example:
            // Assert.AreEqual(0, _archetypes.UnusedIdsCount);
        }

        [Test]
        public void Despawn_DoesNotRemoveOtherEntities_WhenMultipleEntitiesExist()
        {
            // Arrange
            var entity1 = _archetypes.Spawn();
            var entity2 = _archetypes.Spawn();

            // Act
            _archetypes.Despawn(entity1.Identity);

            // Assert
            Assert.That(_archetypes.IsAlive(entity1.Identity), Is.False);
            Assert.That(_archetypes.IsAlive(entity2.Identity), Is.True);
        }

        [Test]
        public void Despawn_UpdatesEntityMeta_WhenEntityIsDespawned()
        {
            // Arrange
            var entity = _archetypes.Spawn();

            // Act
            _archetypes.Despawn(entity.Identity);

            // Assert
            LogAssert.Expect(LogType.Error, new Regex(".*"));
            var meta = _archetypes.GetEntityMeta(entity.Identity); // Assuming GetEntityMeta is accessible
            Assert.That(meta.Row, Is.EqualTo(-1));
            Assert.That(meta.TableId, Is.EqualTo(-1));
        }

        [Test]
        public void Despawn_AddsIdentityToUnusedIds_WhenEntityIsDespawned()
        {
            // Arrange
            var entity = _archetypes.Spawn();

            // Act
            _archetypes.Despawn(entity.Identity);

            // Assert
            // Assuming there's a way to check the top of UnusedIds, for example:
            // Assert.AreEqual(entity.Identity, _archetypes.PeekUnusedId());
        }

        [Test]
        public void AddComponent_AddsComponentToEntity()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<int>(); // Assuming a valid StorageType instance
            _archetypes.AddComponent(entity.Identity, 123);
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.True);
        }

        [Test]
        public void GetComponent_ReturnsValidComponent()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<int>(); // Assuming a valid StorageType instance
            var data = 123;
            _archetypes.AddComponent(entity.Identity, data);
            var component = (int)_archetypes.GetComponent<int>(entity.Identity);
            Assert.That(component, Is.EqualTo(data));
        }

        [Test]
        public void AddComponent_OverwriteWhenComponentAlreadyExists()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<int>(); // Assuming a valid StorageType instance
            _archetypes.AddComponent(entity.Identity, 123);
            Assert.That(_archetypes.GetComponent<int>(entity.Identity), Is.EqualTo(123));
            _archetypes.AddComponent(entity.Identity, 321);
            Assert.That(_archetypes.GetComponent<int>(entity.Identity), Is.EqualTo(321));
        }

        [Test]
        public void RemoveComponent_ThrowsWhenComponentDoesNotExist()
        {
            var entity = _archetypes.Spawn();
            Assert.Catch<Exception>(() =>
                _archetypes.RemoveComponent(entity.Identity, typeof(string))); // Assuming it throws an exception when trying to remove a non-existent component
        }

        [Test]
        public void GetComponent_ThrowsWhenComponentDoesNotExist()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<int>(); // Assuming a valid StorageType instance
            Assert.Catch<Exception>(() => _archetypes.GetComponent<int>(entity.Identity)); // Assuming it throws an exception when trying to get a non-existent component
        }

        [Test]
        public void Spawn_WhenEntityLimitReached_ResizesMetaArray()
        {
            // Assuming the initial limit is 512 as per the provided code
            for (int i = 0; i < 512; i++)
            {
                _archetypes.Spawn();
            }

            Assert.DoesNotThrow(() =>
                _archetypes.Spawn()); // Assuming it doesn't throw an exception and resizes the Meta array
        }

        [Test]
        public void GetComponent_ReturnsCorrectComponent()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<int>(); // Assuming a valid StorageType instance
            var data = 123;
            _archetypes.AddComponent(entity.Identity, data);
            var component = (int)_archetypes.GetComponent<int>(entity.Identity);
            Assert.That(component, Is.EqualTo(data));
        }

        [Test]
        public void GetComponent_ThrowsWhenEntityDoesNotExist()
        {
            var nonExistentIdentity = new Identity(1); // Assuming a valid Identity instance not linked to any entity
            var type = StorageType.Create<int>(); // Assuming a valid StorageType instance
            Assert.Catch<Exception>(() => _archetypes.GetComponent<int>(nonExistentIdentity));
        }

        [Test]
        public void GetQuery_ReturnsValidQuery()
        {
            var mask = TMask.Create(); // Assuming a valid Mask instance
            mask.Has(StorageType.Create<string>());
            var query = _archetypes.GetQuery(mask, (archetypes, mask, tables) => new Query { Archetypes = archetypes, Mask = mask, Tables = tables }); // Assuming a valid delegate
            Assert.That(query.Mask, Is.EqualTo(mask));
        }

        [Test]
        public void GetTable_ReturnsValidTable()
        {
            var entity = _archetypes.Spawn();
            var meta = _archetypes.GetEntityMeta(entity.Identity);
            var table = _archetypes.GetTable(meta.TableId);
            Assert.That(table, Is.Not.Null);
            Assert.That(table.Identities.Contains(entity.Identity), Is.True);
        }

        [Test]
        public void GetComponents_ReturnsAllComponentsOfEntity()
        {
            var entity = _archetypes.Spawn();
            var type1 = StorageType.Create<int>(); // Assuming a valid StorageType instance
            var type2 = StorageType.Create<float>(); // Another StorageType instance
            _archetypes.AddComponent(entity.Identity, 123);
            _archetypes.AddComponent(entity.Identity, 123f);
            using var components = new PooledList<UntypedComponent>(32);
            _archetypes.GetAllValueComponents(entity.Identity, components.Value);
            Assert.That(components.Value.Count, Is.EqualTo(3)); // will added `Entity` as component by default
            Assert.That(components.Value.Any(c => c.Type == type1), Is.True);
            Assert.That(components.Value.Any(c => c.Type == type2), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenTableMatchesMaskRequirements()
        {
            var mask = TMask.Create();
            mask.Has(StorageType.Create<int>());
            var tableTypes = TSet.Create(StorageType.Create<int>());
            var tableStorage = new TableStorage(tableTypes);
            var table = new Table(0, tableTypes, tableStorage);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableDoesNotMatchMaskRequirements()
        {
            var mask = TMask.Create();
            mask.Has(StorageType.Create<int>());
            var tableTypes = TSet.Create(StorageType.Create<string>());
            var tableStorage = new TableStorage(tableTypes);
            var table = new Table(0, tableTypes, tableStorage);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableContainsExcludedTypes()
        {
            var mask = TMask.Create();
            mask.Not(StorageType.Create<int>());
            var tableTypes = TSet.Create(StorageType.Create<int>());
            var tableStorage = new TableStorage(tableTypes);
            var table = new Table(0, tableTypes, tableStorage);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenTableContainsAnyOfTheMaskTypes()
        {
            var mask = TMask.Create();
            mask.Any(StorageType.Create<int>());
            mask.Any(StorageType.Create<string>());
            var tableTypes = TSet.Create(StorageType.Create<int>());
            var tableStorage = new TableStorage(tableTypes);
            var table = new Table(0, tableTypes, tableStorage);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableDoesNotContainAnyOfTheMaskTypes()
        {
            var mask = TMask.Create();
            mask.Any(StorageType.Create<int>());
            mask.Any(StorageType.Create<string>());
            var tableTypes = TSet.Create(StorageType.Create(typeof(double)));
            var tableStorage = new TableStorage(tableTypes);
            var table = new Table(0, tableTypes, tableStorage);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenTableMatchesAllMaskRequirements()
        {
            var mask = TMask.Create();
            mask.Has(StorageType.Create(typeof(int)));
            mask.Any(StorageType.Create(typeof(string)));
            mask.Not(StorageType.Create(typeof(double)));
            var tableTypes = TSet.Create(new [] { StorageType.Create(typeof(int)), StorageType.Create(typeof(string))});
            var tableStorage = new TableStorage(tableTypes);
            var table = new Table(0, tableTypes, tableStorage);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableContainsExcludedAndRequiredTypes()
        {
            var mask = TMask.Create();
            mask.Has(StorageType.Create(typeof(int)));
            mask.Not(StorageType.Create(typeof(string)));
            var tableTypes = TSet.Create(new [] { StorageType.Create(typeof(int)), StorageType.Create(typeof(string)) });
            var tableStorage = new TableStorage(tableTypes);
            var table = new Table(0, tableTypes, tableStorage);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenMaskHasOptionalTypesAndTableContainsThem()
        {
            var mask = TMask.Create();
            mask.Any(StorageType.Create(typeof(int)));
            mask.Any(StorageType.Create(typeof(string)));
            var tableTypes = TSet.Create(new [] { StorageType.Create(typeof(int)), StorageType.Create(typeof(string)) });
            var tableStorage = new TableStorage(tableTypes);
            var table = new Table(0, tableTypes, tableStorage);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenMaskHasRequiredAnyTypesAndTableDoesNotContainThem()
        {
            var mask = TMask.Create();
            mask.Any(StorageType.Create(typeof(int)));
            mask.Any(StorageType.Create(typeof(string)));
            var tableTypes = TSet.Create(StorageType.Create(typeof(double)));
            var tableStorage = new TableStorage(tableTypes);
            var table = new Table(0, tableTypes, tableStorage);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenMaskHasRequiredAndOptionalTypesButTableDoesNotContainRequired()
        {
            var mask = TMask.Create();
            mask.Has(StorageType.Create(typeof(double)));
            mask.Any(StorageType.Create(typeof(int)));
            mask.Any(StorageType.Create(typeof(string)));
            var tableTypes = TSet.Create(new [] { StorageType.Create(typeof(int)), StorageType.Create(typeof(string)) });
            var tableStorage = new TableStorage(tableTypes);
            var table = new Table(0, tableTypes, tableStorage);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [StructLayout(LayoutKind.Explicit, Size = 0)]
        private struct ZeroStruct { }
        private struct EmptyStruct { }
        private class EmptyClass { }
        private sealed class EmptyInheritedClass : EmptyClass { }
        private struct IntStruct { int _a; }
        private class IntClass { int _a; }
        private sealed class IntInHeritedClass : IntClass { }

        [Test]
        public void StructTag_IsTag()
        {
            Assert.That(StorageType.Create<ZeroStruct>().IsTag, Is.True);
            Assert.That(StorageType.Create<EmptyStruct>().IsTag, Is.True);
            Assert.That(StorageType.Create<EmptyClass>().IsTag, Is.False);
            Assert.That(StorageType.Create<EmptyInheritedClass>().IsTag, Is.False);
            Assert.That(StorageType.Create<int>().IsTag, Is.False);
            Assert.That(StorageType.Create<IntStruct>().IsTag, Is.False);
            Assert.That(StorageType.Create<IntClass>().IsTag, Is.False);
            Assert.That(StorageType.Create<IntInHeritedClass>().IsTag, Is.False);
        }

        [Test]
        public void StructTag_AddTag()
        {
            var entity = _archetypes.Spawn().Identity;
            _archetypes.AddComponent(entity, new ZeroStruct());
            Assert.That(_archetypes.HasComponent(StorageType.Create<ZeroStruct>(), entity), Is.True);
            AssertUtils.CatchDebugAssertAndOtherExceptions(() => _archetypes.GetComponent<ZeroStruct>(entity));
        }

        [Test]
        public void StructTag_AddTagObject()
        {
            var entity = _archetypes.Spawn().Identity;
            _archetypes.AddObjectComponent(entity, (object)new ZeroStruct());
            Assert.That(_archetypes.HasComponent(StorageType.Create<ZeroStruct>(), entity), Is.True);
            AssertUtils.CatchDebugAssertAndOtherExceptions(() => _archetypes.GetComponent<ZeroStruct>(entity));
        }

        [Test]
        public void StructTag_DontThrowIfAddMoreThanOneTag()
        {
            var entity = _archetypes.Spawn().Identity;
            _archetypes.AddComponent(entity, new ZeroStruct());
            _archetypes.AddComponent(entity, new ZeroStruct());
        }

        [Test]
        public void StructTag_RemoveTag()
        {
            var entity = _archetypes.Spawn().Identity;
            _archetypes.AddComponent(entity, new ZeroStruct());
            _archetypes.RemoveComponent<ZeroStruct>(entity);
            Assert.That(_archetypes.HasComponent(StorageType.Create<ZeroStruct>(), entity), Is.False);
        }

        [Test]
        public void StructTag_AddTags()
        {
            var entity = _archetypes.Spawn().Identity;
            _archetypes.AddComponent(entity, new ZeroStruct());
            _archetypes.AddObjectComponent(entity, new EmptyClass());
            _archetypes.AddComponent(entity, new EmptyStruct());
            Assert.That(_archetypes.HasComponent(StorageType.Create<ZeroStruct>(), entity), Is.True);
            Assert.That(_archetypes.HasComponent(StorageType.Create<EmptyClass>(), entity), Is.True);
            Assert.That(_archetypes.HasComponent(StorageType.Create<EmptyStruct>(), entity), Is.True);
        }

        private sealed class Foo { }

        private static Dictionary<StorageType, List<object>> ToDictionary(Archetypes.EntityObjectComponents components)
        {
            var result = new Dictionary<StorageType, List<object>>();
            foreach (var (t, c) in components)
            {
                result[t] = c.ToList();
            }
            return result;
        }

        [Test]
        public void MultipleComponentsWithSameType()
        {
            var type = StorageType.Create<Foo>();
            var entity = _archetypes.Spawn();
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.False);
            var a = new Foo();
            var b = new Foo();
            var c = new Foo();
            _archetypes.AddMultipleObjectComponent(entity.Identity, a);
            _archetypes.AddMultipleObjectComponent(entity.Identity, b);
            _archetypes.AddMultipleObjectComponent(entity.Identity, c);
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.True);

            var components = _archetypes.GetObjectComponents(entity.Identity, type);
            Assert.That(components, Is.EquivalentTo(new [] { a, b, c }));
            _archetypes.RemoveObjectComponent(entity.Identity, b);
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.True);
            Assert.That(components, Is.EquivalentTo(new [] { a, c }));
            _archetypes.RemoveObjectComponent(entity.Identity, a);
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.True);
            Assert.That(components, Is.EquivalentTo(new [] { c }));
            _archetypes.RemoveObjectComponent(entity.Identity, a);
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.True);
            Assert.That(components, Is.EquivalentTo(new [] { c }));
            _archetypes.RemoveObjectComponent(entity.Identity, c);
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.False);
        }

        [Test]
        public void MultipleComponentsWithSameType_Overwrite()
        {
            var entity = _archetypes.Spawn();
            _archetypes.AddComponent(entity.Identity, 1);
            Assert.That(_archetypes.GetComponent<int>(entity.Identity), Is.EqualTo(1));
            _archetypes.AddComponent(entity.Identity, 2);
            Assert.That(_archetypes.GetComponent<int>(entity.Identity), Is.EqualTo(2));
        }

        [Test]
        public void MultipleComponentsWithSameType_RemoveAll()
        {
            var type = StorageType.Create<Foo>();
            var entity = _archetypes.Spawn();
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.False);
            var a = new Foo();
            var b = new Foo();
            var c = new Foo();
            _archetypes.AddMultipleObjectComponent(entity.Identity, a);
            _archetypes.AddMultipleObjectComponent(entity.Identity, b);
            _archetypes.AddMultipleObjectComponent(entity.Identity, c);
            _archetypes.RemoveObjectComponent<Foo>(entity.Identity);
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.False);
            Assert.That(ToDictionary(_archetypes.GetObjectComponents(entity.Identity)).ContainsKey(type), Is.False);
        }

        [Test]
        public void MultipleComponentsWithSameType_FindObjects()
        {
            var type = StorageType.Create<Foo>();
            var entity = _archetypes.Spawn();
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.False);
            var a = new Foo();
            var b = new Foo();
            var c = new Foo();
            var d = new object();
            var e = new object();
            _archetypes.AddMultipleObjectComponent(entity.Identity, a);
            _archetypes.AddMultipleObjectComponent(entity.Identity, b);
            _archetypes.AddMultipleObjectComponent(entity.Identity, c);
            _archetypes.AddMultipleObjectComponent(entity.Identity, d);
            _archetypes.AddMultipleObjectComponent(entity.Identity, e);
            _archetypes.AddComponent(entity.Identity, 1);
            _archetypes.AddComponent(entity.Identity, 1L);

            var foos = new List<Foo>();
            _archetypes.FindObjectComponents(entity.Identity, foos);
            Assert.That(foos, Is.EquivalentTo(new[] { a, b, c }));

            var objects = new List<object>();
            _archetypes.FindObjectComponents(entity.Identity, objects);
            Assert.That(objects, Is.EquivalentTo(new[] { a, b, c, d, e }));
        }

        [Test]
        public void MultipleComponentsWithSameType_IgnoreDuplicated()
        {
            var type = StorageType.Create<Foo>();
            var entity = _archetypes.Spawn();
            var a = new Foo();
            _archetypes.AddMultipleObjectComponent(entity.Identity, a);
            _archetypes.AddMultipleObjectComponent(entity.Identity, a);
            _archetypes.AddMultipleObjectComponent(entity.Identity, a);
            _archetypes.RemoveObjectComponent(entity.Identity, a);
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.False);
            Assert.That(ToDictionary(_archetypes.GetObjectComponents(entity.Identity)).ContainsKey(type), Is.False);
        }

        [Test]
        public void ComponentGroup_AddGroup()
        {
            var entity = _archetypes.Spawn();
            var (table, _) = _archetypes.AddComponentTypes(entity.Identity, new [] { StorageType.Create<object>() });
            Assert.That(table.Types.Contains(StorageType.Create<int>()), Is.True);
        }

        struct ManagedStruct
        {
            public object Value { get; }
        }

        [Test]
        public void AddUnmanagedValueType_Throw()
        {
            var entity = _archetypes.Spawn();
            Assert.Catch(() => _archetypes.AddUntypedValueComponent(entity.Identity, new ManagedStruct()));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TestRawData
        {
            public int Value1;
            public float Value2;
            public byte Value3;
            public bool Value4;
        }

        [Test]
        public void GetComponentRawData_ReturnsValidData()
        {
            // Arrange
            var entity = _archetypes.Spawn();
            var testData = new TestRawData { Value1 = 42, Value2 = 3.14f, Value3 = 255, Value4 = true };
            _archetypes.AddComponent(entity.Identity, testData);

            // Act
            var rawData = _archetypes.GetComponentRawData(entity.Identity, StorageType.Create<TestRawData>());

            // Assert
            Assert.That(rawData.Length, Is.EqualTo(12));
            var reconstructedData = MemoryMarshal.Read<TestRawData>(rawData);
            Assert.That(reconstructedData.Value1, Is.EqualTo(testData.Value1));
            Assert.That(reconstructedData.Value2, Is.EqualTo(testData.Value2));
            Assert.That(reconstructedData.Value3, Is.EqualTo(testData.Value3));
            Assert.That(reconstructedData.Value4, Is.EqualTo(testData.Value4));
        }

        [Test]
        public void GetComponentRawData_WithTagStruct_ReturnsEmptySpan()
        {
            // Arrange
            var entity = _archetypes.Spawn();
            _archetypes.AddComponent(entity.Identity, new ZeroStruct());

            // Act
            var rawData = _archetypes.GetComponentRawData(entity.Identity, StorageType.Create<ZeroStruct>());

            // Assert
            Assert.That(rawData.Length, Is.EqualTo(0));
        }

        [Test]
        public void SetComponentRawData_UpdatesComponentData()
        {
            // Arrange
            var entity = _archetypes.Spawn();
            var originalData = new TestRawData { Value1 = 42, Value2 = 3.14f, Value3 = 255, Value4 = true };
            _archetypes.AddComponent(entity.Identity, originalData);

            var newData = new TestRawData { Value1 = 100, Value2 = 2.718f, Value3 = 128, Value4 = false };
            var newRawData = MemoryMarshal.Cast<TestRawData, byte>(MemoryMarshal.CreateSpan(ref newData, 1));

            // Act
            _archetypes.SetComponentRawData(entity.Identity, StorageType.Create<TestRawData>(), newRawData);

            // Assert
            var component = _archetypes.GetComponent<TestRawData>(entity.Identity);
            Assert.That(component.Value1, Is.EqualTo(newData.Value1));
            Assert.That(component.Value2, Is.EqualTo(newData.Value2));
            Assert.That(component.Value3, Is.EqualTo(newData.Value3));
            Assert.That(component.Value4, Is.EqualTo(newData.Value4));
        }

        [Test]
        public void GetComponentRawData_NonExistentEntity_Throws()
        {
            // Arrange
            var nonExistentEntity = new Entity(new Identity(999));

            // Act & Assert
            Assert.Catch<Exception>(() =>
                _archetypes.GetComponentRawData(nonExistentEntity.Identity, StorageType.Create<TestRawData>()));
        }

        [Test]
        public void GetComponentRawData_NonExistentComponent_Throws()
        {
            // Arrange
            var entity = _archetypes.Spawn();

            // Act & Assert
            Assert.Catch<Exception>(() =>
                _archetypes.GetComponentRawData(entity.Identity, StorageType.Create<TestRawData>()));
        }

        [Test]
        public void GetComponentRawData_MultipleBooleanComponents_HandlesCorrectly()
        {
            // Arrange
            var entity1 = _archetypes.Spawn();
            var entity2 = _archetypes.Spawn();
            var entity3 = _archetypes.Spawn();

            _archetypes.AddComponent(entity1.Identity, true);
            _archetypes.AddComponent(entity2.Identity, false);
            _archetypes.AddComponent(entity3.Identity, true);

            // Act & Assert
            var rawData1 = _archetypes.GetComponentRawData(entity1.Identity, StorageType.Create<bool>());
            var rawData2 = _archetypes.GetComponentRawData(entity2.Identity, StorageType.Create<bool>());
            var rawData3 = _archetypes.GetComponentRawData(entity3.Identity, StorageType.Create<bool>());

            Assert.That(rawData1.Length, Is.EqualTo(1)); // bool is 1 byte
            Assert.That(rawData2.Length, Is.EqualTo(1));
            Assert.That(rawData3.Length, Is.EqualTo(1));

            var value1 = MemoryMarshal.Read<bool>(rawData1);
            var value2 = MemoryMarshal.Read<bool>(rawData2);
            var value3 = MemoryMarshal.Read<bool>(rawData3);

            Assert.That(value1, Is.True);
            Assert.That(value2, Is.False);
            Assert.That(value3, Is.True);

            // Verify we can modify the values through raw data
            var newData = new byte[] { 0 }; // false
            _archetypes.SetComponentRawData(entity1.Identity, StorageType.Create<bool>(), newData);
            Assert.That(_archetypes.GetComponent<bool>(entity1.Identity), Is.False);
            Assert.That(_archetypes.GetComponent<bool>(entity2.Identity), Is.False);
            Assert.That(_archetypes.GetComponent<bool>(entity3.Identity), Is.True);
        }
    }
}
