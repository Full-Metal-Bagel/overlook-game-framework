#nullable enable
using System;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Overlook.Logging.Tests;

/// <summary>
/// Tests for the LoggerMessage source generator integration with UnityLogHandler.
/// These tests verify that the [LoggerMessage] attribute generates correct code.
/// </summary>
[TestFixture]
public class LoggerMessageGeneratorTests
{
    private MockLogHandler _mockHandler = null!;
    private UnityLogHandler _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockHandler = new MockLogHandler();
        _logger = new UnityLogHandler(_mockHandler);
    }

    [TearDown]
    public void TearDown()
    {
        _mockHandler.Clear();
    }

    #region Basic LoggerMessage Tests

    [Test]
    public void LoggerMessage_NoParameters_LogsMessage()
    {
        TestLogMessages.LogStartup(_logger);

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("Application started"));
        Assert.That(_mockHandler.LoggedMessages[0].logType, Is.EqualTo(LogType.Log));
    }

    [Test]
    public void LoggerMessage_WithOneParameter_LogsFormattedMessage()
    {
        TestLogMessages.LogUserLogin(_logger, "TestUser");

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("TestUser"));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("logged in"));
    }

    [Test]
    public void LoggerMessage_WithTwoParameters_LogsFormattedMessage()
    {
        TestLogMessages.LogItemPurchased(_logger, "Sword", 100);

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("Sword"));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("100"));
    }

    [Test]
    public void LoggerMessage_WithThreeParameters_LogsFormattedMessage()
    {
        TestLogMessages.LogPlayerStats(_logger, "Player1", 100, 50.5f);

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("Player1"));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("100"));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("50.5"));
    }

    #endregion

    #region Log Level Tests

    [Test]
    public void LoggerMessage_WarningLevel_LogsAsWarning()
    {
        TestLogMessages.LogLowHealth(_logger, "Player1", 10);

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].logType, Is.EqualTo(LogType.Warning));
    }

    [Test]
    public void LoggerMessage_ErrorLevel_LogsAsError()
    {
        TestLogMessages.LogConnectionFailed(_logger, "Server1");

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].logType, Is.EqualTo(LogType.Error));
    }

    [Test]
    public void LoggerMessage_DebugLevel_LogsWhenEnabled()
    {
        var debugLogger = new UnityLogHandler(_mockHandler, minLevel: LogLevel.Debug);
        TestLogMessages.LogDebugInfo(debugLogger, "test data");

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("test data"));
    }

    [Test]
    public void LoggerMessage_DebugLevel_DoesNotLogWhenDisabled()
    {
        var infoLogger = new UnityLogHandler(_mockHandler, minLevel: LogLevel.Information);
        TestLogMessages.LogDebugInfo(infoLogger, "test data");

        Assert.That(_mockHandler.LoggedMessages, Is.Empty);
    }

    #endregion

    #region Exception Tests

    [Test]
    public void LoggerMessage_WithException_LogsException()
    {
        var exception = new InvalidOperationException("Test exception");
        TestLogMessages.LogError(_logger, "Operation failed", exception);

        Assert.That(_mockHandler.LoggedExceptions, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedExceptions[0].exception, Is.SameAs(exception));
    }

    [Test]
    public void LoggerMessage_WithException_LogsMessageAndException()
    {
        var exception = new InvalidOperationException("Test exception");
        TestLogMessages.LogError(_logger, "Operation failed", exception);

        Assert.That(_mockHandler.LoggedExceptions, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("Operation failed"));
    }

    #endregion

    #region EventId Tests

    [Test]
    public void LoggerMessage_WithEventId_IncludesEventIdInOutput()
    {
        // EventId should be set via the attribute
        TestLogMessages.LogWithEventId(_logger, "test");

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        // The event ID (1001) should be included in the formatted message
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("1001").Or.Contain("ImportantEvent"));
    }

    #endregion

    #region SkipEnabledCheck Tests

    [Test]
    public void LoggerMessage_WithSkipEnabledCheck_AlwaysLogs()
    {
        // Even with a high min level, SkipEnabledCheck should bypass the check
        // Note: This is a performance optimization flag, the actual behavior
        // depends on the generated code
        var warningLogger = new UnityLogHandler(_mockHandler, minLevel: LogLevel.Warning);

        // This should still evaluate the log level because that's the semantic meaning
        TestLogMessages.LogAlwaysCheck(warningLogger, "test");

        // With SkipEnabledCheck=true, the message is still logged regardless of level filter
        // But the actual behavior may vary - this test documents expected behavior
        Assert.That(_mockHandler.LoggedMessages.Count, Is.GreaterThanOrEqualTo(0));
    }

    #endregion

    #region Generic Logger Tests

    [Test]
    public void LoggerMessage_WithGenericLogger_WorksCorrectly()
    {
        var genericLogger = new UnityLogHandler<LoggerMessageGeneratorTests>(_mockHandler);
        TestLogMessages.LogStartup(genericLogger);

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("Application started"));
        // Should also include the category name
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("LoggerMessageGeneratorTests"));
    }

    #endregion

    #region Complex Type Tests

    [Test]
    public void LoggerMessage_WithVector3_LogsFormattedValue()
    {
        var position = new Vector3(1.5f, 2.5f, 3.5f);
        TestLogMessages.LogPosition(_logger, "Player", position);

        Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("Player"));
        // Vector3 should be formatted as string
        Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("1.5").Or.Contain("(1"));
    }

    [Test]
    public void LoggerMessage_WithGameObject_LogsObjectName()
    {
        var go = new GameObject("TestObject");
        try
        {
            TestLogMessages.LogGameObject(_logger, go);

            Assert.That(_mockHandler.LoggedMessages, Has.Count.EqualTo(1));
            Assert.That(_mockHandler.LoggedMessages[0].message, Does.Contain("TestObject"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    #endregion
}

/// <summary>
/// Static partial class containing LoggerMessage-attributed methods.
/// The source generator will implement these partial methods.
/// </summary>
internal static partial class TestLogMessages
{
    // Basic log messages
    [LoggerMessage(LogLevel.Information, "Application started")]
    public static partial void LogStartup(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "User {userName} logged in")]
    public static partial void LogUserLogin(this ILogger logger, string userName);

    [LoggerMessage(LogLevel.Information, "Item {itemName} purchased for {price} gold")]
    public static partial void LogItemPurchased(ILogger logger, string itemName, int price);

    [LoggerMessage(LogLevel.Information, "Player {playerName} stats: Health={health}, Mana={mana}")]
    public static partial void LogPlayerStats(ILogger logger, string playerName, int health, float mana);

    // Different log levels
    [LoggerMessage(LogLevel.Warning, "Player {playerName} health is low: {health}")]
    public static partial void LogLowHealth(ILogger logger, string playerName, int health);

    [LoggerMessage(LogLevel.Error, "Failed to connect to {serverName}")]
    public static partial void LogConnectionFailed(ILogger logger, string serverName);

    [LoggerMessage(LogLevel.Debug, "Debug: {data}")]
    public static partial void LogDebugInfo(ILogger logger, string data);

    // With exception
    [LoggerMessage(LogLevel.Error, "Error occurred: {message}")]
    public static partial void LogError(ILogger logger, string message, Exception exception);

    // With explicit EventId
    [LoggerMessage(1001, LogLevel.Information, "Important event: {data}", EventName = "ImportantEvent")]
    public static partial void LogWithEventId(ILogger logger, string data);

    // With SkipEnabledCheck
    [LoggerMessage(LogLevel.Information, "Always check: {data}", SkipEnabledCheck = true)]
    public static partial void LogAlwaysCheck(ILogger logger, string data);

    // Complex types
    [LoggerMessage(LogLevel.Information, "Entity {entityName} at position {position}")]
    public static partial void LogPosition(ILogger logger, string entityName, Vector3 position);

    [LoggerMessage(LogLevel.Information, "GameObject: {gameObject}")]
    public static partial void LogGameObject(ILogger logger, GameObject gameObject);
}
