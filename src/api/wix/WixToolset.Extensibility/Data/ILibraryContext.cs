// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;

    /// <summary>
    /// Context provided during library creation operations.
    /// </summary>
    public interface ILibraryContext
    {
        /// <summary>
        /// Service provider.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Indicates whether files should be bound into the library.
        /// </summary>
        bool BindFiles { get; set; }

        /// <summary>
        /// Collection of bindpaths used to bind files.
        /// </summary>
        IReadOnlyCollection<IBindPath> BindPaths { get; set; }

        /// <summary>
        /// Collection of extensions used during creation of library.
        /// </summary>
        IReadOnlyCollection<ILibrarianExtension> Extensions { get; set; }

        /// <summary>
        /// Identifier of the library.
        /// </summary>
        string LibraryId { get; set; }

        /// <summary>
        /// Collection of localization files to use in the library.
        /// </summary>
        IReadOnlyCollection<Localization> Localizations { get; set; }

        /// <summary>
        /// Collection of intermediates to include in the library.
        /// </summary>
        IReadOnlyCollection<Intermediate> Intermediates { get; set; }

        /// <summary>
        /// Cancellation token.
        /// </summary>
        CancellationToken CancellationToken { get; set; }
    }
}
