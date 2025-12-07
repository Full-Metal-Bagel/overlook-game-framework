#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UnityEngine;

namespace Overlook.Logging.Tests;

/// <summary>
/// Mock ILogHandler for testing UnityLogHandler behavior.
/// </summary>
public class MockLogHandler : ILogHandler
{
    public List<(LogType logType, UnityEngine.Object? context, string message)> LoggedMessages { get; } = new();
    public List<(Exception exception, UnityEngine.Object? context)> LoggedExceptions { get; } = new();

    public void LogFormat(LogType logType, UnityEngine.Object? context, string format, params object[] args)
    {
        var message = string.Format(format, args);
        LoggedMessages.Add((logType, context, message));
    }

    public void LogException(Exception exception, UnityEngine.Object? context)
    {
        LoggedExceptions.Add((exception, context));
    }

    public void Clear()
    {
        LoggedMessages.Clear();
        LoggedExceptions.Clear();
    }
}

[TestFixture]
public class UnityLogHandlerTests
{
    private MockLogHandler _mockHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockHandler = new MockLogHandler();
    }

    [TearDown]
    public void TearDown()
    {
        _mockHandler.Clear();
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithNullLogHandler_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UnityLogHandler(null!, "Test"));
    }

    [Test]
    public void Constructor_WithValidLogHandler_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => new UnityLogHandler(_mockHandler, "Test"));
    }

    [Test]
    public void Constructor_WithDefaultParameters_CreatesValidLogger()
    {
        var logger = new UnityLogHandler(_mockHandler);
        Assert.That(logger.IsEnabled(LogLevel.Trace), Is.True);
    }

    #endregion

    #region IsEnabled Tests

    [Test]
    public void IsEnabled_WithNoneLevel_ReturnsFalse()
    {
        var logger = new UnityLogHandler(_mockHandler);
        Assert.That(logger.IsEnabled(LogLevel.None), Is.False);
    }

    [TestCase(LogLevel.Trace)]
    [TestCase(LogLevel.Debug)]
    [TestCase(LogLevel.Information)]
    [TestCase(LogLevel.Warning)]
    [TestCase(LogLevel.Error)]
    [TestCase(LogLevel.Critical)]
    public void IsEnabled_WithDefaultMinLevel_ReturnsTrue(LogLevel logLevel)
    {
        var logger = new UnityLogHandler(_mockHandler, minLevel: LogLevel.Trace);
        Assert.That(logger.IsEnabled(logLevel), Is.True);
    }

    [Test]
    public void IsEnabled_BelowMinLevel_ReturnsFalse()
    {
        var logger = new UnityLogHandler(_mockHandler, minLevel: LogLevel.Warning);

        Assert.That(logger.IsEnabled(LogLevel.Trace), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Debug), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Information), Is.False);
    }

    [Test]
    public void IsEnabled_AtOrAboveMinLevel_ReturnsTrue()
    {
        var logger = new UnityLogHandler(_mockHandler, minLevel: LogLevel.Warning);

        Assert.That(logger.IsEnabled(LogLevel.Warning), Is.True);
        Assert.That(logger.IsEnabled(LogLevel.Error), Is.True);
        Assert.That(logger.IsEnabled(LogLevel.Critical), Is.True);
    }

    #endregion

    #region Log Method Tests

    [Test]
    public void Log_WhenDisabled_DoesNotLog()
    {
        var logger = new UnityLogHandler(_mockHandler, minLevel: LogLevel.Warning);

        logger.Log(LogLevel.Debug, new EventId(0), "test", null, (s, e) => s);

        Assert.That(_mockHandler.LoggedMessages, Is.Empty);
    }

    [Test]
    public void Log_WithNullFormatter_ThrowsArgumentNullException()
    {
        var logger = new UnityLogHandler(_mockHandler);

        Assert.Throws<ArgumentNullException>(() =>
            logger.Log(LogLevel.Information, new EventId(0), "test", null, null!));
    }

    [Test]
    public void Log_WithEmptyMessageAndNoException_DoesNotLog()
    {
        var logger = new UnityLogHandler(_mockHandler);

        logger.Log(LogLevel.Information, new EventId(0), "", null, (s, e) => s);

        Assert.That(_mockHandler.LoggedMessages, Is.Empty);
    }

    [Test]
    public void Log_WithNullMessageAndNoException_DoesNotLog()
    {
        var logger = new UnityLogHandler(_mockHandler);

        logger.Log<string?>(LogLevel.Information, new EventId(0), null, null, (s, e) => s ?? "");

        Assert.That(_mockHandler.LoggedMessages, Is.Empty);
    }

    [Test]
    public void Log_WithValidMessage_LogsMessage()
    {
        var logger = new UnityLogHandler(_mockHandler);

        logger.Log(LogLevel.Information, new EventId(0), "test message", null, (s, e) => s);

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("test message"));
    }

    #endregion

    #region Log Level Mapping Tests

    [TestCase(LogLevel.Trace, LogType.Log)]
    [TestCase(LogLevel.Debug, LogType.Log)]
    [TestCase(LogLevel.Information, LogType.Log)]
    [TestCase(LogLevel.Warning, LogType.Warning)]
    [TestCase(LogLevel.Error, LogType.Error)]
    [TestCase(LogLevel.Critical, LogType.Error)]
    public void Log_MapsLogLevelToCorrectUnityLogType(LogLevel logLevel, LogType expectedLogType)
    {
        var logger = new UnityLogHandler(_mockHandler);

        logger.Log(logLevel, new EventId(0), "test", null, (s, e) => s);

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].logType, Is.EqualTo(expectedLogType));
    }

    #endregion

    #region Message Formatting Tests

    [TestCase(LogLevel.Trace, "[TRACE]")]
    [TestCase(LogLevel.Debug, "[DEBUG]")]
    [TestCase(LogLevel.Information, "[INFO]")]
    [TestCase(LogLevel.Warning, "[WARN]")]
    [TestCase(LogLevel.Error, "[ERROR]")]
    [TestCase(LogLevel.Critical, "[CRIT]")]
    public void Log_IncludesCorrectLevelPrefix(LogLevel logLevel, string expectedPrefix)
    {
        var logger = new UnityLogHandler(_mockHandler);

        logger.Log(logLevel, new EventId(0), "test", null, (s, e) => s);

        Assert.That(_mockHandler.LoggedMessages[0].message, Does.StartWith(expectedPrefix));
    }

    [Test]
    public void Log_WithCategoryName_IncludesCategoryInMessage()
    {
        var logger = new UnityLogHandler(_mockHandler, categoryName: "MyCategory");

        logger.Log(LogLevel.Information, new EventId(0), "test", null, (s, e) => s);

        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("[MyCategory]"));
    }

    [Test]
    public void Log_WithEventId_IncludesEventIdInMessage()
    {
        var logger = new UnityLogHandler(_mockHandler);

        logger.Log(LogLevel.Information, new EventId(42, "TestEvent"), "test", null, (s, e) => s);

        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("[TestEvent(42)]"));
    }

    [Test]
    public void Log_WithZeroEventId_DoesNotIncludeEventIdInMessage()
    {
        var logger = new UnityLogHandler(_mockHandler);

        logger.Log(LogLevel.Information, new EventId(0), "test message", null, (s, e) => s);

        // Should contain "[INFO] test message" without extra brackets for event id
        Assert.That(_mockHandler.LoggedMessages[0].message, Is.EqualTo("[INFO] test message"));
    }

    [Test]
    public void Log_WithCategoryAndEventId_FormatsMessageCorrectly()
    {
        var logger = new UnityLogHandler(_mockHandler, categoryName: "TestCategory");

        logger.Log(LogLevel.Information, new EventId(123, "TestEvent"), "hello world", null, (s, e) => s);

        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("[INFO]"));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("[TestCategory]"));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("[TestEvent(123)]"));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("hello world"));
    }

    #endregion

    #region Exception Handling Tests

    [Test]
    public void Log_WithException_LogsException()
    {
        var logger = new UnityLogHandler(_mockHandler);
        var exception = new InvalidOperationException("Test exception");

        logger.Log(LogLevel.Error, new EventId(0), "error occurred", exception, (s, e) => s);

        Assert.That(_mockHandler.LoggedExceptions, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedExceptions[0].exception, Is.SameAs(exception));
    }

    [Test]
    public void Log_WithExceptionAndMessage_LogsBoth()
    {
        var logger = new UnityLogHandler(_mockHandler);
        var exception = new InvalidOperationException("Test exception");

        logger.Log(LogLevel.Error, new EventId(0), "error occurred", exception, (s, e) => s);

        Assert.That(_mockHandler.LoggedExceptions, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("error occurred"));
    }

    [Test]
    public void Log_WithExceptionAndEmptyMessage_OnlyLogsException()
    {
        var logger = new UnityLogHandler(_mockHandler);
        var exception = new InvalidOperationException("Test exception");

        logger.Log(LogLevel.Error, new EventId(0), "", exception, (s, e) => s);

        Assert.That(_mockHandler.LoggedExceptions, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages, Is.Empty);
    }

    #endregion

    #region BeginScope Tests

    [Test]
    public void BeginScope_WithNonObjectState_ReturnsNonNullDisposable()
    {
        var logger = new UnityLogHandler(_mockHandler);

        using var scope = logger.BeginScope("test scope");

        Assert.That(scope, Is.Not.Null);
    }

    [Test]
    public void BeginScope_DisposingDoesNotThrow()
    {
        var logger = new UnityLogHandler(_mockHandler);

        Assert.DoesNotThrow(() =>
        {
            using var scope = logger.BeginScope("test scope");
        });
    }

    [Test]
    public void BeginScope_WithNonObjectState_ReturnsSameNullScopeInstance()
    {
        var logger = new UnityLogHandler(_mockHandler);

        using var scope1 = logger.BeginScope("scope1");
        using var scope2 = logger.BeginScope(123);

        Assert.That(scope1, Is.SameAs(scope2));
    }

    [Test]
    public void BeginScope_WithUnityObject_OverridesContext()
    {
        var contextObject = new GameObject("TestContext");
        try
        {
            var logger = new UnityLogHandler(_mockHandler);

            using (logger.BeginScope(contextObject))
            {
                logger.Log(LogLevel.Information, new EventId(0), "test", null, (s, e) => s);
            }

            Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
            Assert.That(_mockHandler.LoggedMessages[0].context, Is.SameAs(contextObject));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(contextObject);
        }
    }

    [Test]
    public void BeginScope_WithUnityObject_RestoresOriginalContextOnDispose()
    {
        var originalContext = new GameObject("OriginalContext");
        var scopeContext = new GameObject("ScopeContext");
        try
        {
            var logger = new UnityLogHandler(_mockHandler, context: originalContext);

            using (logger.BeginScope(scopeContext))
            {
                logger.Log(LogLevel.Information, new EventId(0), "inside scope", null, (s, e) => s);
            }

            logger.Log(LogLevel.Information, new EventId(0), "outside scope", null, (s, e) => s);

            Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(2));
            Assert.That(_mockHandler.LoggedMessages[0].context, Is.SameAs(scopeContext));
            Assert.That(_mockHandler.LoggedMessages[1].context, Is.SameAs(originalContext));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(originalContext);
            UnityEngine.Object.DestroyImmediate(scopeContext);
        }
    }

    [Test]
    public void BeginScope_WithUnityObject_SupportsNestedScopes()
    {
        var context1 = new GameObject("Context1");
        var context2 = new GameObject("Context2");
        try
        {
            var logger = new UnityLogHandler(_mockHandler);

            using (logger.BeginScope(context1))
            {
                logger.Log(LogLevel.Information, new EventId(0), "level1", null, (s, e) => s);

                using (logger.BeginScope(context2))
                {
                    logger.Log(LogLevel.Information, new EventId(0), "level2", null, (s, e) => s);
                }

                logger.Log(LogLevel.Information, new EventId(0), "back to level1", null, (s, e) => s);
            }

            Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(3));
            Assert.That(_mockHandler.LoggedMessages[0].context, Is.SameAs(context1));
            Assert.That(_mockHandler.LoggedMessages[1].context, Is.SameAs(context2));
            Assert.That(_mockHandler.LoggedMessages[2].context, Is.SameAs(context1));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(context1);
            UnityEngine.Object.DestroyImmediate(context2);
        }
    }

    [Test]
    public void BeginScope_WithUnityObject_ReturnsDifferentInstanceThanNullScope()
    {
        var contextObject = new GameObject("TestContext");
        try
        {
            var logger = new UnityLogHandler(_mockHandler);

            using var nullScope = logger.BeginScope("string state");
            using var contextScope = logger.BeginScope(contextObject);

            Assert.That(contextScope, Is.Not.SameAs(nullScope));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(contextObject);
        }
    }

    #endregion
}

[TestFixture]
public class UnityLogHandlerGenericTests
{
    private MockLogHandler _mockHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockHandler = new MockLogHandler();
    }

    [TearDown]
    public void TearDown()
    {
        _mockHandler.Clear();
    }

    private class TestCategoryClass { }

    [Test]
    public void Constructor_UsesCategoryTypeNameAsCategory()
    {
        var logger = new UnityLogHandler<TestCategoryClass>(_mockHandler);

        logger.Log(LogLevel.Information, new EventId(0), "test", null, (s, e) => s);

        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("[TestCategoryClass]"));
    }

    [Test]
    public void IsEnabled_DelegatesToInnerLogger()
    {
        var logger = new UnityLogHandler<TestCategoryClass>(_mockHandler, minLevel: LogLevel.Warning);

        Assert.That(logger.IsEnabled(LogLevel.Debug), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Warning), Is.True);
    }

    [Test]
    public void Log_DelegatesToInnerLogger()
    {
        var logger = new UnityLogHandler<TestCategoryClass>(_mockHandler);

        logger.Log(LogLevel.Information, new EventId(0), "test message", null, (s, e) => s);

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("test message"));
    }

    [Test]
    public void BeginScope_DelegatesToInnerLogger()
    {
        var logger = new UnityLogHandler<TestCategoryClass>(_mockHandler);

        using var scope = logger.BeginScope("test");

        Assert.That(scope, Is.Not.Null);
    }

    [Test]
    public void ImplementsILoggerOfT()
    {
        var logger = new UnityLogHandler<TestCategoryClass>(_mockHandler);

        Assert.That(logger, Is.InstanceOf<ILogger<TestCategoryClass>>());
    }
}
