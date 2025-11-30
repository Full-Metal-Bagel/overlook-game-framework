// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Provides information to guide the production of a strongly typed logging method.
    /// </summary>
    /// <remarks>
    /// <para>The method this attribute is applied to:</para>
    /// <para>   - Must be a partial method.</para>
    /// <para>   - Must return <c>void</c>.</para>
    /// <para>   - Must not be generic.</para>
    /// <para>   - Must have an <see cref="ILogger"/> as one of its parameters.</para>
    /// <para>   - Must have a <see cref="Microsoft.Extensions.Logging.LogLevel"/> as one of its parameters.</para>
    /// <para>   - None of the parameters can be generic.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LoggerMessageAttribute : Attribute
    {
        public LoggerMessageAttribute() { }

        public LoggerMessageAttribute(int eventId, LogLevel level, string message)
        {
            EventId = eventId;
            Level = level;
            Message = message;
        }

        public LoggerMessageAttribute(LogLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        public LoggerMessageAttribute(LogLevel level)
        {
            Level = level;
        }

        public LoggerMessageAttribute(string message)
        {
            Message = message;
        }

        public int EventId { get; set; } = -1;
        public string? EventName { get; set; }
        public LogLevel Level { get; set; } = LogLevel.None;
        public string Message { get; set; } = "";
        public bool SkipEnabledCheck { get; set; }
    }
}
