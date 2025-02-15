// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.Core.MSTestPackage
{
    using System;
    using WixToolset.Data;

    /// <summary>
    /// Utility class to help format messages.
    /// </summary>
    public static class WixMessageFormatter
    {
        /// <summary>
        /// Formats a message into a standard string with the level, id, and message.
        /// </summary>
        /// <param name="message">Message to format</param>
        /// <returns>Standard message formatting with the level, id, and message.</returns>
        public static string FormatMessage(Message message)
        {
            return $"{message.Level} {message.Id}: {message}";
        }

        /// <summary>
        /// Formats a message into a standard string with the level, id, and message.
        /// </summary>
        /// <param name="message">Message to format</param>
        /// <param name="replacementMatch">Match for the replacement</param>
        /// <param name="replacement">Value to replace</param>
        /// <returns>Standard message formatting with the level, id, and message.</returns>
        public static string FormatMessage(Message message, string replacementMatch, string replacement)
        {
            if (replacement is null)
            {
                throw new ArgumentNullException(nameof(replacement));
            }

            return $"{message.Level} {message.Id}: {message}".Replace(replacementMatch, replacement);
        }
    }
}
