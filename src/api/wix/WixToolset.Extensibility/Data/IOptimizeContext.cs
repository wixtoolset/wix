// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;

    /// <summary>
    /// Context provided to the optimizer.
    /// </summary>
    public interface IOptimizeContext
    {
        /// <summary>
        /// Service provider made available to the optimizer and its extensions.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Set of extensions provided to the optimizer.
        /// </summary>
        IReadOnlyCollection<IOptimizerExtension> Extensions { get; set; }

        /// <summary>
        /// Intermediate folder.
        /// </summary>
        string IntermediateFolder { get; set; }

        /// <summary>
        /// Collection of bindpaths used to bind files.
        /// </summary>
        IReadOnlyCollection<IBindPath> BindPaths { get; set; }

        /// <summary>
        /// Bind variables used during optimization.
        /// </summary>
        IDictionary<string, string> BindVariables { get; set; }

        /// <summary>
        /// Gets or sets the platform which the optimizer will use when defaulting 64-bit symbol properties.
        /// </summary>
        /// <value>The platform which the optimizer will use when defaulting 64-bit symbol properties.</value>
        Platform Platform { get; set; }

        /// <summary>
        /// Collection of intermediates to optimize.
        /// </summary>
        IReadOnlyCollection<Intermediate> Intermediates { get; set; }

        /// <summary>
        /// Collection of localization files to use in the optimizer.
        /// </summary>
        IReadOnlyCollection<Localization> Localizations { get; set; }

        /// <summary>
        /// Cancellation token.
        /// </summary>
        CancellationToken CancellationToken { get; set; }
    }
}
