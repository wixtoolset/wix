// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;

    /// <summary>
    /// Result of a library combine operation.
    /// </summary>
    public interface ILibraryResult
    {
        /// <summary>
        /// Collection of files tracked when binding files into the library.
        /// </summary>
        IReadOnlyCollection<ITrackedFile> TrackedFiles { get; set; }

        /// <summary>
        /// Output of librarian.
        /// </summary>
        Intermediate Library { get; set; }
    }
}
