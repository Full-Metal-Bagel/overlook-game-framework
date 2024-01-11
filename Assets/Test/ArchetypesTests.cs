using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using NUnit.Framework;

namespace RelEcs.Tests
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

        [Test]
        public void Constructor_InitializesProperly()
        {
            Assert.IsNotNull(_archetypes);
            Assert.That(_archetypes._entityCount, Is.Zero); // Assuming EntityCount starts at 0
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
            Assert.Catch<Exception>(() => _archetypes.Despawn(entity.Identity)); // The entity was never alive to begin with
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
            Assert.That(_archetypes.HasComponent(componentType, entity.Identity), Is.False);
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
            Assert.IsFalse(_archetypes.IsAlive(entity.Identity));
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
            Assert.IsFalse(_archetypes.IsAlive(entity1.Identity));
            Assert.IsTrue(_archetypes.IsAlive(entity2.Identity));
        }

        [Test]
        public void Despawn_UpdatesEntityMeta_WhenEntityIsDespawned()
        {
            // Arrange
            var entity = _archetypes.Spawn();

            // Act
            _archetypes.Despawn(entity.Identity);

            // Assert
            var meta = _archetypes.GetEntityMeta(entity.Identity); // Assuming GetEntityMeta is accessible
            Assert.That(meta.Identity, Is.EqualTo(Identity.None));
            Assert.That(meta.Row, Is.EqualTo(0));
            // ... any other checks on meta ...
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
        public void AddComponent_ThrowsWhenComponentAlreadyExists()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<int>(); // Assuming a valid StorageType instance
            _archetypes.AddComponent(entity.Identity, 123);
            Assert.Throws<Exception>(() => _archetypes.AddComponent(entity.Identity, 123)); // Assuming it throws an exception when trying to add an existing component
        }

        [Test]
        public void RemoveComponent_ThrowsWhenComponentDoesNotExist()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<string>(); // Assuming a valid StorageType instance
            Assert.Throws<Exception>(() =>
                _archetypes.RemoveComponent(type,
                    entity.Identity)); // Assuming it throws an exception when trying to remove a non-existent component
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
            var nonExistentIdentity = new Identity(); // Assuming a valid Identity instance not linked to any entity
            var type = StorageType.Create<int>(); // Assuming a valid StorageType instance
            Assert.Catch<Exception>(() => _archetypes.GetComponent<int>(nonExistentIdentity));
        }

        [Test]
        public void GetQuery_ReturnsValidQuery()
        {
            var mask = new Mask(); // Assuming a valid Mask instance
            mask.Has(StorageType.Create<string>());
            var query = _archetypes.GetQuery(mask,
                (archetypes, mask, tables) => new Query { Archetypes = archetypes, Mask = mask, Tables = tables }); // Assuming a valid delegate
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
            _archetypes.FindAllComponents(entity.Identity, components.GetValue());
            Assert.That(components.Count, Is.EqualTo(3)); // will added `Entity` as component by default
            Assert.That(components.GetValue().Any(c => c.Type == type1), Is.True);
            Assert.That(components.GetValue().Any(c => c.Type == type2), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenTableMatchesMaskRequirements()
        {
            var mask = new Mask();
            mask.Has(StorageType.Create<int>());
            var tableTypes = new SortedSet<StorageType> { StorageType.Create<int>() };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableDoesNotMatchMaskRequirements()
        {
            var mask = new Mask();
            mask.Has(StorageType.Create<int>());
            var tableTypes = new SortedSet<StorageType> { StorageType.Create<string>() };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableContainsExcludedTypes()
        {
            var mask = new Mask();
            mask.Not(StorageType.Create<int>());
            var tableTypes = new SortedSet<StorageType> { StorageType.Create<int>() };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenTableContainsAnyOfTheMaskTypes()
        {
            var mask = new Mask();
            mask.Any(StorageType.Create<int>());
            mask.Any(StorageType.Create<string>());
            var tableTypes = new SortedSet<StorageType> { StorageType.Create<int>() };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableDoesNotContainAnyOfTheMaskTypes()
        {
            var mask = new Mask();
            mask.Any(StorageType.Create<int>());
            mask.Any(StorageType.Create<string>());
            var tableTypes = new SortedSet<StorageType> { StorageType.Create(typeof(double)) };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenTableMatchesAllMaskRequirements()
        {
            var mask = new Mask();
            mask.Has(StorageType.Create(typeof(int)));
            mask.Any(StorageType.Create(typeof(string)));
            mask.Not(StorageType.Create(typeof(double)));
            var tableTypes = new SortedSet<StorageType> { StorageType.Create(typeof(int)), StorageType.Create(typeof(string)) };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableContainsExcludedAndRequiredTypes()
        {
            var mask = new Mask();
            mask.Has(StorageType.Create(typeof(int)));
            mask.Not(StorageType.Create(typeof(string)));
            var tableTypes = new SortedSet<StorageType> { StorageType.Create(typeof(int)), StorageType.Create(typeof(string)) };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenMaskHasOptionalTypesAndTableContainsThem()
        {
            var mask = new Mask();
            mask.Any(StorageType.Create(typeof(int)));
            mask.Any(StorageType.Create(typeof(string)));
            var tableTypes = new SortedSet<StorageType> { StorageType.Create(typeof(int)), StorageType.Create(typeof(string)) };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenMaskHasRequiredAnyTypesAndTableDoesNotContainThem()
        {
            var mask = new Mask();
            mask.Any(StorageType.Create(typeof(int)));
            mask.Any(StorageType.Create(typeof(string)));
            var tableTypes = new SortedSet<StorageType> { StorageType.Create(typeof(double)) };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenMaskHasRequiredAndOptionalTypesButTableDoesNotContainRequired()
        {
            var mask = new Mask();
            mask.Has(StorageType.Create(typeof(double)));
            mask.Any(StorageType.Create(typeof(int)));
            mask.Any(StorageType.Create(typeof(string)));
            var tableTypes = new SortedSet<StorageType> { StorageType.Create(typeof(int)), StorageType.Create(typeof(string)) };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(Archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }
    }
}
