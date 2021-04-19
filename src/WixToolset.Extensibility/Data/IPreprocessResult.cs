// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System.Collections.Generic;
    using System.Xml.Linq;

    /// <summary>
    /// Result of preprocessing.
    /// </summary>
    public interface IPreprocessResult
    {
        /// <summary>
        /// Document result of preprocessor.
        /// </summary>
        XDocument Document { get; set; }

        /// <summary>
        /// Collection of files included during preprocessing.
        /// </summary>
        IReadOnlyCollection<IIncludedFile> IncludedFiles { get; set; }
    }
}
