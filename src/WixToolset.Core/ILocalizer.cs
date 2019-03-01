// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Xml.Linq;
    using WixToolset.Data;

    /// <summary>
    /// Parses localization source files.
    /// </summary>
    public interface ILocalizer
    {
        /// <summary>
        /// Loads a localization file from a path on disk.
        /// </summary>
        /// <param name="path">Path to localization file saved on disk.</param>
        /// <returns>Returns the loaded localization file.</returns>
        Localization ParseLocalizationFile(string path);

        /// <summary>
        /// Loads a localization file from memory.
        /// </summary>
        /// <param name="document">Document to parse as localization file.</param>
        /// <returns>Returns the loaded localization file.</returns>
        Localization ParseLocalizationFile(XDocument document);
    }
}
