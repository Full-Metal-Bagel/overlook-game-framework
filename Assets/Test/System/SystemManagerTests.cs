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
            var factory = new MockSystemFactory("TestSystem", mockSystem, enable: true, tickStage: 0, tickTimes: -1);
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
            var factory = new MockSystemFactory("DisabledSystem", mockSystem, enable: false, tickStage: 0, tickTimes: -1);
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
                new MockSystemFactory("System1", system1, enable: true, tickStage: 0, tickTimes: -1),
                new MockSystemFactory("System2", system2, enable: true, tickStage: 1, tickTimes: -1),
                new MockSystemFactory("System3", system3, enable: true, tickStage: 0, tickTimes: -1)
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
                new MockSystemFactory("Enabled1", system1, enable: true, tickStage: 0, tickTimes: -1),
                new MockSystemFactory("Disabled", system2, enable: false, tickStage: 1, tickTimes: -1),
                new MockSystemFactory("Enabled2", system3, enable: true, tickStage: 2, tickTimes: -1)
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
                new MockSystemFactory("Stage2System", system1, enable: true, tickStage: 2, tickTimes: -1),
                new MockSystemFactory("Stage0System", system2, enable: true, tickStage: 0, tickTimes: -1),
                new MockSystemFactory("Stage1System", system3, enable: true, tickStage: 1, tickTimes: -1)
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
            var factory = new MockSystemFactory("TestSystem", mockSystem, enable: true, tickStage: 0, tickTimes: -1);
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
                new MockSystemFactory("System1", system1, enable: true, tickStage: 0, tickTimes: -1),
                new MockSystemFactory("System2", system2, enable: true, tickStage: 0, tickTimes: -1),
                new MockSystemFactory("System3", system3, enable: true, tickStage: 1, tickTimes: -1)
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
                new MockSystemFactory("Stage0", system0, enable: true, tickStage: 0, tickTimes: -1),
                new MockSystemFactory("Stage1", system1, enable: true, tickStage: 1, tickTimes: -1),
                new MockSystemFactory("Stage2", system2, enable: true, tickStage: 2, tickTimes: -1)
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
            var factory = new MockSystemFactory("TestSystem", mockSystem, enable: true, tickStage: 0, tickTimes: -1);
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
                new MockSystemFactory("ThrowingSystem", throwingSystem, enable: true, tickStage: 0, tickTimes: -1),
                new MockSystemFactory("NormalSystem", normalSystem, enable: true, tickStage: 0, tickTimes: -1)
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
            var factory = new MockSystemFactory("TestSystem", mockSystem, enable: true, tickStage: 0, tickTimes: 0);
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
            var factory = new MockSystemFactory("TestSystem", mockSystem, enable: true, tickStage: 0, tickTimes: 3);
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
            var factory = new MockSystemFactory("TestSystem", mockSystem, enable: true, tickStage: 0, tickTimes: -1);
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
            var factory = new MockSystemFactory("TestSystem", mockSystem, enable: true, tickStage: 0, tickTimes: -1);
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
            var factory = new MockSystemFactory("DisposableSystem", disposableSystem, enable: true, tickStage: 0, tickTimes: -1);
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
                new MockSystemFactory("System1", system1, enable: true, tickStage: 0, tickTimes: -1),
                new MockSystemFactory("System2", system2, enable: true, tickStage: 1, tickTimes: -1),
                new MockSystemFactory("System3", system3, enable: true, tickStage: 2, tickTimes: -1)
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
            var factory = new MockSystemFactory("DisposableSystem", disposableSystem, enable: true, tickStage: 0, tickTimes: -1);
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
                new MockSystemFactory("ThrowingSystem", throwingSystem, enable: true, tickStage: 0, tickTimes: -1),
                new MockSystemFactory("NormalSystem", normalSystem, enable: true, tickStage: 1, tickTimes: -1)
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
            var factory = new MockSystemFactory("TestSystem", mockSystem, enable: true, tickStage: 0, tickTimes: -1);
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
            var factory = new MockSystemFactory("TestSystem", mockSystem, enable: true, tickStage: 1, tickTimes: 5);
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
                new MockSystemFactory("Stage0_Sys1", stage0System1, enable: true, tickStage: 0, tickTimes: 2),
                new MockSystemFactory("Stage0_Sys2", stage0System2, enable: true, tickStage: 0, tickTimes: -1),
                new MockSystemFactory("Stage1_Sys", stage1System, enable: true, tickStage: 1, tickTimes: 1),
                new MockSystemFactory("Stage2_Sys", stage2System, enable: true, tickStage: 2, tickTimes: 0)
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

        #region Mock Classes

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

        private class MockSystemFactory : ISystemFactory
        {
            private readonly ISystem _system;

            public string SystemName { get; }
            public bool Enable { get; }
            public byte TickStage { get; }
            public int TickTimes { get; }

            public MockSystemFactory(string systemName, ISystem system, bool enable, byte tickStage, int tickTimes)
            {
                SystemName = systemName;
                _system = system;
                Enable = enable;
                TickStage = tickStage;
                TickTimes = tickTimes;
            }

            public ISystem Resolve(Container container, int systemIndex)
            {
                return _system;
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
