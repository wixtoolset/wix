// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using WixToolset.Data;

    public interface IIncludedFile
    {
        /// <summary>
        /// Gets the full path of the included file.
        /// </summary>
        /// <value>The full path of the included file.</value>
        string Path { get; set; }

        /// <summary>
        /// Gets the source line numbers.
        /// </summary>
        /// <value>The source line numbers.</value>
        SourceLineNumber SourceLineNumbers { get; set; }
    }
}
