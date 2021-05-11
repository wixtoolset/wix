// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using WixToolset.Data;

    /// <summary>
    /// Context provided during linking.
    /// </summary>
    public interface ILinkContext
    {
        /// <summary>
        /// Service provider.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Collection of extensions to use during linking.
        /// </summary>
        IReadOnlyCollection<ILinkerExtension> Extensions { get; set; }

        /// <summary>
        /// Collection of extension data to use during linking.
        /// </summary>
        IReadOnlyCollection<IExtensionData> ExtensionData { get; set; }

        /// <summary>
        /// Expected output type.
        /// </summary>
        OutputType ExpectedOutputType { get; set; }

        /// <summary>
        /// Collection of intermediates to link.
        /// </summary>
        IReadOnlyCollection<Intermediate> Intermediates { get; set; }

        /// <summary>
        /// Symbol definition creator used to load extension data.
        /// </summary>
        ISymbolDefinitionCreator SymbolDefinitionCreator { get; set; }

        /// <summary>
        /// Cancellation token.
        /// </summary>
        CancellationToken CancellationToken { get; set; }
    }
}
