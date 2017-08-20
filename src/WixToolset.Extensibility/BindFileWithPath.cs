// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    /// <summary>
    /// Bind file with its path.
    /// </summary>
    public class BindFileWithPath
    {
        /// <summary>
        /// Gets or sets the identifier of the file with this path.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string Path { get; set; }
    }
}
