// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Generators
{
    public static class DiagnosticDescriptors
    {
        public static DiagnosticDescriptor InvalidLoggingMethodName { get; } = new(
            id: "SYSLIB1001",
            title: "Logging method names cannot start with _",
            messageFormat: "Logging method names cannot start with _",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntMentionLogLevelInMessage { get; } = new(
            id: "SYSLIB1002",
            title: "Don't include log level parameters as templates in the logging message",
            messageFormat: "Don't include a template for {0} in the logging message since it is implicitly taken care of",
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidLoggingMethodParameterName { get; } = new(
            id: "SYSLIB1003",
            title: "Logging method parameter names cannot start with _",
            messageFormat: "Logging method parameter names cannot start with _",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingRequiredType { get; } = new(
            id: "SYSLIB1005",
            title: "Could not find a required type definition",
            messageFormat: "Could not find definition for type {0}",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntReuseEventIds { get; } = new(
            id: "SYSLIB1006",
            title: "Multiple logging methods cannot use the same event id within a class",
            messageFormat: "Multiple logging methods are using event id {0} in class {1}",
            category: "LoggingGenerator",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodMustReturnVoid { get; } = new(
            id: "SYSLIB1007",
            title: "Logging methods must return void",
            messageFormat: "Logging methods must return void",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLoggerArgument { get; } = new(
            id: "SYSLIB1008",
            title: "One of the arguments to a static logging method must implement the Microsoft.Extensions.Logging.ILogger interface",
            messageFormat: "One of the arguments to the static logging method '{0}' must implement the Microsoft.Extensions.Logging.ILogger interface",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodShouldBeStatic { get; } = new(
            id: "SYSLIB1009",
            title: "Logging methods must be static",
            messageFormat: "Logging methods must be static",
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodMustBePartial { get; } = new(
            id: "SYSLIB1010",
            title: "Logging methods must be partial",
            messageFormat: "Logging methods must be partial",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodIsGeneric { get; } = new(
            id: "SYSLIB1011",
            title: "Logging methods cannot be generic",
            messageFormat: "Logging methods cannot be generic",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RedundantQualifierInMessage { get; } = new(
            id: "SYSLIB1012",
            title: "Redundant qualifier in logging message",
            messageFormat: "Remove redundant qualifier (Info:, Warning:, Error:, etc) from the logging message since it is implicit in the specified log level.",
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntMentionExceptionInMessage { get; } = new(
            id: "SYSLIB1013",
            title: "Don't include exception parameters as templates in the logging message",
            messageFormat: "Don't include a template for {0} in the logging message since it is implicitly taken care of",
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor TemplateHasNoCorrespondingArgument { get; } = new(
            id: "SYSLIB1014",
            title: "Logging template has no corresponding method argument",
            messageFormat: "Template '{0}' is not provided as argument to the logging method",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ArgumentHasNoCorrespondingTemplate { get; } = new(
            id: "SYSLIB1015",
            title: "Argument is not referenced from the logging message",
            messageFormat: "Argument '{0}' is not referenced from the logging message",
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodHasBody { get; } = new(
            id: "SYSLIB1016",
            title: "Logging methods cannot have a body",
            messageFormat: "Logging methods cannot have a body",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLogLevel { get; } = new(
            id: "SYSLIB1017",
            title: "A LogLevel value must be supplied",
            messageFormat: "A LogLevel value must be supplied in the LoggerMessage attribute or as a parameter to the logging method",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntMentionLoggerInMessage { get; } = new(
            id: "SYSLIB1018",
            title: "Don't include logger parameters as templates in the logging message",
            messageFormat: "Don't include a template for {0} in the logging message since it is implicitly taken care of",
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLoggerField { get; } = new(
            id: "SYSLIB1019",
            title: "Couldn't find a field of type Microsoft.Extensions.Logging.ILogger",
            messageFormat: "Couldn't find a field of type Microsoft.Extensions.Logging.ILogger in class {0}",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MultipleLoggerFields { get; } = new(
            id: "SYSLIB1020",
            title: "Found multiple fields of type Microsoft.Extensions.Logging.ILogger",
            messageFormat: "Found multiple fields of type Microsoft.Extensions.Logging.ILogger in class {0}",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InconsistentTemplateCasing { get; } = new(
            id: "SYSLIB1021",
            title: "Can't have the same template with different casing",
            messageFormat: "Can't have the same template with different casing",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MalformedFormatStrings { get; } = new(
            id: "SYSLIB1022",
            title: "Logging method contains malformed format strings",
            messageFormat: "Logging method '{0}' contains malformed format strings",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor GeneratingForMax6Arguments { get; } = new(
            id: "SYSLIB1023",
            title: "Generating more than 6 arguments is not supported",
            messageFormat: "Generating more than 6 arguments is not supported",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidLoggingMethodParameterOut { get; } = new(
            id: "SYSLIB1024",
            title: "Argument is using the unsupported out parameter modifier",
            messageFormat: "Argument '{0}' is using the unsupported out parameter modifier",
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntReuseEventNames { get; } = new(
            id: "SYSLIB1025",
            title: "Multiple logging methods should not use the same event name within a class",
            messageFormat: "Multiple logging methods are using event name {0} in class {1}",
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingUnsupportedLanguageVersion { get; } = new(
            id: "SYSLIB1026",
            title: "C# language version not supported by the source generator",
            messageFormat: "The Logging source generator is not available in C# {0}. Please use language version {1} or greater.",
            category: "LoggingGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor PrimaryConstructorParameterLoggerHidden { get; } = new(
            id: "SYSLIB1027",
            title: "Primary constructor parameter of type Microsoft.Extensions.Logging.ILogger is hidden by a field",
            messageFormat: "Class '{0}' has a primary constructor parameter of type Microsoft.Extensions.Logging.ILogger that is hidden by a field in the class or a base class, preventing its use",
            category: "LoggingGenerator",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}
