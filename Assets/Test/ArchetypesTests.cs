using System;
using System.Collections.Generic;
using System.Linq;
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
            Assert.That(_archetypes.EntityCount, Is.Zero); // Assuming EntityCount starts at 0
        }

        [Test]
        public void Spawn_ReturnsValidEntity()
        {
            var entity = _archetypes.Spawn();
            Assert.That(entity, Is.Not.Null);
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
            _archetypes.AddComponent(componentType, entity.Identity, 42);

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
        public void Despawn_RemovesEntityRelations_WhenEntityHasRelations()
        {
            // Arrange
            var entity = _archetypes.Spawn();
            var relatedEntity = _archetypes.Spawn();
            var relationType = StorageType.Create(typeof(object), relatedEntity.Identity); // Assuming Relation is a type representing relations
            _archetypes.AddComponent(relationType, entity.Identity, relatedEntity);

            // Act
            _archetypes.Despawn(entity.Identity);

            // Assert
            Assert.That(_archetypes.HasComponent(relationType, entity.Identity), Is.False);
            // Assuming there's a way to check if the relation still exists, for example:
            Assert.That(_archetypes.GetTarget(relationType, relatedEntity.Identity), Is.EqualTo(Entity.None));
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
            var type = StorageType.Create<object>(); // Assuming a valid StorageType instance
            _archetypes.AddComponent(type, entity.Identity, new object());
            Assert.That(_archetypes.HasComponent(type, entity.Identity), Is.True);
        }

        [Test]
        public void GetComponent_ReturnsValidComponent()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<object>(); // Assuming a valid StorageType instance
            var data = new object();
            _archetypes.AddComponent(type, entity.Identity, data);
            var component = _archetypes.GetComponent(type, entity.Identity);
            Assert.That(component, Is.EqualTo(data));
        }

        [Test]
        public void Lock_LocksArchetypes()
        {
            _archetypes.Lock();
            var type = StorageType.Create<object>();
            var identify = _archetypes.Spawn().Identity;
            _archetypes.AddComponent(type, identify, new object());
            Assert.That(_archetypes.HasComponent(type, identify), Is.False);
            _archetypes.Unlock();
            Assert.That(_archetypes.HasComponent(type, identify), Is.True);
        }

        [Test]
        public void Unlock_UnlocksArchetypes()
        {
            var entity = _archetypes.Spawn();
            _archetypes.Lock();
            _archetypes.Unlock();
            Assert.DoesNotThrow(() =>
                _archetypes.AddComponent(StorageType.Create<object>(), entity.Identity,
                    new object())); // Assuming operations don't throw when unlocked
        }

        // ... Add more tests for edge cases, different scenarios, and possible exceptions ...
        [Test]
        public void AddComponent_ThrowsWhenComponentAlreadyExists()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<object>(); // Assuming a valid StorageType instance
            _archetypes.AddComponent(type, entity.Identity, new object());
            Assert.Throws<Exception>(() =>
                _archetypes.AddComponent(type, entity.Identity,
                    new object())); // Assuming it throws an exception when trying to add an existing component
        }

        [Test]
        public void RemoveComponent_ThrowsWhenComponentDoesNotExist()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<object>(); // Assuming a valid StorageType instance
            Assert.Throws<Exception>(() =>
                _archetypes.RemoveComponent(type,
                    entity.Identity)); // Assuming it throws an exception when trying to remove a non-existent component
        }

        [Test]
        public void GetComponent_ThrowsWhenComponentDoesNotExist()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<object>(); // Assuming a valid StorageType instance
            Assert.Catch<Exception>(() =>
                _archetypes.GetComponent(type,
                    entity.Identity)); // Assuming it throws an exception when trying to get a non-existent component
        }

        [Test]
        public void AddComponent_WhenLocked_QueuesOperation()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<object>(); // Assuming a valid StorageType instance
            _archetypes.Lock();
            _archetypes.AddComponent(type, entity.Identity, new object());
            _archetypes.Unlock();
            Assert.That(_archetypes.HasComponent(type,
                entity.Identity), Is.True); // Assuming the component is added after unlocking
        }

        [Test]
        public void RemoveComponent_WhenLocked_QueuesOperation()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<object>(); // Assuming a valid StorageType instance
            _archetypes.AddComponent(type, entity.Identity, new object());
            _archetypes.Lock();
            _archetypes.RemoveComponent(type, entity.Identity);
            _archetypes.Unlock();
            Assert.That(_archetypes.HasComponent(type,
                entity.Identity), Is.False); // Assuming the component is removed after unlocking
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
            var type = StorageType.Create<object>(); // Assuming a valid StorageType instance
            var data = new object();
            _archetypes.AddComponent(type, entity.Identity, data);
            var component = _archetypes.GetComponent(type, entity.Identity);
            Assert.That(component, Is.EqualTo(data));
        }

        [Test]
        public void GetComponent_ThrowsWhenEntityDoesNotExist()
        {
            var nonExistentIdentity = new Identity(); // Assuming a valid Identity instance not linked to any entity
            var type = StorageType.Create<object>(); // Assuming a valid StorageType instance
            Assert.Catch<Exception>(() => _archetypes.GetComponent(type, nonExistentIdentity));
        }

        [Test]
        public void GetQuery_ReturnsValidQuery()
        {
            var mask = new Mask(); // Assuming a valid Mask instance
            mask.Has(StorageType.Create<object>());
            var query = _archetypes.GetQuery(mask,
                (archetypes, mask, tables) => new Query(archetypes, mask, tables)); // Assuming a valid delegate
            Assert.That(query, Is.Not.Null);
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
        public void GetTarget_ReturnsValidEntity()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<object>(entity.Identity); // Assuming a valid StorageType instance
            _archetypes.AddComponent(type, entity.Identity, new object());
            var targetEntity = _archetypes.GetTarget(type, entity.Identity);
            Assert.That(targetEntity, Is.EqualTo(entity));
        }

        [Test]
        public void GetTargets_ReturnsValidEntities()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<object>(entity.Identity); // Assuming a valid StorageType instance
            _archetypes.AddComponent(type, entity.Identity, new object());
            var targetEntities = _archetypes.GetTargets(type, entity.Identity);
            Assert.That(targetEntities, Is.EquivalentTo(new[] { entity }));
        }


        [Test]
        public void GetTargets_ReturnsEmptyArray_WhenNoRelationsExist()
        {
            var entity = _archetypes.Spawn();
            var type = StorageType.Create<object>(entity
                .Identity); // Assuming a valid StorageType instance representing a relation
            var targets = _archetypes.GetTargets(type, entity.Identity);
            Assert.That(targets, Is.Empty);
        }

        [Test]
        public void GetTargets_ReturnsCorrectTargets_WhenRelationsExist()
        {
            var entity = _archetypes.Spawn();
            var relatedEntity1 = _archetypes.Spawn();
            var relatedEntity2 = _archetypes.Spawn();
            var type = StorageType.Create<object>(); // Assuming a valid StorageType instance representing a relation

            _archetypes.AddComponent(StorageType.Create<object>(relatedEntity1.Identity), entity.Identity,
                new object());
            _archetypes.AddComponent(StorageType.Create<object>(relatedEntity2.Identity), entity.Identity,
                new object());

            var targets = _archetypes.GetTargets(type, entity.Identity);
            Assert.That(targets, Is.EquivalentTo(new[] { relatedEntity1, relatedEntity2 }));
        }

        [Test]
        public void GetTargets_DoesNotReturnUnrelatedEntities()
        {
            var entity = _archetypes.Spawn();
            var relatedEntity = _archetypes.Spawn();
            var unrelatedEntity = _archetypes.Spawn();
            var type = StorageType.Create<object>();

            _archetypes.AddComponent(StorageType.Create<object>(relatedEntity.Identity), entity.Identity, new object());

            var targets = _archetypes.GetTargets(type, entity.Identity);
            Assert.That(targets, Is.EquivalentTo(new[] { relatedEntity }));
        }
        //
        // [Test]
        // public void GetTargets_ThrowsException_WhenEntityDoesNotExist()
        // {
        //     var nonExistentIdentity = new Identity(); // Assuming a valid Identity instance not linked to any entity
        //     var type = StorageType.Create(); // Assuming a valid StorageType instance representing a relation
        //     Assert.Throws<Exception>(() => _archetypes.GetTargets(type, nonExistentIdentity));
        // }

        [Test]
        public void GetComponents_ReturnsAllComponentsOfEntity()
        {
            var entity = _archetypes.Spawn();
            var type1 = StorageType.Create<int>(); // Assuming a valid StorageType instance
            var type2 = StorageType.Create<float>(); // Another StorageType instance
            _archetypes.AddComponent(type1, entity.Identity, 123);
            _archetypes.AddComponent(type2, entity.Identity, 123f);
            var components = _archetypes.GetComponents(entity.Identity);
            Assert.That(components.Length, Is.EqualTo(3)); // will added `Entity` as component by default
            Assert.That(components.Any(c => c.Item1 == type1), Is.True);
            Assert.That(components.Any(c => c.Item1 == type2), Is.True);
        }


        [Test]
        public void GetTypeEntity_ReturnsExistingEntity_WhenTypeAlreadyAssociated()
        {
            var type = typeof(int); // Example type
            var firstEntity = _archetypes.GetTypeEntity(type);
            var secondEntity = _archetypes.GetTypeEntity(type);
            Assert.That(secondEntity, Is.EqualTo(firstEntity));
        }

        [Test]
        public void GetTypeEntity_ReturnsNewEntity_WhenTypeNotPreviouslyAssociated()
        {
            var type = typeof(string); // Example type
            var entity = _archetypes.GetTypeEntity(type);
            Assert.IsNotNull(entity);
        }

        [Test]
        public void GetTypeEntity_AssociatesDifferentEntities_ForDifferentTypes()
        {
            var type1 = typeof(int); // Example type
            var type2 = typeof(double); // Another example type
            var entity1 = _archetypes.GetTypeEntity(type1);
            var entity2 = _archetypes.GetTypeEntity(type2);
            Assert.That(entity2, Is.Not.EqualTo(entity1));
        }

        [Test]
        public void GetTypeEntity_ReturnsDistinctEntities_ForDistinctTypes()
        {
            var type1 = typeof(float); // Example type
            var type2 = typeof(char); // Another example type
            var entity1 = _archetypes.GetTypeEntity(type1);
            var entity2 = _archetypes.GetTypeEntity(type2);
            Assert.That(entity2, Is.Not.EqualTo(entity1));
        }

        [Test]
        public void GetTypeEntity_ThrowsException_WhenProvidedNullType()
        {
            Assert.Throws<ArgumentNullException>(() => _archetypes.GetTypeEntity(null!));
        }

        [Test]
        public void GetTypeEntity_AssociatesEntity_OnlyOnce_WhenCalledMultipleTimes()
        {
            var type = typeof(decimal); // Example type
            var entity1 = _archetypes.GetTypeEntity(type);
            var entity2 = _archetypes.GetTypeEntity(type);
            var entity3 = _archetypes.GetTypeEntity(type);
            Assert.That(entity2, Is.EqualTo(entity1));
            Assert.That(entity3, Is.EqualTo(entity2));
        }

        [Test]
        public void GetTypeEntity_DoesNotAffect_OtherEntitiesOrComponents()
        {
            var type = typeof(byte); // Example type
            var initialEntityCount = _archetypes.EntityCount; // Assuming a way to get the current entity count
            var entity = _archetypes.GetTypeEntity(type);
            var newEntityCount = _archetypes.EntityCount;
            Assert.That(newEntityCount, Is.EqualTo(initialEntityCount + 1));
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenTableMatchesMaskRequirements()
        {
            var mask = new Mask();
            mask.Has(StorageType.Create<int>());
            var tableTypes = new SortedSet<StorageType> { StorageType.Create<int>() };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(_archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableDoesNotMatchMaskRequirements()
        {
            var mask = new Mask();
            mask.Has(StorageType.Create<int>());
            var tableTypes = new SortedSet<StorageType> { StorageType.Create<string>() };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(_archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableContainsExcludedTypes()
        {
            var mask = new Mask();
            mask.Not(StorageType.Create<int>());
            var tableTypes = new SortedSet<StorageType> { StorageType.Create<int>() };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(_archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenTableContainsAnyOfTheMaskTypes()
        {
            var mask = new Mask();
            mask.Any(StorageType.Create<int>());
            mask.Any(StorageType.Create<string>());
            var tableTypes = new SortedSet<StorageType> { StorageType.Create<int>() };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(_archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableDoesNotContainAnyOfTheMaskTypes()
        {
            var mask = new Mask();
            mask.Any(StorageType.Create<int>());
            mask.Any(StorageType.Create<string>());
            var tableTypes = new SortedSet<StorageType> { StorageType.Create(typeof(double)) };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(_archetypes.IsMaskCompatibleWith(mask, table), Is.False);
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
            Assert.That(_archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsFalse_WhenTableContainsExcludedAndRequiredTypes()
        {
            var mask = new Mask();
            mask.Has(StorageType.Create(typeof(int)));
            mask.Not(StorageType.Create(typeof(string)));
            var tableTypes = new SortedSet<StorageType> { StorageType.Create(typeof(int)), StorageType.Create(typeof(string)) };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(_archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenMaskHasOptionalTypesAndTableContainsThem()
        {
            var mask = new Mask();
            mask.Any(StorageType.Create(typeof(int)));
            mask.Any(StorageType.Create(typeof(string)));
            var tableTypes = new SortedSet<StorageType> { StorageType.Create(typeof(int)), StorageType.Create(typeof(string)) };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(_archetypes.IsMaskCompatibleWith(mask, table), Is.True);
        }

        [Test]
        public void IsMaskCompatibleWith_ReturnsTrue_WhenMaskHasRequiredAnyTypesAndTableDoesNotContainThem()
        {
            var mask = new Mask();
            mask.Any(StorageType.Create(typeof(int)));
            mask.Any(StorageType.Create(typeof(string)));
            var tableTypes = new SortedSet<StorageType> { StorageType.Create(typeof(double)) };
            var table = new Table(0, _archetypes, tableTypes);
            Assert.That(_archetypes.IsMaskCompatibleWith(mask, table), Is.False);
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
            Assert.That(_archetypes.IsMaskCompatibleWith(mask, table), Is.False);
        }
    }
}
