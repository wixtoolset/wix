// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using System.Reflection;

    /// <summary>
    /// WiX branding interface.
    /// </summary>
    public interface IWixBranding
    {
        /// <summary>
        /// Gets the value for CreatingApplication field (MSI Summary Information Stream).
        /// </summary>
        /// <returns>String for creating application.</returns>
        string GetCreatingApplication();

        /// <summary>
        /// Replaces branding placeholders in original string.
        /// </summary>
        /// <param name="original">Original string containing placeholders to replace.</param>
        /// <param name="assembly">Optional assembly with branding information, if not specified core branding is used.</param>
        /// <returns></returns>
        string ReplacePlaceholders(string original, Assembly assembly = null);
    }
}
