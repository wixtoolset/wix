// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    public class IntermediateFieldPathValue
    {
        /// <summary>
        /// Gets or sets the index of the embedded file in a library.
        /// </summary>
        /// <value>The index of the embedded file.</value>
        public int? EmbeddedFileIndex { get; set; }

        /// <summary>
        /// Gets the base URI of the path field.
        /// </summary>
        /// <value>The base URI of the path field.</value>
        public Uri BaseUri { get; set; }

        /// <summary>
        /// Gets or sets the data for this field.
        /// </summary>
        /// <value>Data in the field.</value>
        public string Path { get; set; }
    }
}
