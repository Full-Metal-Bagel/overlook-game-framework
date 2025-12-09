using System;
using NUnit.Framework;

namespace Overlook.Ecs.Tests
{
    [QueryComponent(typeof(Position))]
    public readonly partial record struct TestEntityWithSingleComponent;

    [QueryComponent(typeof(Position))]
    [QueryComponent(typeof(Velocity))]
    [QueryComponent(typeof(Health), IsOptional = true)]
    public readonly partial record struct TestEntityWithMultipleComponents;

    [QueryComponent(typeof(Health), Name = "HP", IsOptional = true, IsReadOnly = true)]
    public readonly partial record struct TestEntityWithConfiguredComponent;

    [QueryComponent(typeof(Position), QueryOnly = true)]
    public readonly partial record struct TestEntityWithQueryOnlyComponent;

    [TestFixture]
    public class QueryComponentAttributeTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithValidType_SetsComponentType()
        {
            // Arrange & Act
            var attr = new QueryComponentAttribute(typeof(Position));

            // Assert
            Assert.That(attr.ComponentType, Is.EqualTo(typeof(Position)));
        }

        [Test]
        public void Constructor_WithNullType_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new QueryComponentAttribute(null!));
        }

        #endregion

        #region Property Default Values Tests

        [Test]
        public void Name_DefaultValue_IsNull()
        {
            // Arrange & Act
            var attr = new QueryComponentAttribute(typeof(Position));

            // Assert
            Assert.That(attr.Name, Is.Null);
        }

        [Test]
        public void IsOptional_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var attr = new QueryComponentAttribute(typeof(Position));

            // Assert
            Assert.That(attr.IsOptional, Is.False);
        }

        [Test]
        public void IsReadOnly_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var attr = new QueryComponentAttribute(typeof(Position));

            // Assert
            Assert.That(attr.IsReadOnly, Is.False);
        }

        [Test]
        public void QueryOnly_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var attr = new QueryComponentAttribute(typeof(Position));

            // Assert
            Assert.That(attr.QueryOnly, Is.False);
        }

        #endregion

        #region Property Setter Tests

        [Test]
        public void Name_CanBeSet()
        {
            // Arrange
            var attr = new QueryComponentAttribute(typeof(Position));

            // Act
            attr.Name = "Pos";

            // Assert
            Assert.That(attr.Name, Is.EqualTo("Pos"));
        }

        [Test]
        public void IsOptional_CanBeSet()
        {
            // Arrange
            var attr = new QueryComponentAttribute(typeof(Position));

            // Act
            attr.IsOptional = true;

            // Assert
            Assert.That(attr.IsOptional, Is.True);
        }

        [Test]
        public void IsReadOnly_CanBeSet()
        {
            // Arrange
            var attr = new QueryComponentAttribute(typeof(Position));

            // Act
            attr.IsReadOnly = true;

            // Assert
            Assert.That(attr.IsReadOnly, Is.True);
        }

        [Test]
        public void QueryOnly_CanBeSet()
        {
            // Arrange
            var attr = new QueryComponentAttribute(typeof(Position));

            // Act
            attr.QueryOnly = true;

            // Assert
            Assert.That(attr.QueryOnly, Is.True);
        }

        #endregion

        #region Attribute Usage Tests

        [Test]
        public void Attribute_AllowsMultiple()
        {
            // Arrange & Act
            var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(QueryComponentAttribute),
                typeof(AttributeUsageAttribute)
            );

            // Assert
            Assert.That(attributeUsage, Is.Not.Null);
            Assert.That(attributeUsage!.AllowMultiple, Is.True);
        }

        [Test]
        public void Attribute_CanBeAppliedToClass()
        {
            // Arrange & Act
            var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(QueryComponentAttribute),
                typeof(AttributeUsageAttribute)
            );

            // Assert
            Assert.That(attributeUsage, Is.Not.Null);
            Assert.That(attributeUsage!.ValidOn.HasFlag(AttributeTargets.Class), Is.True);
        }

        [Test]
        public void Attribute_CanBeAppliedToStruct()
        {
            // Arrange & Act
            var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(QueryComponentAttribute),
                typeof(AttributeUsageAttribute)
            );

            // Assert
            Assert.That(attributeUsage, Is.Not.Null);
            Assert.That(attributeUsage!.ValidOn.HasFlag(AttributeTargets.Struct), Is.True);
        }

        [Test]
        public void Attribute_CanBeRetrievedFromDecoratedType()
        {
            // Arrange & Act
            var attributes = Attribute.GetCustomAttributes(typeof(TestEntityWithSingleComponent), typeof(QueryComponentAttribute));

            // Assert
            Assert.That(attributes, Has.Length.EqualTo(1));
            var attr = (QueryComponentAttribute)attributes[0];
            Assert.That(attr.ComponentType, Is.EqualTo(typeof(Position)));
        }

        [Test]
        public void Attribute_MultipleAttributesCanBeRetrieved()
        {
            // Arrange & Act
            var attributes = Attribute.GetCustomAttributes(typeof(TestEntityWithMultipleComponents), typeof(QueryComponentAttribute));

            // Assert
            Assert.That(attributes, Has.Length.EqualTo(3));
        }

        [Test]
        public void Attribute_WithAllPropertiesSet_RetrievesCorrectValues()
        {
            // Arrange & Act
            var attributes = Attribute.GetCustomAttributes(typeof(TestEntityWithConfiguredComponent), typeof(QueryComponentAttribute));

            // Assert
            Assert.That(attributes, Has.Length.EqualTo(1));
            var attr = (QueryComponentAttribute)attributes[0];
            Assert.That(attr.ComponentType, Is.EqualTo(typeof(Health)));
            Assert.That(attr.Name, Is.EqualTo("HP"));
            Assert.That(attr.IsOptional, Is.True);
            Assert.That(attr.IsReadOnly, Is.True);
            Assert.That(attr.QueryOnly, Is.False);
        }

        [Test]
        public void Attribute_QueryOnlyComponent_RetrievesCorrectValue()
        {
            // Arrange & Act
            var attributes = Attribute.GetCustomAttributes(typeof(TestEntityWithQueryOnlyComponent), typeof(QueryComponentAttribute));

            // Assert
            Assert.That(attributes, Has.Length.EqualTo(1));
            var attr = (QueryComponentAttribute)attributes[0];
            Assert.That(attr.QueryOnly, Is.True);
        }

        #endregion
    }

    #region Generated Query Integration Tests

    [TestFixture]
    public class QueryableEntityIntegrationTests
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

        #region Single Component Tests

        [Test]
        public void Query_WithSingleComponent_ReturnsMatchingEntities()
        {
            // Arrange - Create entities with Position
            var entity1 = EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);
            var entity2 = EntityBuilder.Create().Add(new Position(3, 4)).Build(_world);
            EntityBuilder.Create().Add(new Velocity(5, 6)).Build(_world); // No Position
            var expectedEntities = new[] { entity1.Entity, entity2.Entity };

            // Act
            var query = QueryBuilder.Create()
                .HasTestEntityWithSingleComponent()
                .BuildAsTestEntityWithSingleComponent(_world);

            // Assert
            var count = 0;
            foreach (var entity in query)
            {
                count++;
                Assert.That(expectedEntities, Does.Contain(entity.Entity.Entity));
            }
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void AsTestEntityWithSingleComponent_WrapEntityCorrectly()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create().Add(new Position(10, 20)).Build(_world);

            // Act
            var testEntity = worldEntity.AsTestEntityWithSingleComponent();

            // Assert
            Assert.That(testEntity.Position.X, Is.EqualTo(10));
            Assert.That(testEntity.Position.Y, Is.EqualTo(20));
        }

        [Test]
        public void IsTestEntityWithSingleComponent_ReturnsTrue_WhenEntityHasRequiredComponents()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);

            // Act & Assert
            Assert.That(worldEntity.IsTestEntityWithSingleComponent(), Is.True);
        }

        [Test]
        public void IsTestEntityWithSingleComponent_ReturnsFalse_WhenEntityMissingComponents()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create().Add(new Velocity(1, 2)).Build(_world);

            // Act & Assert
            Assert.That(worldEntity.IsTestEntityWithSingleComponent(), Is.False);
        }

        [Test]
        public void TestEntityWithSingleComponent_CanModifyComponentViaRef()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create().Add(new Position(10, 20)).Build(_world);
            var testEntity = worldEntity.AsTestEntityWithSingleComponent();

            // Act
            testEntity.Position = new Position(100, 200);

            // Assert
            Assert.That(_world.GetComponent<Position>(worldEntity.Entity).X, Is.EqualTo(100));
            Assert.That(_world.GetComponent<Position>(worldEntity.Entity).Y, Is.EqualTo(200));
        }

        #endregion

        #region Multiple Components Tests

        [Test]
        public void Query_WithMultipleComponents_ReturnsMatchingEntities()
        {
            // Arrange
            var entity1 = EntityBuilder.Create()
                .Add(new Position(1, 2))
                .Add(new Velocity(3, 4))
                .Build(_world);
            EntityBuilder.Create().Add(new Position(5, 6)).Build(_world); // Missing Velocity
            EntityBuilder.Create().Add(new Velocity(7, 8)).Build(_world); // Missing Position

            // Act
            var query = QueryBuilder.Create()
                .HasTestEntityWithMultipleComponents()
                .BuildAsTestEntityWithMultipleComponents(_world);

            // Assert
            var count = 0;
            foreach (var entity in query)
            {
                count++;
                Assert.That(entity.Entity.Entity, Is.EqualTo(entity1.Entity));
            }
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void TestEntityWithMultipleComponents_CanAccessAllRequiredComponents()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create()
                .Add(new Position(10, 20))
                .Add(new Velocity(30, 40))
                .Build(_world);

            // Act
            var testEntity = worldEntity.AsTestEntityWithMultipleComponents();

            // Assert
            Assert.That(testEntity.Position.X, Is.EqualTo(10));
            Assert.That(testEntity.Position.Y, Is.EqualTo(20));
            Assert.That(testEntity.Velocity.X, Is.EqualTo(30));
            Assert.That(testEntity.Velocity.Y, Is.EqualTo(40));
        }

        [Test]
        public void TestEntityWithMultipleComponents_OptionalComponent_ReturnsFalse_WhenNotPresent()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create()
                .Add(new Position(1, 2))
                .Add(new Velocity(3, 4))
                .Build(_world);

            // Act
            var testEntity = worldEntity.AsTestEntityWithMultipleComponents();

            // Assert
            Assert.That(testEntity.HasHealth, Is.False);
        }

        [Test]
        public void TestEntityWithMultipleComponents_OptionalComponent_ReturnsTrue_WhenPresent()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create()
                .Add(new Position(1, 2))
                .Add(new Velocity(3, 4))
                .Add(new Health(100))
                .Build(_world);

            // Act
            var testEntity = worldEntity.AsTestEntityWithMultipleComponents();

            // Assert
            Assert.That(testEntity.HasHealth, Is.True);
        }

        [Test]
        public void TestEntityWithMultipleComponents_TryGetOptionalComponent_ReturnsValue_WhenPresent()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create()
                .Add(new Position(1, 2))
                .Add(new Velocity(3, 4))
                .Add(new Health(100))
                .Build(_world);

            // Act
            var testEntity = worldEntity.AsTestEntityWithMultipleComponents();
            var health = testEntity.TryGetHealth();

            // Assert
            Assert.That(health.Value, Is.EqualTo(100));
        }

        [Test]
        public void TestEntityWithMultipleComponents_TryGetOptionalComponent_ReturnsDefault_WhenNotPresent()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create()
                .Add(new Position(1, 2))
                .Add(new Velocity(3, 4))
                .Build(_world);

            // Act
            var testEntity = worldEntity.AsTestEntityWithMultipleComponents();
            var health = testEntity.TryGetHealth(new Health(999)); // Default value

            // Assert
            Assert.That(health.Value, Is.EqualTo(999));
        }

        [Test]
        public void TestEntityWithMultipleComponents_TrySetOptionalComponent_ReturnsFalse_WhenNotPresent()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create()
                .Add(new Position(1, 2))
                .Add(new Velocity(3, 4))
                .Build(_world);

            // Act
            var testEntity = worldEntity.AsTestEntityWithMultipleComponents();
            var result = testEntity.TrySetHealth(new Health(100));

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TestEntityWithMultipleComponents_TrySetOptionalComponent_ReturnsTrue_WhenPresent()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create()
                .Add(new Position(1, 2))
                .Add(new Velocity(3, 4))
                .Add(new Health(50))
                .Build(_world);

            // Act
            var testEntity = worldEntity.AsTestEntityWithMultipleComponents();
            var result = testEntity.TrySetHealth(new Health(100));

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_world.GetComponent<Health>(worldEntity.Entity).Value, Is.EqualTo(100));
        }

        #endregion

        #region ReadOnly Query Tests

        [Test]
        public void ReadOnlyQuery_IteratesEntities()
        {
            // Arrange
            EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);
            EntityBuilder.Create().Add(new Position(3, 4)).Build(_world);

            // Act
            var query = QueryBuilder.Create()
                .HasTestEntityWithSingleComponent()
                .BuildAsTestEntityWithSingleComponentReadOnly(_world);

            // Assert
            var count = 0;
            foreach (var entity in query)
            {
                count++;
                // ReadOnly should still provide access to Position
                Assert.That(entity.Position, Is.Not.EqualTo(default(Position)));
            }
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void AsReadOnly_ReturnsReadOnlyView()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create().Add(new Position(10, 20)).Build(_world);
            var testEntity = worldEntity.AsTestEntityWithSingleComponent();

            // Act
            var readOnly = testEntity.AsReadOnly;

            // Assert
            Assert.That(readOnly.Position.X, Is.EqualTo(10));
            Assert.That(readOnly.Position.Y, Is.EqualTo(20));
        }

        #endregion

        #region Implicit Operators Tests

        [Test]
        public void ImplicitOperator_ToEntity_Works()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);
            var testEntity = worldEntity.AsTestEntityWithSingleComponent();

            // Act
            Entity entity = testEntity;

            // Assert
            Assert.That(entity, Is.EqualTo(worldEntity.Entity));
        }

        [Test]
        public void ImplicitOperator_ToWorldEntity_Works()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);
            var testEntity = worldEntity.AsTestEntityWithSingleComponent();

            // Act
            WorldEntity result = testEntity;

            // Assert
            Assert.That(result.Entity, Is.EqualTo(worldEntity.Entity));
            Assert.That(result.World, Is.EqualTo(_world));
        }

        [Test]
        public void ExplicitOperator_FromWorldEntity_Works()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);

            // Act
            var testEntity = (TestEntityWithSingleComponent)worldEntity;

            // Assert
            Assert.That(testEntity.Position.X, Is.EqualTo(1));
        }

        [Test]
        public void ImplicitOperator_ToReadOnly_Works()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);
            var testEntity = worldEntity.AsTestEntityWithSingleComponent();

            // Act
            TestEntityWithSingleComponent.ReadOnly readOnly = testEntity;

            // Assert
            Assert.That(readOnly.Position.X, Is.EqualTo(1));
        }

        #endregion

        #region QueryOnly Component Tests

        [Test]
        public void QueryOnlyComponent_IncludedInQueryFilter()
        {
            // Arrange
            EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);
            EntityBuilder.Create().Add(new Velocity(3, 4)).Build(_world); // No Position

            // Act
            var query = QueryBuilder.Create()
                .HasTestEntityWithQueryOnlyComponent()
                .BuildAsTestEntityWithQueryOnlyComponent(_world);

            // Assert - Should match only entity with Position
            var count = 0;
            foreach (var _ in query)
            {
                count++;
            }
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void IsTestEntityWithQueryOnlyComponent_IncludesQueryOnlyComponent()
        {
            // Arrange
            var withPosition = EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);
            var withoutPosition = EntityBuilder.Create().Add(new Velocity(3, 4)).Build(_world);

            // Assert
            Assert.That(withPosition.IsTestEntityWithQueryOnlyComponent(), Is.True);
            Assert.That(withoutPosition.IsTestEntityWithQueryOnlyComponent(), Is.False);
        }

        #endregion

        #region ToString Tests

        [Test]
        public void ToString_ReturnsEntityString()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);
            var testEntity = worldEntity.AsTestEntityWithSingleComponent();

            // Act
            var result = testEntity.ToString();

            // Assert - Should contain the type name and component properties
            Assert.That(result, Does.StartWith(nameof(TestEntityWithSingleComponent)));
            Assert.That(result, Does.Contain("Position = Position { X = 1, Y = 2 }"));
        }

        [Test]
        public void ReadOnly_ToString_ReturnsEntityString()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create().Add(new Position(1, 2)).Build(_world);
            var testEntity = worldEntity.AsTestEntityWithSingleComponent();
            var readOnly = testEntity.AsReadOnly;

            // Act
            var result = readOnly.ToString();

            // Assert - ReadOnly ToString should have ".ReadOnly" after the type name
            var expectedTypeName = nameof(TestEntityWithSingleComponent) + ".ReadOnly";
            Assert.That(result, Does.StartWith(expectedTypeName));
            Assert.That(result, Does.Contain("Position = Position { X = 1, Y = 2 }"));
        }

        [Test]
        public void ToString_MultipleComponents_ReturnsEntityString()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create()
                .Add(new Position(10, 20))
                .Add(new Velocity(30, 40))
                .Build(_world);
            var testEntity = worldEntity.AsTestEntityWithMultipleComponents();

            // Act
            var result = testEntity.ToString();

            // Assert - Should contain the type name and component properties
            Assert.That(result, Does.StartWith(nameof(TestEntityWithMultipleComponents)));
            Assert.That(result, Does.Contain("Position = Position { X = 10, Y = 20 }"));
            Assert.That(result, Does.Contain("Velocity = Velocity { X = 30, Y = 40 }"));
            Assert.That(result, Does.Contain("Health? = <none>"));
        }

        [Test]
        public void ReadOnly_ToString_MultipleComponents_ReturnsEntityString()
        {
            // Arrange
            var worldEntity = EntityBuilder.Create()
                .Add(new Position(10, 20))
                .Add(new Velocity(30, 40))
                .Build(_world);
            var testEntity = worldEntity.AsTestEntityWithMultipleComponents();
            var readOnly = testEntity.AsReadOnly;

            // Act
            var result = readOnly.ToString();

            // Assert - ReadOnly ToString should have ".ReadOnly" after the type name
            var expectedTypeName = nameof(TestEntityWithMultipleComponents) + ".ReadOnly";
            Assert.That(result, Does.StartWith(expectedTypeName));
            Assert.That(result, Does.Contain("Position = Position { X = 10, Y = 20 }"));
            Assert.That(result, Does.Contain("Velocity = Velocity { X = 30, Y = 40 }"));
            Assert.That(result, Does.Contain("Health? = <none>"));
        }

        #endregion
    }

    #endregion
}
