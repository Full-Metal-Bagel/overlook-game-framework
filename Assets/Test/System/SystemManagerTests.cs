using System;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using OneShot;

namespace Overlook.System.Tests
{
    [TestFixture]
    public class SystemManagerTests
    {
        private Container _container;
        private MockLogger _logger;

        [SetUp]
        public void SetUp()
        {
            _container = new Container();
            _logger = new MockLogger();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Dispose();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithValidParameters_CreatesSystemManager()
        {
            // Arrange & Act
            var systemManager = new SystemManager(_container, Array.Empty<ISystemFactory>(), _logger);

            // Assert
            Assert.That(systemManager.Container, Is.EqualTo(_container));
            Assert.That(systemManager.Count, Is.EqualTo(0));
            Assert.That(systemManager.Systems, Is.Not.Null);
            Assert.That(systemManager.SystemNames, Is.Not.Null);
        }

        #endregion

        #region CreateSystems Tests

        [Test]
        public void CreateSystems_WithNoFactories_CreatesEmptySystemList()
        {
            // Arrange
            var systemManager = new SystemManager(_container, Array.Empty<ISystemFactory>(), _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.Count, Is.EqualTo(0));
            Assert.That(systemManager.Systems.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreateSystems_WithSingleEnabledFactory_CreatesSystem()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.Count, Is.EqualTo(1));
            Assert.That(systemManager.Systems[0], Is.EqualTo(mockSystem));
            Assert.That(systemManager.SystemNames[0], Is.EqualTo("TestSystem"));
        }

        [Test]
        public void CreateSystems_WithDisabledFactory_SkipsSystem()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "DisabledSystem", Enable: false);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreateSystems_WithMultipleFactories_CreatesSystems()
        {
            // Arrange
            var system1 = new MockSystem();
            var system2 = new MockSystem();
            var system3 = new MockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(system1, TickStage: 0, TickTimes: -1, Name: "System1", Enable: true),
                new InstanceSystemFactory(system2, TickStage: 1, TickTimes: -1, Name: "System2", Enable: true),
                new InstanceSystemFactory(system3, TickStage: 0, TickTimes: -1, Name: "System3", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.Count, Is.EqualTo(3));
        }

        [Test]
        public void CreateSystems_WithMixedEnabledDisabled_CreatesOnlyEnabled()
        {
            // Arrange
            var system1 = new MockSystem();
            var system2 = new MockSystem();
            var system3 = new MockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(system1, TickStage: 0, TickTimes: -1, Name: "Enabled1", Enable: true),
                new InstanceSystemFactory(system2, TickStage: 1, TickTimes: -1, Name: "Disabled", Enable: false),
                new InstanceSystemFactory(system3, TickStage: 2, TickTimes: -1, Name: "Enabled2", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.Count, Is.EqualTo(2));
            Assert.That(systemManager.Systems[0], Is.EqualTo(system1));
            Assert.That(systemManager.Systems[1], Is.EqualTo(system3));
        }

        [Test]
        public void CreateSystems_WithMultipleStages_OrdersSystemsByStage()
        {
            // Arrange
            var system1 = new MockSystem();
            var system2 = new MockSystem();
            var system3 = new MockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(system1, TickStage: 2, TickTimes: -1, Name: "Stage2System", Enable: true),
                new InstanceSystemFactory(system2, TickStage: 0, TickTimes: -1, Name: "Stage0System", Enable: true),
                new InstanceSystemFactory(system3, TickStage: 1, TickTimes: -1, Name: "Stage1System", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.Count, Is.EqualTo(3));
            // Systems should be ordered by stage
            var runtimeSystem0 = systemManager.GetSystem(0);
            var runtimeSystem1 = systemManager.GetSystem(1);
            var runtimeSystem2 = systemManager.GetSystem(2);

            Assert.That(runtimeSystem0.TickStage, Is.LessThanOrEqualTo(runtimeSystem1.TickStage));
            Assert.That(runtimeSystem1.TickStage, Is.LessThanOrEqualTo(runtimeSystem2.TickStage));
        }

        #endregion

        #region Tick Tests

        [Test]
        public void Tick_WithNoSystems_DoesNotThrow()
        {
            // Arrange
            var systemManager = new SystemManager(_container, Array.Empty<ISystemFactory>(), _logger);
            systemManager.CreateSystems();

            // Act & Assert
            Assert.DoesNotThrow(() => systemManager.Tick(0));
            Assert.DoesNotThrow(() => systemManager.Tick(1));
        }

        [Test]
        public void Tick_WithSingleSystem_CallsTick()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Tick(0);

            // Assert
            Assert.That(mockSystem.TickCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_WithMultipleSystems_CallsAllSystemsInStage()
        {
            // Arrange
            var system1 = new MockSystem();
            var system2 = new MockSystem();
            var system3 = new MockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(system1, TickStage: 0, TickTimes: -1, Name: "System1", Enable: true),
                new InstanceSystemFactory(system2, TickStage: 0, TickTimes: -1, Name: "System2", Enable: true),
                new InstanceSystemFactory(system3, TickStage: 1, TickTimes: -1, Name: "System3", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Tick(0);

            // Assert
            Assert.That(system1.TickCount, Is.EqualTo(1));
            Assert.That(system2.TickCount, Is.EqualTo(1));
            Assert.That(system3.TickCount, Is.EqualTo(0)); // Different stage
        }

        [Test]
        public void Tick_WithDifferentStages_CallsOnlySystemsInThatStage()
        {
            // Arrange
            var system0 = new MockSystem();
            var system1 = new MockSystem();
            var system2 = new MockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(system0, TickStage: 0, TickTimes: -1, Name: "Stage0", Enable: true),
                new InstanceSystemFactory(system1, TickStage: 1, TickTimes: -1, Name: "Stage1", Enable: true),
                new InstanceSystemFactory(system2, TickStage: 2, TickTimes: -1, Name: "Stage2", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Tick(1);

            // Assert
            Assert.That(system0.TickCount, Is.EqualTo(0));
            Assert.That(system1.TickCount, Is.EqualTo(1));
            Assert.That(system2.TickCount, Is.EqualTo(0));
        }

        [Test]
        public void Tick_WithInvalidStage_DoesNotThrow()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act & Assert
            Assert.DoesNotThrow(() => systemManager.Tick(100));
            Assert.That(mockSystem.TickCount, Is.EqualTo(0));
        }

        [Test]
        public void Tick_WithSystemThrowingException_LogsErrorAndContinues()
        {
            // Arrange
            var throwingSystem = new ThrowingMockSystem();
            var normalSystem = new MockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(throwingSystem, TickStage: 0, TickTimes: -1, Name: "ThrowingSystem", Enable: true),
                new InstanceSystemFactory(normalSystem, TickStage: 0, TickTimes: -1, Name: "NormalSystem", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Tick(0);

            // Assert
            Assert.That(throwingSystem.TickCount, Is.EqualTo(1));
            Assert.That(normalSystem.TickCount, Is.EqualTo(1)); // Should still be called
            Assert.That(_logger.CriticalCount, Is.GreaterThan(0)); // Error should be logged
        }

        [Test]
        public void Tick_WithTickTimesZero_DoesNotTickSystem()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: 0, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Tick(0);

            // Assert
            Assert.That(mockSystem.TickCount, Is.EqualTo(0));
        }

        [Test]
        public void Tick_WithPositiveTickTimes_DecreasesRemainedTimes()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: 3, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act & Assert
            Assert.That(systemManager.RemainedTimes[0], Is.EqualTo(3));

            systemManager.Tick(0);
            Assert.That(mockSystem.TickCount, Is.EqualTo(1));
            Assert.That(systemManager.RemainedTimes[0], Is.EqualTo(2));

            systemManager.Tick(0);
            Assert.That(mockSystem.TickCount, Is.EqualTo(2));
            Assert.That(systemManager.RemainedTimes[0], Is.EqualTo(1));

            systemManager.Tick(0);
            Assert.That(mockSystem.TickCount, Is.EqualTo(3));
            Assert.That(systemManager.RemainedTimes[0], Is.EqualTo(0));

            // Should stop ticking after reaching 0
            systemManager.Tick(0);
            Assert.That(mockSystem.TickCount, Is.EqualTo(3));
            Assert.That(systemManager.RemainedTimes[0], Is.EqualTo(0));
        }

        [Test]
        public void Tick_WithNegativeTickTimes_TicksIndefinitely()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act
            for (int i = 0; i < 100; i++)
            {
                systemManager.Tick(0);
            }

            // Assert
            Assert.That(mockSystem.TickCount, Is.EqualTo(100));
            Assert.That(systemManager.RemainedTimes[0], Is.EqualTo(-1)); // Should remain -1
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void Dispose_WithNoSystems_DoesNotThrow()
        {
            // Arrange
            var systemManager = new SystemManager(_container, Array.Empty<ISystemFactory>(), _logger);
            systemManager.CreateSystems();

            // Act & Assert
            Assert.DoesNotThrow(() => systemManager.Dispose());
        }

        [Test]
        public void Dispose_WithNonDisposableSystem_DoesNotThrow()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act & Assert
            Assert.DoesNotThrow(() => systemManager.Dispose());
        }

        [Test]
        public void Dispose_WithDisposableSystem_DisposesSystem()
        {
            // Arrange
            var disposableSystem = new DisposableMockSystem();
            var factory = new InstanceSystemFactory(disposableSystem, TickStage: 0, TickTimes: -1, Name: "DisposableSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Dispose();

            // Assert
            Assert.That(disposableSystem.IsDisposed, Is.True);
        }

        [Test]
        public void Dispose_WithMultipleDisposableSystems_DisposesAll()
        {
            // Arrange
            var system1 = new DisposableMockSystem();
            var system2 = new DisposableMockSystem();
            var system3 = new DisposableMockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(system1, TickStage: 0, TickTimes: -1, Name: "System1", Enable: true),
                new InstanceSystemFactory(system2, TickStage: 1, TickTimes: -1, Name: "System2", Enable: true),
                new InstanceSystemFactory(system3, TickStage: 2, TickTimes: -1, Name: "System3", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Dispose();

            // Assert
            Assert.That(system1.IsDisposed, Is.True);
            Assert.That(system2.IsDisposed, Is.True);
            Assert.That(system3.IsDisposed, Is.True);
        }

        [Test]
        public void Dispose_WithAlreadyDisposedSystem_DoesNotThrow()
        {
            // Arrange
            var disposableSystem = new DisposableMockSystem();
            disposableSystem.Dispose(); // Pre-dispose
            var factory = new InstanceSystemFactory(disposableSystem, TickStage: 0, TickTimes: -1, Name: "DisposableSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act & Assert
            Assert.DoesNotThrow(() => systemManager.Dispose());
        }

        [Test]
        public void Dispose_WithSystemThrowingException_LogsErrorAndContinuesDisposing()
        {
            // Arrange
            var throwingSystem = new ThrowingDisposableMockSystem();
            var normalSystem = new DisposableMockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(throwingSystem, TickStage: 0, TickTimes: -1, Name: "ThrowingSystem", Enable: true),
                new InstanceSystemFactory(normalSystem, TickStage: 1, TickTimes: -1, Name: "NormalSystem", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Dispose();

            // Assert
            Assert.That(normalSystem.IsDisposed, Is.True); // Should still dispose other systems
            Assert.That(_logger.WarningCount, Is.GreaterThan(0)); // Error should be logged
        }

        [Test]
        public void Dispose_ClearsSystemsList()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Dispose();

            // Assert
            Assert.That(systemManager.Count, Is.EqualTo(0));
        }

        #endregion

        #region GetSystem Tests

        [Test]
        public void GetSystem_WithValidIndex_ReturnsRuntimeSystem()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 1, TickTimes: 5, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act
            var runtimeSystem = systemManager.GetSystem(0);

            // Assert
            Assert.That(runtimeSystem.System, Is.EqualTo(mockSystem));
            Assert.That(runtimeSystem.Name, Is.EqualTo("TestSystem"));
            Assert.That(runtimeSystem.TickStage, Is.EqualTo(1));
            Assert.That(runtimeSystem.RemainedTimes, Is.EqualTo(5));
        }

        #endregion

        #region Complex Integration Tests

        [Test]
        public void ComplexScenario_MultipleStagesAndTickTimes_WorksCorrectly()
        {
            // Arrange
            var stage0System1 = new MockSystem();
            var stage0System2 = new MockSystem();
            var stage1System = new MockSystem();
            var stage2System = new MockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(stage0System1, TickStage: 0, TickTimes: 2, Name: "Stage0_Sys1", Enable: true),
                new InstanceSystemFactory(stage0System2, TickStage: 0, TickTimes: -1, Name: "Stage0_Sys2", Enable: true),
                new InstanceSystemFactory(stage1System, TickStage: 1, TickTimes: 1, Name: "Stage1_Sys", Enable: true),
                new InstanceSystemFactory(stage2System, TickStage: 2, TickTimes: 0, Name: "Stage2_Sys", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);
            systemManager.CreateSystems();

            // Act - Tick stage 0 three times
            systemManager.Tick(0);
            systemManager.Tick(0);
            systemManager.Tick(0);

            // Tick stage 1 twice
            systemManager.Tick(1);
            systemManager.Tick(1);

            // Tick stage 2 once
            systemManager.Tick(2);

            // Assert
            Assert.That(stage0System1.TickCount, Is.EqualTo(2)); // TickTimes was 2
            Assert.That(stage0System2.TickCount, Is.EqualTo(3)); // TickTimes was -1 (infinite)
            Assert.That(stage1System.TickCount, Is.EqualTo(1)); // TickTimes was 1
            Assert.That(stage2System.TickCount, Is.EqualTo(0)); // TickTimes was 0 (never ticks)
        }

        #endregion

        #region SystemFactory Tests

        [Test]
        public void SystemFactory_WithType_ResolvesSystem()
        {
            // Arrange
            var factory = new SystemFactory(typeof(ResolvableMockSystem), TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.Count, Is.EqualTo(1));
            Assert.That(systemManager.Systems[0], Is.InstanceOf<ResolvableMockSystem>());
            Assert.That(systemManager.SystemNames[0], Is.EqualTo("TestSystem"));
        }

        [Test]
        public void SystemFactory_WithoutName_UsesTypeName()
        {
            // Arrange
            var factory = new SystemFactory(typeof(ResolvableMockSystem));
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.SystemNames[0], Is.EqualTo("ResolvableMockSystem"));
        }

        [Test]
        public void SystemFactory_WithDisabled_SkipsSystem()
        {
            // Arrange
            var factory = new SystemFactory(typeof(ResolvableMockSystem), Enable: false);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.Count, Is.EqualTo(0));
        }

        [Test]
        public void SystemFactory_Tick_CallsResolvedSystem()
        {
            // Arrange
            var factory = new SystemFactory(typeof(ResolvableMockSystem), TickStage: 0, TickTimes: -1);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Tick(0);

            // Assert
            var system = (ResolvableMockSystem)systemManager.Systems[0];
            Assert.That(system.TickCount, Is.EqualTo(1));
        }

        #endregion

        #region SystemFactory<T> Tests

        [Test]
        public void SystemFactoryGeneric_ResolvesSystem()
        {
            // Arrange
            var factory = new SystemFactory<ResolvableMockSystem>(TickStage: 0, TickTimes: -1, Name: "GenericTestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.Count, Is.EqualTo(1));
            Assert.That(systemManager.Systems[0], Is.InstanceOf<ResolvableMockSystem>());
            Assert.That(systemManager.SystemNames[0], Is.EqualTo("GenericTestSystem"));
        }

        [Test]
        public void SystemFactoryGeneric_WithoutName_UsesTypeName()
        {
            // Arrange
            var factory = new SystemFactory<ResolvableMockSystem>();
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.SystemNames[0], Is.EqualTo("ResolvableMockSystem"));
        }

        [Test]
        public void SystemFactoryGeneric_WithDisabled_SkipsSystem()
        {
            // Arrange
            var factory = new SystemFactory<ResolvableMockSystem>(Enable: false);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);

            // Act
            systemManager.CreateSystems();

            // Assert
            Assert.That(systemManager.Count, Is.EqualTo(0));
        }

        [Test]
        public void SystemFactoryGeneric_Tick_CallsResolvedSystem()
        {
            // Arrange
            var factory = new SystemFactory<ResolvableMockSystem>(TickStage: 0, TickTimes: -1);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Tick(0);

            // Assert
            var system = (ResolvableMockSystem)systemManager.Systems[0];
            Assert.That(system.TickCount, Is.EqualTo(1));
        }

        [Test]
        public void SystemFactoryGeneric_WithTickTimes_RespectsLimit()
        {
            // Arrange
            var factory = new SystemFactory<ResolvableMockSystem>(TickStage: 0, TickTimes: 2);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act
            systemManager.Tick(0);
            systemManager.Tick(0);
            systemManager.Tick(0); // Should not tick

            // Assert
            var system = (ResolvableMockSystem)systemManager.Systems[0];
            Assert.That(system.TickCount, Is.EqualTo(2));
        }

        #endregion

        #region GetStageEvents Tests

        [Test]
        public void GetStageEvents_WithValidStage_ReturnsSystemEventsManager()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act
            var stageEvents = systemManager.GetStageEvents(0);

            // Assert
            Assert.That(stageEvents, Is.Not.Null);
            Assert.That(stageEvents, Is.InstanceOf<SystemEventsManager>());
        }

        [Test]
        public void GetStageEvents_WithMultipleStages_ReturnsDifferentManagers()
        {
            // Arrange
            var system0 = new MockSystem();
            var system1 = new MockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(system0, TickStage: 0, TickTimes: -1, Name: "Stage0", Enable: true),
                new InstanceSystemFactory(system1, TickStage: 1, TickTimes: -1, Name: "Stage1", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);
            systemManager.CreateSystems();

            // Act
            var events0 = systemManager.GetStageEvents(0);
            var events1 = systemManager.GetStageEvents(1);

            // Assert
            Assert.That(events0, Is.Not.Null);
            Assert.That(events1, Is.Not.Null);
            Assert.That(events0, Is.Not.SameAs(events1));
        }

        [Test]
        public void GetStageEvents_WithInvalidStage_ThrowsException()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => systemManager.GetStageEvents(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => systemManager.GetStageEvents(1));
        }

        [Test]
        public void GetStageEvents_EventsAreTickedAfterStageTick()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            var stageEvents = systemManager.GetStageEvents(0);
            stageEvents.AppendEvent(new TestEvent { Value = 42 });

            // Assert - Event is pending before tick
            Assert.That(stageEvents.GetEvents<TestEvent>().Count, Is.EqualTo(0));

            // Act - Tick the stage
            systemManager.Tick(0);

            // Assert - Event is available after tick
            Assert.That(stageEvents.GetEvents<TestEvent>().Count, Is.EqualTo(1));
            Assert.That(stageEvents.GetEvents<TestEvent>()[0].Value, Is.EqualTo(42));
        }

        [Test]
        public void GetStageEvents_EventsAreIsolatedBetweenStages()
        {
            // Arrange
            var system0 = new MockSystem();
            var system1 = new MockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(system0, TickStage: 0, TickTimes: -1, Name: "Stage0", Enable: true),
                new InstanceSystemFactory(system1, TickStage: 1, TickTimes: -1, Name: "Stage1", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);
            systemManager.CreateSystems();

            var events0 = systemManager.GetStageEvents(0);
            var events1 = systemManager.GetStageEvents(1);

            // Act - Append event to stage 0 only
            events0.AppendEvent(new TestEvent { Value = 100 });
            systemManager.Tick(0);
            systemManager.Tick(1);

            // Assert - Event is only in stage 0
            Assert.That(events0.GetEvents<TestEvent>().Count, Is.EqualTo(1));
            Assert.That(events1.GetEvents<TestEvent>().Count, Is.EqualTo(0));
        }

        [Test]
        public void GetStageEvents_MultipleEventsInSameStage()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            var stageEvents = systemManager.GetStageEvents(0);

            // Act - Append multiple events
            stageEvents.AppendEvent(new TestEvent { Value = 1 });
            stageEvents.AppendEvent(new TestEvent { Value = 2 });
            stageEvents.AppendEvent(new TestEvent { Value = 3 });
            systemManager.Tick(0);

            // Assert
            var events = stageEvents.GetEvents<TestEvent>();
            Assert.That(events.Count, Is.EqualTo(3));
            Assert.That(events[0].Value, Is.EqualTo(1));
            Assert.That(events[1].Value, Is.EqualTo(2));
            Assert.That(events[2].Value, Is.EqualTo(3));
        }

        [Test]
        public void GetStageEvents_EventExpiresAfterLastingFrames()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            var stageEvents = systemManager.GetStageEvents(0);

            // Act - Append event with lasting frames = 1 (default)
            stageEvents.AppendEvent(new TestEvent { Value = 42 }, lastingFrames: 1);
            systemManager.Tick(0); // Frame 0: event becomes available

            // Assert - Event is available
            Assert.That(stageEvents.GetEvents<TestEvent>().Count, Is.EqualTo(1));

            // Act - Tick again
            systemManager.Tick(0); // Frame 1: event expires

            // Assert - Event has expired
            Assert.That(stageEvents.GetEvents<TestEvent>().Count, Is.EqualTo(0));
        }

        [Test]
        public void GetStageEvents_EventLastsMultipleFrames()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            var stageEvents = systemManager.GetStageEvents(0);

            // Act - Append event with lasting frames = 3
            stageEvents.AppendEvent(new TestEvent { Value = 42 }, lastingFrames: 3);
            systemManager.Tick(0); // Frame 0: event becomes available

            // Assert - Event is available for 3 frames
            Assert.That(stageEvents.GetEvents<TestEvent>().Count, Is.EqualTo(1));

            systemManager.Tick(0); // Frame 1
            Assert.That(stageEvents.GetEvents<TestEvent>().Count, Is.EqualTo(1));

            systemManager.Tick(0); // Frame 2
            Assert.That(stageEvents.GetEvents<TestEvent>().Count, Is.EqualTo(1));

            systemManager.Tick(0); // Frame 3: event expires
            Assert.That(stageEvents.GetEvents<TestEvent>().Count, Is.EqualTo(0));
        }

        [Test]
        public void GetStageEvents_MultipleEventTypes()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            var stageEvents = systemManager.GetStageEvents(0);

            // Act - Append different event types
            stageEvents.AppendEvent(new TestEvent { Value = 42 });
            stageEvents.AppendEvent(new AnotherTestEvent { Name = 123 });
            systemManager.Tick(0);

            // Assert - Both event types are available
            Assert.That(stageEvents.GetEvents<TestEvent>().Count, Is.EqualTo(1));
            Assert.That(stageEvents.GetEvents<TestEvent>()[0].Value, Is.EqualTo(42));
            Assert.That(stageEvents.GetEvents<AnotherTestEvent>().Count, Is.EqualTo(1));
            Assert.That(stageEvents.GetEvents<AnotherTestEvent>()[0].Name, Is.EqualTo(123));
        }

        [Test]
        public void GetStageEvents_SystemCanAppendEventsDuringTick()
        {
            // Arrange
            SystemEventsManager capturedEvents = null!;
            var eventAppendingSystem = new EventAppendingSystem(() =>
            {
                capturedEvents.AppendEvent(new TestEvent { Value = 999 });
            });
            var factory = new InstanceSystemFactory(eventAppendingSystem, TickStage: 0, TickTimes: -1, Name: "EventSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            capturedEvents = systemManager.GetStageEvents(0);

            // Assert - No events before tick
            Assert.That(capturedEvents.GetEvents<TestEvent>().Count, Is.EqualTo(0));

            // Act - First tick: system appends event, events tick at end of stage
            systemManager.Tick(0);

            // Assert - Event is available immediately (events tick happens after systems tick)
            Assert.That(capturedEvents.GetEvents<TestEvent>().Count, Is.EqualTo(1));
            Assert.That(capturedEvents.GetEvents<TestEvent>()[0].Value, Is.EqualTo(999));

            // Act - Second tick: first event expires, new event appended
            systemManager.Tick(0);

            // Assert - Still one event (old expired, new available)
            Assert.That(capturedEvents.GetEvents<TestEvent>().Count, Is.EqualTo(1));
        }

        [Test]
        public void GetStageEvents_EventsCanBeIteratedWithForeach()
        {
            // Arrange
            var mockSystem = new MockSystem();
            var factory = new InstanceSystemFactory(mockSystem, TickStage: 0, TickTimes: -1, Name: "TestSystem", Enable: true);
            var systemManager = new SystemManager(_container, new[] { factory }, _logger);
            systemManager.CreateSystems();

            var stageEvents = systemManager.GetStageEvents(0);
            stageEvents.AppendEvent(new TestEvent { Value = 10 });
            stageEvents.AppendEvent(new TestEvent { Value = 20 });
            stageEvents.AppendEvent(new TestEvent { Value = 30 });
            systemManager.Tick(0);

            // Act
            var sum = 0;
            foreach (var e in stageEvents.GetEvents<TestEvent>())
            {
                sum += e.Value;
            }

            // Assert
            Assert.That(sum, Is.EqualTo(60));
        }

        [Test]
        public void GetStageEvents_IndependentFrameCountersPerStage()
        {
            // Arrange
            var system0 = new MockSystem();
            var system1 = new MockSystem();
            var factories = new ISystemFactory[]
            {
                new InstanceSystemFactory(system0, TickStage: 0, TickTimes: -1, Name: "Stage0", Enable: true),
                new InstanceSystemFactory(system1, TickStage: 1, TickTimes: -1, Name: "Stage1", Enable: true)
            };
            var systemManager = new SystemManager(_container, factories, _logger);
            systemManager.CreateSystems();

            var events0 = systemManager.GetStageEvents(0);
            var events1 = systemManager.GetStageEvents(1);

            // Act - Tick stage 0 multiple times, stage 1 once
            events0.AppendEvent(new TestEvent { Value = 1 }, lastingFrames: 2);
            systemManager.Tick(0); // Stage 0: frame 0
            systemManager.Tick(0); // Stage 0: frame 1

            events1.AppendEvent(new TestEvent { Value = 2 }, lastingFrames: 2);
            systemManager.Tick(1); // Stage 1: frame 0

            // Assert - Stage 0 event expired, Stage 1 event still valid
            Assert.That(events0.GetEvents<TestEvent>().Count, Is.EqualTo(1)); // Still frame 1, expires at frame 2
            Assert.That(events1.GetEvents<TestEvent>().Count, Is.EqualTo(1));

            systemManager.Tick(0); // Stage 0: frame 2 - event expires
            Assert.That(events0.GetEvents<TestEvent>().Count, Is.EqualTo(0));
            Assert.That(events1.GetEvents<TestEvent>().Count, Is.EqualTo(1)); // Stage 1 still at frame 0
        }

        private struct TestEvent
        {
            public int Value;
        }

        private struct AnotherTestEvent
        {
            public int Name;
        }

        private class EventAppendingSystem : ISystem
        {
            private readonly Action _onTick;

            public EventAppendingSystem(Action onTick)
            {
                _onTick = onTick;
            }

            public void Tick() => _onTick();
        }

        #endregion

        #region Mock Classes

        // Public class that can be resolved by DI container
        public class ResolvableMockSystem : ISystem
        {
            public int TickCount { get; private set; }

            public void Tick()
            {
                TickCount++;
            }
        }

        private class MockSystem : ISystem
        {
            public int TickCount { get; private set; }

            public void Tick()
            {
                TickCount++;
            }
        }

        private class ThrowingMockSystem : ISystem
        {
            public int TickCount { get; private set; }

            public void Tick()
            {
                TickCount++;
                throw new InvalidOperationException("Simulated system tick error");
            }
        }

        private class DisposableMockSystem : ISystem, IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Tick()
            {
            }

            public void Dispose()
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(DisposableMockSystem));
                IsDisposed = true;
            }
        }

        private class ThrowingDisposableMockSystem : ISystem, IDisposable
        {
            public void Tick()
            {
            }

            public void Dispose()
            {
                throw new InvalidOperationException("Simulated dispose error");
            }
        }

        private class MockLogger : ILogger<SystemManager>
        {
            public int CriticalCount { get; private set; }
            public int WarningCount { get; private set; }

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (logLevel == LogLevel.Critical)
                    CriticalCount++;
                else if (logLevel == LogLevel.Warning)
                    WarningCount++;
            }
        }

        #endregion
    }
}
