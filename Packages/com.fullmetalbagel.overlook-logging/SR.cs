// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Abstractions
{
    /// <summary>
    /// System resources for logging error messages.
    /// </summary>
    internal static class SR
    {
        internal const string UnexpectedNumberOfNamedParameters = "The format string '{0}' does not contain the expected number of named parameters. Expected {1} parameter(s) but found {2} parameter(s).";

        internal static string Format(string resourceFormat, params object?[] args)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, resourceFormat, args);
        }
    }
}
