// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;

    /// <summary>
    /// Context for resolve.
    /// </summary>
    public interface IResolveContext
    {
        /// <summary>
        /// Service provider.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Bind paths used during resolution.
        /// </summary>
        IReadOnlyCollection<IBindPath> BindPaths { get; set; }

        /// <summary>
        /// Resolve extensions.
        /// </summary>
        IReadOnlyCollection<IResolverExtension> Extensions { get; set; }

        /// <summary>
        /// Extension data.
        /// </summary>
        IReadOnlyCollection<IExtensionData> ExtensionData { get; set; }

        /// <summary>
        /// List of cultures to filter the localizations.
        /// </summary>
        IReadOnlyCollection<string> FilterCultures { get; set; }

        /// <summary>
        /// Intermediate folder.
        /// </summary>
        string IntermediateFolder { get; set; }

        /// <summary>
        /// Intermediate to resolve.
        /// </summary>
        Intermediate IntermediateRepresentation { get; set; }

        /// <summary>
        /// Localizations used to resolve.
        /// </summary>
        IReadOnlyCollection<Localization> Localizations { get; set; }

        /// <summary>
        /// Indicates whether to allow localization and bind variables to remain unresolved.
        /// </summary>
        bool AllowUnresolvedVariables { get; set; }

        /// <summary>
        /// Cancellation token.
        /// </summary>
        CancellationToken CancellationToken { get; set; }
    }
}
